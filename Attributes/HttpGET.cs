using System;

namespace HttpServer.Attributes
{
    public class HttpGET : Attribute
    {
        public string MethodURI;

        public HttpGET(string methodUri)
        {
            MethodURI = methodUri;
        }

        public HttpGET() { MethodURI = null; }
    }
}