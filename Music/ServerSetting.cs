using System;
using System.Collections.Generic;
using System.Text;

namespace Music
{
    public class ServerSetting
    {
        public int Port { get; set; } = 7700;

        public string Path { get; set; } = @"./music/";
    }
}
