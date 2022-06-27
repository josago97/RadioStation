using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Media;

namespace RadioStation.AndroidClient.Media
{
    public class MediaController
    {
        private MediaService.MediaController _player;
        private PlayerState _state;
        private MediaMetadata _metadata;
        private Task _ensureInitTask;

        public PlayerState State 
        {
            get => _state;

            private set
            {
                _state = value;
                StateChanged?.Invoke(_state);
            }
        }

        public MediaMetadata Metadata
        {
            get => _metadata;

            private set
            {
                _metadata = value;
                MetadataUpdated?.Invoke(_metadata);
            }
        }

        public bool IsPlaying => State == PlayerState.Connecting || State == PlayerState.Playing;

        public event Action<PlayerState> StateChanged;
        public event Action<MediaMetadata> MetadataUpdated;

        public MediaController()
        {
            _ensureInitTask = Task.Run(() => SpinWait.SpinUntil(() => _player != null));
        }

        public void Init(MediaService.MediaController player)
        {
            _player = player;

            _player.StateUpdated += s => State = s;
            _player.MetadataUpdated += m => Metadata = m;

            State = player.State;
            Metadata = player.Metadata;
        }

        public async void Play()
        {
            await _ensureInitTask;
            _player.Controls.Play();
        }

        public void Pause()
        {
            Stop();
        }

        public async void Stop()
        {
            await _ensureInitTask;
            _player.Controls.Stop();
        }
    }
}