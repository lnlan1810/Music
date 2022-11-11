using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using HttpServer.Attributes;
using HttpServer.MyORM;

namespace HttpServer
{
    public class HttpServer : IDisposable
    {
        private ServerSetting _serverSettings;
        private readonly HttpListener _httpListener;

        public string SettingsPath;
        public ServerStatus Status = ServerStatus.Stopped;
        public ServerStatus ServerStatus { get; private set; } = ServerStatus.Stopped;

        public HttpServer()
        {
            _httpListener = new HttpListener();
        }

        public HttpServer(string settingsPath)
        {
            SettingsPath = settingsPath;
            _httpListener = new HttpListener();
        }

        public void Start()
        {
            if (ServerStatus == ServerStatus.Stopped)
            {
                _serverSettings = JsonSerializer.Deserialize<ServerSetting>(File.ReadAllBytes("./settings.json"));
                _httpListener.Prefixes.Clear();
                _httpListener.Prefixes.Add($"http://localhost:{_serverSettings.Port}/");
                Console.WriteLine("Server is starting...");
                _httpListener.Start();
 
                Console.WriteLine($"Server running on port {_serverSettings.Port}");
                Listening();
            }
            else
            {
                Console.WriteLine("Server is already running!");
            }
        }

        public void Stop()
        {
            if (ServerStatus == ServerStatus.Stopped)
                return;

            Console.WriteLine("Server stop...");
            _httpListener.Stop();
            ServerStatus = ServerStatus.Stopped;
            Console.WriteLine("Server stopped.");
        }

        private async void Listening()
        {
            while (_httpListener.IsListening)
            {
                var _httpContext = await _httpListener.GetContextAsync();

              //  ShowRequestData(_httpContext.Request);
                
                if (MethodHandler(_httpContext)) return;

                StaticFiles(_httpContext.Request, _httpContext.Response);
            }
        }

        private void StaticFiles(HttpListenerRequest request, HttpListenerResponse response)
        {
 
            byte[] buffer;

            if (Directory.Exists(_serverSettings.Path))
            {
                string fileUrl = request.RawUrl.Replace("%20", " ");
                buffer = GetFile(fileUrl);

                if (buffer == null)
                {
                    response.Headers.Set("Content-Type", "text/plain");
                    response.StatusCode = (int)HttpStatusCode.NotFound;

                    string err = "404 - Not Found";
                    buffer = Encoding.UTF8.GetBytes(err);
                }
                else
                {
                    response.Headers.Set("Content-Type", GetExtension(fileUrl));
                }
            }
            else
            {
                response.Headers.Set("Content-Type", "text/plain");
                response.StatusCode = (int)HttpStatusCode.NotFound;

                string err = $"404 - No Directory {_serverSettings.Path}";
                buffer = Encoding.UTF8.GetBytes(err);
            }

            response.ContentLength64 = buffer.Length;

            Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();

        }

        private byte[] GetFile(string url)
        {
            byte[] buffer = null;
            var filePath = _serverSettings.Path + url;
            if (Directory.Exists(filePath))
            {
                filePath = filePath + "index.html";
                if (File.Exists(filePath))
                {
                    buffer = File.ReadAllBytes(filePath);
                }
            }
            else if (File.Exists(filePath))
            {
                buffer = File.ReadAllBytes(filePath);
            }

            return buffer;
        }

