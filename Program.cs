using System;

namespace HttpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            HttpServer server = new HttpServer();
            server.Start();

            while(true)
            {
                switch (Console.ReadLine())
                {
                    case "start":
                        server.Start();
                        break;
                    case "restart":
                        server.Restart();
                        break;
                    case "stop":
                        server.Stop();
                        break;
                    default:
                        Console.WriteLine("Неизвестная команда");
                        break;
                }
            }
        }
    }
}