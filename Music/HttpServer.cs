using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Music
{
    public class HttpServer : IDisposable
    {

        public ServerStatus Status = ServerStatus.Stop;

        private ServerSetting _serverSetting;

        private readonly HttpListener _httpListener;

        public HttpServer()
        {
            _httpListener = new HttpListener();
            // _httpListener.Prefixes.Add($"http://localhost:{_serverSetting.Port}/");

        }

        public void Start()
        {

            if (Status == ServerStatus.Start)
            {
                //Server is already running- Сервер уже запущен
                Console.WriteLine("Server is already running");
                return;
            }

            _serverSetting = JsonSerializer.Deserialize<ServerSetting>(File.ReadAllBytes("./settings.json"));

            _httpListener.Prefixes.Clear();
            _httpListener.Prefixes.Add($"http://localhost:{_serverSetting.Port}/");

            //Запуск сервера....
            Console.WriteLine("Server start ....");
            _httpListener.Start();


            // Сервер запущен
            Console.WriteLine("Server started");
            Status = ServerStatus.Start;


            Listening();

        }

        public void Stop()
        {
            if (Status == ServerStatus.Stop) return;

            //  Остановка сервера...
            Console.WriteLine("Stopping the server...");
            _httpListener.Stop();

            Status = ServerStatus.Stop;
            // сервер остановлен
            Console.WriteLine("server stopped");
        }

        private void Listening()
        {
            _httpListener.BeginGetContext(new AsyncCallback(ListenerCallback), _httpListener);

        }

        private void ListenerCallback(IAsyncResult result)
        {
            if (_httpListener.IsListening)
            {

                var _httpContext = _httpListener.EndGetContext(result);

                HttpListenerRequest request = _httpContext.Request;

                // получаем обьеккт ответа 
                HttpListenerResponse response = _httpContext.Response;


                /*
                // создаем ответ в виде кода html
                string responseStr = "<html><head><meta charset='utf8'></head><body>Привет мир!</body></html>";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseStr);
                */

                byte[] buffer;

                // var path = Directory.GetCurrentDirectory();

                if (Directory.Exists(_serverSetting.Path))
                {
                    buffer = getFile(request.RawUrl.Replace("%20", " "));

                    if (buffer == null)
                    {
                        response.Headers.Set("Content-Type", "text/plain");

                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        string err = "404 - not found";

                        buffer = Encoding.UTF8.GetBytes(err);

                    }
                }
                else
                {
                    string err = $"Directory '{_serverSetting}'404 - not found";

                    buffer = Encoding.UTF8.GetBytes(err);

                }
                Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);

                // закрываем поток
                output.Close();

                Listening();
            }
        }

        private byte[] getFile(string rawUrl)
        {
            byte[] buffer = null;
            var filePath = _serverSetting.Path + rawUrl;

            if (Directory.Exists(filePath))
            {
                //каталог
                filePath = filePath + "/index.html";
                if (File.Exists(filePath))
                {
                    buffer = File.ReadAllBytes(filePath);
                }

            }
            else if (File.Exists(filePath))
            {
                //файл
                buffer = File.ReadAllBytes(filePath);
            }

            return buffer;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
