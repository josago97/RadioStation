using System;
using System.IO;
using System.Text.Json;
using Sharplus.System;

namespace RadioStation.ConsoleStation
{
    public static class Settings
    {
        public static int Port;
        public static string MusicPath;
        public static string Name;
        public static string Genre;

        static Settings()
        {
            EnvironmentUtils.LoadVariables("Environment.env");

            Port = int.Parse(Environment.GetEnvironmentVariable("PORT"));
            MusicPath = Environment.GetEnvironmentVariable("MUSIC_PATH");
            Name = Environment.GetEnvironmentVariable("NAME");
            Genre = Environment.GetEnvironmentVariable("GENRE");
        }
    }
}
