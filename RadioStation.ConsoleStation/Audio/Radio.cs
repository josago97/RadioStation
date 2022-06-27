using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Lame;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using RadioStation.ConsoleStation.Utils;

namespace RadioStation.ConsoleStation.Audio
{
    public class Radio
    {
        public const int METADATA_BYTE_RATE = 16384; // 2^14 bytes
        public const int AUDIO_BIT_RATE = 96;
        private const int MAX_FRAMES_BUFFER = 100;
        private const int MAX_FRAMES_INITIAL_BUFFER = MAX_FRAMES_BUFFER * 3;
        private static readonly WaveFormat AUDIO_FORMAT = new WaveFormat(44100, 16, 2);

        private ConcurrentList<Listener> _listeners;
        private PlayList _playList;
        private Song _currentSong;
        private Stopwatch _playStopwatch;
        private IcyMetadata _currentMetadata;
        private List<Mp3Frame> _framesBuffer;
        private CircularArray<Mp3Frame> _initialBuffer;
        private object _sendLocker;

        public event Action<Exception> Error;
        public event Action<Listener> ListenerConnected;
        public event Action<Listener> ListenerDisconnected;
        public event Action<Song> SongStartedPlay;
        public event Action<Song> SongFinishedPlay;

        public Song CurrentSong => _currentSong;
        public int ListenersCount => _listeners.Count;

        public Radio(MusicCollection musicCollection)
        {
            _playList = new PlayList(musicCollection);
            _listeners = new ConcurrentList<Listener>();
            _playStopwatch = new Stopwatch();
            _framesBuffer = new List<Mp3Frame>(MAX_FRAMES_BUFFER);
            _initialBuffer = new CircularArray<Mp3Frame>(MAX_FRAMES_INITIAL_BUFFER);
            _sendLocker = new object();
        }

        public void AddListener(Listener listener)
        {
            if (!_listeners.Contains(listener))
            {
                InitListener(listener);
                Task.Run(() => ListenerConnected?.Invoke(listener));
            }
        }

        public void RemoveListener(Listener listener)
        {
            _listeners.Remove(listener);
            Task.Run(() => ListenerDisconnected?.Invoke(listener));
        }

        public void Start()
        {
            Thread playThread = new Thread(PlaySongs);
            playThread.Priority = ThreadPriority.Highest;
            playThread.Start();
        }

        private void PlaySongs()
        {
            Func<SongStreaming> getNextSong = () =>
            {
                _playList.MoveNext();
                return _playList.Current;
            };

            try
            {
                SongStreaming currentSong = getNextSong();

                while (true)
                {
                    _currentSong = currentSong;
                    Task<SongStreaming> getNextSongTask = Task.Run(getNextSong);
                    PlaySong(currentSong);
                    currentSong = getNextSongTask.Result;
                }
            }
            catch (Exception e)
            {
                Task.Run(() => Error?.Invoke(e));
            }
        }

        private void PlaySong(SongStreaming song)
        {
            Task.Run(() => SongStartedPlay?.Invoke(song));

            using (Mp3FileReader mp3Reader = song.Mp3Reader)
            {
                ReadMetadata(song);
                PlayMp3File(mp3Reader);
            }

            Task.Run(() => SongFinishedPlay?.Invoke(song));
        }

        private void ReadMetadata(Song song)
        {
            _currentMetadata = new IcyMetadata(song.Name, song.Category);
            lock (_sendLocker) _listeners.ForEach(l => l.UpdateMetadata(_currentMetadata));
        }

        private void PlayMp3File(Mp3FileReader mp3Reader)
        {
            bool endOfFile = false;
            long lastAudioTimeTicks = 0;
            long lastStopwatchTicks = 0;
            long waitTicks = 0;
            Task sendFramesTask = null;

            _playStopwatch.Restart();

            while (!endOfFile)
            {
                endOfFile = ReadFrames(mp3Reader);

                if (_framesBuffer.Count > 0)
                {
                    Mp3Frame[] frames = _framesBuffer.ToArray();
                    sendFramesTask?.Wait();
                    sendFramesTask = Task.Run(() => SendFrames(frames));
                    _framesBuffer.Clear();

                    waitTicks += CalculateElapseTicks(mp3Reader.CurrentTime.Ticks, ref lastAudioTimeTicks);
                    waitTicks -= CalculateElapseTicks(_playStopwatch.ElapsedTicks, ref lastStopwatchTicks);

                    if (waitTicks > 0)
                    {
                        Thread.Sleep(new TimeSpan(waitTicks));
                        waitTicks -= CalculateElapseTicks(_playStopwatch.ElapsedTicks, ref lastStopwatchTicks);
                    }
                }
                else
                {
                    endOfFile = true;
                }
            }

            _playStopwatch.Stop();
        }

        private bool ReadFrames(Mp3FileReader mp3Reader)
        {
            bool endOfFile = false;

            while (_framesBuffer.Count < MAX_FRAMES_BUFFER && !endOfFile)
            {
                Mp3Frame mp3Frame = mp3Reader.ReadNextFrame();

                if (mp3Frame != null)
                    _framesBuffer.Add(mp3Frame);
                else
                    endOfFile = true;
            }

            return endOfFile;
        }

