using System;

namespace HttpServer.Attributes
{
    public class HttpPOST : Attribute
    {
        public string MethodURI;

        public HttpPOST(string methodUri)
        {
            MethodURI = methodUri;
        }
    }
}