        private bool MethodHandler(HttpListenerContext _httpContext)
        {
            // объект запроса
            HttpListenerRequest request = _httpContext.Request;

            // объект ответа
            HttpListenerResponse response = _httpContext.Response;

            if (_httpContext.Request.Url.Segments.Length < 2) return false;

            string controllerName = _httpContext.Request.Url.Segments[1].Replace("/", "");

            string[] strParams = _httpContext.Request.Url
                                    .Segments
                                    .Skip(2)
                                    .Select(s => s.Replace("/", ""))
                                    .ToArray();

            if (strParams.Length == 0 && request.HttpMethod == "POST")
            {
                Stream body = request.InputStream;
                Encoding encoding = request.ContentEncoding;
                StreamReader reader = new StreamReader(body, encoding);
                Console.WriteLine("получен post запрос, content length: {0}", request.ContentLength64);

                string s = reader.ReadToEnd();
                var paramList = new List<string>();
                foreach (string a in s.Split('&'))
                {
                    paramList.Add(a.Split('=')[1]);
                }

                strParams = paramList.ToArray();
                body.Close();
                reader.Close();
            }

            var assembly = Assembly.GetExecutingAssembly();

            var controller = assembly.GetTypes().Where(t => Attribute.IsDefined(t, typeof(HttpController))).FirstOrDefault(c => c.Name.ToLower() == controllerName.ToLower());

            if (controller == null) return false;

            var test = typeof(HttpController).Name;
            var method = controller.GetMethods()
                .FirstOrDefault(t => t.GetCustomAttributes(true)
                    .Any(attr => attr.GetType().Name == $"Http{_httpContext.Request.HttpMethod}") && t.GetParameters().Length == strParams.Length);

            if (method == null) return false;

            object? ret;

            if (strParams.Length == method.GetParameters().Length)
            {
                List<object> queryParams = new List<object>();
                bool BadRequest = false;
                try
                {
                    queryParams = method.GetParameters()
                        .Select((p, i) => Convert.ChangeType(strParams[i], p.ParameterType))
                        .ToList();
                }
                catch (FormatException)
                {
                    BadRequest = true;
                }

                if (!BadRequest) ret = method.Invoke(Activator.CreateInstance(controller), queryParams.ToArray());
                else
                {
                    ret = new string("Bad arguments!");
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                }

            }
            else
            {
                ret = new string("Argument count mismatch!");
                response.StatusCode = (int)HttpStatusCode.BadRequest;
            }

            if (ret != null && ret.ToString() == "Steam_redirect")
            {
                using (response)
                {
                    response.StatusCode = (int)HttpStatusCode.Redirect;
                    response.Headers.Set("Location", "http://store.steampowered.com/");

                    return true;
                }
            }

            response.ContentType = "Application/json";

            byte[] buffer = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(ret));
            response.ContentLength64 = buffer.Length;

            Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);

            output.Close();

            return true;
        }


        private string GetExtension(string url)
        {
            string ext = Path.GetExtension(url);
            if (Directory.Exists(_serverSettings.Path + url)) ext = ".html";
            switch (ext)
            {
                case ".html":
                    return "text/html";
                case ".css":
                    return "text/css";
                case ".js":
                    return "text/javascript";
                case ".png":
                    return "image/png";
                case ".svg":
                    return "image/svg+xml";
            }
            return "text/plain";
        }
        

        public void Restart()
        {
            Console.WriteLine(" Server restart..");//Перезапуск сервера
            Stop();
            Start();
        }

        public void Dispose()
        {
            _httpListener.Close();
        }

         public object[] ShowRequestData(HttpListenerRequest request)
        {
            if (!request.HasEntityBody)
            {
                Console.WriteLine("No client data was sent with the request.");
                return null;
            }

            Stream body = request.InputStream;
            Encoding encoding = request.ContentEncoding;
            StreamReader reader = new StreamReader(body, encoding);
            if (request.ContentType != null)
            {
                Console.WriteLine("Client data content type {0}", request.ContentType);

            }
            Console.WriteLine("Client data content length {0}", request.ContentLength64);

            Console.WriteLine("data of client data:");

            string s = reader.ReadToEnd();
            Console.WriteLine(s);
            Console.WriteLine("end of client data");

            body.Close();
            reader.Close();

            object[] paramsA = null;

            return paramsA;
        }

        private void Show404(ref HttpListenerResponse response, ref byte[] buffer)
        {
            response.Headers.Set("Content-Type", "text/html");
            response.StatusCode = 404;
            response.ContentEncoding = Encoding.UTF8;
        }
    }
}