using System;

namespace HttpServer.Attributes
{
    public class HttpController : Attribute
    {
        public string ControllerName;

        public HttpController(string name)
        {
            ControllerName = name;
        }
    }
}