        private long CalculateElapseTicks(long currentTicks, ref long lastTicks)
        {
            long elapseTicks = currentTicks - lastTicks;
            lastTicks = currentTicks;

            return elapseTicks;
        }

        private void SendFrames(Mp3Frame[] frames)
        {
            lock (_sendLocker)
            {
                _initialBuffer.AddRange(frames);

                for (int i = _listeners.Count - 1; i >= 0; i--)
                {
                    Listener listener = _listeners[i];

                    if (listener.IsListening)
                    {
                        SendFrames(listener, frames);
                    }
                    else
                    {
                        RemoveListener(listener);
                    }
                }
            }
        }

        private void InitListener(Listener listener)
        {
            lock (_sendLocker)
            {
                listener.UpdateMetadata(_currentMetadata);

                Mp3Frame[] frames = _initialBuffer.ToArray();
                SendFrames(listener, frames);
                _listeners.Add(listener);
            }
        }

        private void SendFrames(Listener listener, Mp3Frame[] frames)
        {
            byte[] data = frames.SelectMany(f => f.RawData).ToArray();
            if (data.Length > 0) listener.SendData(data);
        }

        private class SongStreaming : Song
        {
            public Mp3FileReader Mp3Reader { get; private set; }

            public SongStreaming(Song song, Stream stream) : base(song.Id, song.Name, song.Category)
            {
                CreateMp3Reader(stream);
            }

            private void CreateMp3Reader(Stream stream)
            {
                WaveFormat desiredFormat = AUDIO_FORMAT;
                int desiredBitRate = AUDIO_BIT_RATE;
                Stream desiredStream = new MemoryStream();

                using (var reader = new Mp3FileReader(stream))
                using (var conversionStream = new WaveFormatConversionStream(desiredFormat, reader))
                using (var normalizedAudio = NormalizeVolume(conversionStream))
                using (var mp3Writer = new LameMP3FileWriter(desiredStream, desiredFormat, desiredBitRate))
                {
                    normalizedAudio.CopyTo(mp3Writer);
                    normalizedAudio.Flush();
                }

                desiredStream.Position = 0;
                Mp3Reader = new Mp3FileReader(desiredStream);
            }

            private Stream NormalizeVolume(WaveStream waveStream)
            {
                var volumeProvider = new VolumeSampleProvider(waveStream.ToSampleProvider());
                float[] buffer = new float[waveStream.WaveFormat.SampleRate];
                float maxVolume = 0;
                int read;

                while ((read = volumeProvider.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < read; i++)
                    {
                        float absoluteVolume = Math.Abs(buffer[i]);
                        if (absoluteVolume > maxVolume) maxVolume = absoluteVolume;
                    }
                }

                waveStream.Position = 0;
                if (maxVolume > 0) volumeProvider.Volume = 1.0f / maxVolume;

                MemoryStream normalizedAudio = new MemoryStream();
                WaveFileWriter.WriteWavFileToStream(normalizedAudio, volumeProvider.ToWaveProvider16());

                return normalizedAudio;
            }
        }

        private class PlayList : IEnumerator<SongStreaming>
        {
            private MusicCollection _musicCollection;
            private Song[] _allSongs;
            private int _readIndex;
            private Random _random;

            public SongStreaming Current { get; private set; }

            object IEnumerator.Current => Current;

            public PlayList(MusicCollection musicCollection)
            {
                _musicCollection = musicCollection;
                _random = new Random();

                Reset();
            }

            public bool MoveNext()
            {
                if (_allSongs == null || _allSongs.Length == 0 || _readIndex >= _allSongs.Length)
                {
                    GetAllSong();
                    _readIndex = 0;
                }

                SongStreaming newSong = null;

                do
                {
                    Song songCandidate = _allSongs[_readIndex];
                    Stream songStream = _musicCollection.GetStream(songCandidate);

                    if (songStream != null) newSong = new SongStreaming(songCandidate, songStream);
                     
                    _readIndex++;

                } while (newSong == null);

                Current = newSong;

                return true;
            }

            public void Reset()
            {
                _readIndex = 0;
                Current = null;
            }

            public void Dispose()
            {
            }

            private void GetAllSong()
            {
                do
                {
                    _musicCollection.Refresh();
                }
                while (_musicCollection.AllSongs.Length == 0);

                _allSongs = _musicCollection.AllSongs.OrderBy(_ => _random.Next()).ToArray();

                if (_allSongs.Length > 1 && Current != null && _allSongs[0].Id == Current.Id)
                {
                    int start = _allSongs.Length / 2;
                    int destinationIndex = _random.Next(start, _allSongs.Length);
                    Song song = _allSongs[0];
                    _allSongs[0] = _allSongs[destinationIndex];
                    _allSongs[destinationIndex] = song;
                }
            }
        }
    }
}
