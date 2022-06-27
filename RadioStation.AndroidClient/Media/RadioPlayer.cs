using System;
using System.Text.RegularExpressions;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Com.Google.Android.Exoplayer2;
using Com.Google.Android.Exoplayer2.Metadata;
using Com.Google.Android.Exoplayer2.Metadata.Icy;
using Com.Google.Android.Exoplayer2.Source;
using Com.Google.Android.Exoplayer2.Trackselection;
using Com.Google.Android.Exoplayer2.Upstream;
using Java.IO;
using RadioStation.AndroidClient.Utils;
using Xamarin.Essentials;

namespace RadioStation.AndroidClient.Media
{
    public class RadioPlayer : IDisposable
    {
        private const int METADATA_DURATION = 100; // milliseconds
        private static readonly TimeSpan RETRY_DELAY = TimeSpan.FromSeconds(1);

        private SimpleExoPlayer _player;
        private IDataSourceFactory _dataSourceFactory;

        public PlayerState State { get; private set; }
        public MediaMetadata Metadata { get; private set; }
        public float Volume { get => _player.Volume; set => _player.Volume = value; }
        public bool IsPlaying => State == PlayerState.Playing || State == PlayerState.Connecting;

        public event Action<PlayerState> StateChanged;
        public event Action<MediaMetadata> MetadataUpdated;

        public RadioPlayer(Context context)
        {
            _dataSourceFactory = new DefaultHttpDataSourceFactory(nameof(RadioPlayer));
            _player = new SimpleExoPlayer.Builder(context).Build();

            Listener listener = new Listener();
            listener.StateChanged += OnStateChanged;
            listener.MetadataChanged += OnMetadataUpdated;

            _player.AddListener(listener);
            _player.AddMetadataOutput(listener);
            _player.SetWakeMode((int)WakeLockFlags.Partial);
        }

        public void Play()
        {
            _player.PlayWhenReady = true;
            _player.Prepare(BuildMediaSource(Settings.RadioUrl));


            /*if (State != PlayerState.Playing)
            {
                IsPlaying = true;

                _player.PlayWhenReady = true;
                _player.Prepare(BuildMediaSource(URL));
            }*/
        }

        public void Pause()
        {
            Stop();
        }

        public void Stop()
        {

            _player.Stop(true);
            /*
            if (IsPlaying)
            {
                IsPlaying = false;

                _player.Stop(true);
            }*/
        }

        private IMediaSource BuildMediaSource(string url)
        {
            var uri = Android.Net.Uri.Parse(url);
            return new ProgressiveMediaSource.Factory(_dataSourceFactory)
                .SetLoadErrorHandlingPolicy(new ErrorHandler(RETRY_DELAY))
                .CreateMediaSource(uri);
        }

        private void OnMetadataUpdated(IcyMetadata icyMetadata)
        {
            var metadataBuilder = new MediaMetadata.Builder();

            if (icyMetadata == null)
            {
                metadataBuilder.PutString(MediaMetadata.MetadataKeyTitle, AppInfo.Name)
                    .PutString(MediaMetadata.MetadataKeyDisplayTitle, AppInfo.Name);

                if (State == PlayerState.Connecting)
                {
                    metadataBuilder.PutString(MediaMetadata.MetadataKeyArtist, "Conectando...")
                        .PutString(MediaMetadata.MetadataKeyDisplaySubtitle, "Conectando...");
                }
            }
            else
            {
                string streamTitle = icyMetadata.StreamTitle;
                string artist = Regex.Match(streamTitle, ".*(?= -)").Value.Trim();
                string songName = Regex.Match(streamTitle, "(?<=- ).*").Value.Trim();
                string album = $"{AppInfo.Name} ({icyMetadata.StreamUrl})";
                Bitmap artwork = Settings.Artwork;

                metadataBuilder.PutString(MediaMetadata.MetadataKeyDisplayTitle, songName)
                    .PutString(MediaMetadata.MetadataKeyDisplaySubtitle, artist)
                    .PutString(MediaMetadata.MetadataKeyDisplayDescription, album)
                    .PutString(MediaMetadata.MetadataKeyTitle, songName)
                    .PutString(MediaMetadata.MetadataKeyArtist, artist)
                    .PutString(MediaMetadata.MetadataKeyAlbum, album)
                    .PutBitmap(MediaMetadata.MetadataKeyArt, artwork)
                    .PutLong(MediaMetadata.MetadataKeyDuration, METADATA_DURATION);
            }

            Metadata = metadataBuilder.Build();
            MetadataUpdated?.Invoke(Metadata);
        }

        private void OnStateChanged()
        {
            PlayerState state = _player.PlaybackState switch
            {
                IPlayer.StateBuffering => PlayerState.Connecting,
                IPlayer.StateReady => PlayerState.Playing,
                _ => PlayerState.Stopped
            };

            if (State != state)
            {
                State = state;
                StateChanged?.Invoke(state);
            }
        }

        public void Dispose()
        {
            _player.Release();
        }

        private class Listener : Java.Lang.Object, IPlayerEventListener, IMetadataOutput
        {
            private IcyMetadata _icyMetadata;

            public event Action StateChanged;
            public event Action<IcyMetadata> MetadataChanged;

            private void UpdateMetadata(IcyMetadata icyMetadata)
            {
                if (_icyMetadata != icyMetadata)
                {
                    _icyMetadata = icyMetadata;
                    MetadataChanged?.Invoke(icyMetadata);
                }
            }

            public void OnIsPlayingChanged(bool isPlaying)
            {
                if (!isPlaying) UpdateMetadata(null);
            }

            public void OnLoadingChanged(bool isLoading) { }

            public void OnMetadata(Metadata metadata)
            {
                IcyInfo icyInfo = metadata.Get(0) as IcyInfo;

                if (icyInfo != null)
                {
                    IcyMetadata icyMetadata = new IcyMetadata(icyInfo.Title, icyInfo.Url);

                    UpdateMetadata(icyMetadata);
                }
            }

            public void OnPlaybackParametersChanged(PlaybackParameters playbackParameters) { }
            public void OnPlaybackSuppressionReasonChanged(int playbackSuppressionReason) { }
            public void OnPlayerError(ExoPlaybackException error) 
            {
                Logger.LogError($"{nameof(RadioPlayer)} {nameof(OnPlayerError)}: {error}");
            }

            public void OnPlayerStateChanged(bool playWhenReady, int playbackState)
            {
                StateChanged?.Invoke();
            }

            public void OnPositionDiscontinuity(int reason) { }
            public void OnRepeatModeChanged(int repeatMode) { }
            public void OnSeekProcessed() { }
            public void OnShuffleModeEnabledChanged(bool shuffleModeEnabled) { }
            public void OnTimelineChanged(Timeline timeline, int reason) { }
            public void OnTracksChanged(TrackGroupArray trackGroups, TrackSelectionArray trackSelections) { }
        }

        private class ErrorHandler : DefaultLoadErrorHandlingPolicy
        {
            public TimeSpan RetryDelay { get; private set; }

            public ErrorHandler(TimeSpan retryDelay)
            {
                RetryDelay = retryDelay;
            }

            public override long GetBlacklistDurationMsFor(int dataType, long loadDurationMs, IOException exception, int errorCount)
            {
                return C.TimeUnset;
            }

            public override long GetRetryDelayMsFor(int dataType, long loadDurationMs, IOException exception, int errorCount)
            {
                return RetryDelay.Milliseconds;
            }

            public override int GetMinimumLoadableRetryCount(int dataType)
            {
                return int.MaxValue;
            }
        }
    }
}