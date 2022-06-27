using System;
using System.Diagnostics;
using System.Threading.Tasks;
using RadioStation.ConsoleStation.Audio;

namespace RadioStation.ConsoleStation
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
                Start();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error {e.Message}: {e.StackTrace}");
            }

            Task.Delay(-1).Wait();
        }

        private static void Start()
        {
            Radio radio = StartRadio(Settings.MusicPath);
            WebListener webListener = StartWebListener(radio);
            StartScreen(webListener, radio);
        }

        private static Radio StartRadio(string musicPath)
        {
            Radio radio = new Radio(new MusicCollectionFileSystem(musicPath));
            radio.Start();

            return radio;
        }
        private static WebListener StartWebListener(Radio radio)
        {
            WebListener webListener = new WebListener(radio);
            webListener.Start();

            return webListener;
        }

        static void StartScreen(WebListener webListener, Radio radio)
        {
            new MainScreen(webListener, radio).Start();
        }
    }
}
