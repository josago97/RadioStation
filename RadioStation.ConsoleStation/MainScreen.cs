using System;
using System.Collections.Generic;
using RadioStation.ConsoleStation.Audio;
using RadioStation.ConsoleStation.Utils;

namespace RadioStation.ConsoleStation
{
    public class MainScreen
    {
        private const int HISTORY_SIZE = 10;

        private WebListener _webListener;
        private Radio _radio;
        private CircularArray<Song> _songHistory;
        private CircularArray<Exception> _errorHistory;

        private object _screenLocker = new object();

        public MainScreen(WebListener webListener, Radio radio)
        {
            _webListener = webListener;
            _radio = radio;

            _songHistory = new CircularArray<Song>(HISTORY_SIZE);
            _errorHistory = new CircularArray<Exception>(HISTORY_SIZE);

            _radio.Error += OnError;
            _radio.SongStartedPlay += _ => Update();
            _radio.SongFinishedPlay += OnSongFinished;
            _radio.ListenerConnected += _ => Update();
            _radio.ListenerDisconnected += _ => Update();
        }

        public void Start()
        {
            Update();
        }

        private void OnError(Exception error)
        {
            lock(_screenLocker) _errorHistory.Add(error);

            Update();
        }

        private void OnSongFinished(Song song)
        {
            lock (_screenLocker) _songHistory.Add(song);

            Update();
        }

        private void Update()
        {
            lock (_screenLocker)
            {
                Console.Clear();

                DisplayUrl();
                Console.WriteLine();
                DisplayListeners();
                Console.WriteLine();
                DisplayCurrentSong();
                Console.WriteLine();
                DisplaySongHistory();
                Console.WriteLine();
                Console.WriteLine();
                DisplayErrorHistory();
            }
        }

        private void DisplayUrl()
        {
            Console.WriteLine($"Desplegado en: [{string.Join(", ", _webListener.Prefixes)}]");
        }

        private void DisplayListeners()
        {
            Console.WriteLine($"Oyentes: {_radio.ListenersCount}");
        }

        private void DisplayCurrentSong()
        {
            Song currentSong = _radio.CurrentSong;
            string display = currentSong != null ? currentSong.ToString() : "Ninguna";
            Console.WriteLine($"Canción actual: {display}");
        }

        private void DisplaySongHistory()
        {
            Console.WriteLine("Historial de reproducción");

            if (_songHistory.Length > 0)
            {
                for (int i = 0; i < _songHistory.Length; i++)
                {
                    Song song = _songHistory[_songHistory.Length - i - 1];
                    Console.WriteLine($"{i + 1} -> {song}");
                }
            }
            else
            {
                Console.WriteLine("Vacío");
            }
        }

        private void DisplayErrorHistory()
        {
            Console.WriteLine("Lista de errores");

            if (_errorHistory.Length > 0)
            {
                for(int i = 0; i < _errorHistory.Length; i++)
                {
                    Exception error = _errorHistory[_errorHistory.Length - i - 1];

                    Console.WriteLine($"{i + 1} -> {error.GetType()}, {error.Message} {error.StackTrace}");
                }
            }
            else
            {
                Console.WriteLine("Vacío");
            }
        }
    }
}
