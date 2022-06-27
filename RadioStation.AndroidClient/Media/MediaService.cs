using System;
using Android.App;
using Android.Content;
using Android.Media;
using Android.Media.Session;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Media;
using Android.Support.V4.Media.Session;
using AndroidX.Media;
using AndroidX.Media.Session;
using RadioStation.AndroidClient.Utils;
using static Android.Media.AudioManager;
using static Android.Media.Browse.MediaBrowser;

namespace RadioStation.AndroidClient.Media
{
    [Service(Enabled = true, Exported = true)]
    [IntentFilter(new[] { SERVICE_NAME, MEDIA_INTENT })]
    public class MediaService : MediaBrowserServiceCompat, IOnAudioFocusChangeListener
    {
        private const string SERVICE_NAME = ServiceInterface;
        private const string MEDIA_INTENT = Intent.ActionMediaButton;

        private WifiManager _wifiManager;
        private WifiManager.WifiLock _wifiLock;
        private PowerManager _powerManager;
        private PowerManager.WakeLock _wakeLock;
        private AudioManager _audioManager;
        private AudioFocusRequestClass _audioFocusRequest;
        private MediaPlayer _player;

        public MediaSessionCompat MediaSession { get; private set; }
        public NotificationHandler NotificationHandler { get; private set; }
        public MediaController Controller { get; private set; }

        public override void OnCreate()
        {
            base.OnCreate();

            InitMediaSession();
            InitPlayer();

            _wifiManager = (WifiManager)GetSystemService(WifiService);
            _powerManager = (PowerManager)GetSystemService(PowerService);
            _audioManager = (AudioManager)GetSystemService(AudioService);

            NotificationHandler = new NotificationHandler(this);

            Logger.LogInfo($"{nameof(MediaService)} created");
        }

        public override IBinder OnBind(Intent intent)
        {
            return new ServiceBinder<MediaService>(this);
        }

        public override BrowserRoot OnGetRoot(string clientPackageName, int clientUid, Bundle rootHints)
        {
            return new BrowserRoot(nameof(AudioService), null);
        }

        public override void OnLoadChildren(string parentId, Result result)
        {
            JavaList<MediaItem> mediaItems = new JavaList<MediaItem>();
            result.SendResult(mediaItems);
        }

        private void InitMediaSession()
        {
            MediaSession = new MediaSessionCompat(this, PackageName);

            Intent intent = PackageManager?.GetLaunchIntentForPackage(PackageName);
            PendingIntent pendingIntent = PendingIntent.GetActivity(ApplicationContext, 0, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
            MediaSession.SetSessionActivity(pendingIntent);

            SessionToken = MediaSession.SessionToken;
        }

        private void InitPlayer()
        {
            Controller = new MediaController(this);
            _player = new MediaPlayer(this);
            MediaSession.SetCallback(new Callback(_player));
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            StartCommandResult result = base.OnStartCommand(intent, flags, startId);

            Logger.LogInfo($"{nameof(MediaService)}: Intent recibido {intent.Action}");

            if (intent.Action != MEDIA_INTENT && long.TryParse(intent.Action, out long action))
            {
                var mediaControls = MediaSession.Controller.GetTransportControls();

                switch (action)
                {
                    case PlaybackState.ActionPlay:
                        mediaControls.Play();
                        break;

                    case PlaybackState.ActionPause:
                        mediaControls.Pause();
                        break;

                    case PlaybackState.ActionStop:
                        mediaControls.Stop();
                        break;
                }
            }
            else if (intent.Action == MEDIA_INTENT)
            {
                MediaButtonReceiver.HandleIntent(MediaSession, intent);
            }
            else if (!NotificationHandler.NotificationIsActive)
            {
                // En Android un servicio que se cree en background debe mostrar una notificación
                NotificationHandler.ShowTemporalNotification();
            }

            return result;
        }

        private void AcquireWifiAndWake()
        {
            if (_wifiLock == null || !_wifiLock.IsHeld)
            {
                _wifiLock = _wifiManager.CreateWifiLock(WifiMode.FullHighPerf, "xamarin_wifi_lock");
                _wifiLock.Acquire();
            }

            if (_wakeLock == null || !_wakeLock.IsHeld)
            {
                _wakeLock = _powerManager.NewWakeLock(WakeLockFlags.Partial | WakeLockFlags.AcquireCausesWakeup, "xamarin_wake_lock");
                _wakeLock.Acquire();
            }
        }

        private void ReleaseWifiAndWake()
        {
            _wifiLock?.Release();
            _wifiLock = null;

            _wakeLock?.Release();
            _wakeLock = null;
        }

        private bool RequestAudioFocus()
        {
            _audioFocusRequest ??= new AudioFocusRequestClass.Builder(AudioFocus.Gain)
                   .SetOnAudioFocusChangeListener(this)
                   .Build();

            AudioFocusRequest audioFocus = _audioManager.RequestAudioFocus(_audioFocusRequest);

            return audioFocus == AudioFocusRequest.Granted;
        }

        private void ReleaseAudioFocus()
        {
            if (_audioFocusRequest != null)
                _audioManager.AbandonAudioFocusRequest(_audioFocusRequest);
        }

        public void OnAudioFocusChange([GeneratedEnum] AudioFocus focusChange)
        {
            _player.OnAudioFocusChanged(focusChange);
        }

        private void UpdateNotification()
        {
            bool isForeground = _player.IsPlaying;

            NotificationHandler.UpdateNotification();

            if (!isForeground)
            {
                StopForeground(false);
            }
        }

        public override void OnDestroy()
        {
            _player.Dispose();
            UnregisterMediaSession();
            base.OnDestroy();
        }

        private void UnregisterMediaSession()
        {
            if (MediaSession != null)
            {
                MediaSession.Active = false;
                MediaSession.Release();
                MediaSession = null;
            }
        }

        private class Callback : MediaSessionCompat.Callback
        {
            private MediaPlayer _player;

            public Callback(MediaPlayer player)
            {
                _player = player;
            }

            public override void OnPlay()
            {
                base.OnPlay();

                Logger.LogInfo($"{nameof(MediaService)}: Play");
                _player.Play();
            }

            public override void OnPause()
            {
                base.OnPause();

                Logger.LogInfo($"{nameof(MediaService)}: Pause");
                _player.Pause();
            }

            public override void OnStop()
            {
                base.OnStop();

                Logger.LogInfo($"{nameof(MediaService)}: Stop");
                _player.Stop();
            }
        }

        public class MediaController
        {
            private readonly MediaService _mediaService;

            public PlayerState State { get; private set; }
            public MediaMetadata Metadata { get; private set; }
            public MediaSessionCompat MediaSession => _mediaService.MediaSession;
            public MediaControllerCompat.TransportControls Controls => MediaSession.Controller.GetTransportControls();

            public event Action<MediaMetadata> MetadataUpdated;
            public event Action<PlayerState> StateUpdated;

            public MediaController(MediaService mediaService)
            {
                _mediaService = mediaService;
            }

            public void SetMetadata(MediaMetadata metadata)
            {
                Metadata = metadata;
                MetadataUpdated?.Invoke(metadata);
            }

            public void SetState(PlayerState state)
            {
                State = state;
                StateUpdated?.Invoke(state);
            }
        }

        private enum PauseReason { None, FocusLost, FocusLostTransient, User };

        private class MediaPlayer : IDisposable
        {
            private const float VOLUME_DUCK_FOCUS = 0.1f;

            private MediaService _mediaService;
            private RadioPlayer _player;
            private BecomingNoisyReceiver _noisyReceiver;
            private PauseReason _pauseReason;

            public MediaSessionCompat MediaSession => _mediaService.MediaSession;
            public float Volume { get => _player.Volume; set => _player.Volume = value; }
            public bool IsPlaying => _player.IsPlaying;

            public MediaPlayer(MediaService mediaService)
            {
                _mediaService = mediaService;
                _noisyReceiver = new BecomingNoisyReceiver(mediaService, MediaSession.SessionToken);
                _player = new RadioPlayer(mediaService);

                _player.MetadataUpdated += _ => UpdateMediaSession();
                _player.StateChanged += _ => UpdateMediaSession();

                SetMetadata(null);
                SetState(PlayerState.None);
            }

            public void PlayPause()
            {
                if (_player.IsPlaying) Pause();
                else Play();
            }

            public void Play()
            {
                if (_player.State == PlayerState.Playing) return;

                if (_mediaService.RequestAudioFocus())
                {
                    _mediaService.MediaSession.Active = true;
                    _noisyReceiver.Register();
                    _mediaService.AcquireWifiAndWake();
                    _player.Play();
                    _pauseReason = PauseReason.None;
                }
            }

            public void Pause(PauseReason reason = PauseReason.User)
            {
                if (_player.State == PlayerState.Paused) return;

                _player.Pause();

                if (reason != PauseReason.FocusLostTransient)
                {
                    _mediaService.ReleaseAudioFocus();
                }

                _noisyReceiver.Unregister();
                _mediaService.ReleaseWifiAndWake();

                _pauseReason = reason;
            }

            public void Stop()
            {
                if (_player.State == PlayerState.Stopped) return;

                _player.Stop();
                _mediaService.ReleaseAudioFocus();
                _mediaService.MediaSession.Active = false;
                _noisyReceiver.Unregister();
                _mediaService.ReleaseWifiAndWake();
            }

            public void OnAudioFocusChanged(AudioFocus focusChange)
            {
                switch (focusChange)
                {
                    case AudioFocus.Gain:
                        OnGainFocus();
                        break;

                    case AudioFocus.Loss:
                        OnLossFocus();
                        break;

                    case AudioFocus.LossTransient:
                        OnLossTransientFocus();
                        break;

                    case AudioFocus.LossTransientCanDuck:
                        OnLossTransientCanDuckFocus();
                        break;
                }
            }

            private void OnGainFocus()
            {
                if (_pauseReason != PauseReason.User)
                {
                    Play();
                }

                _player.Volume = 1;
            }

            private void OnLossFocus()
            {
                //We have lost focus stop!
                Pause(PauseReason.FocusLost);
            }

            private void OnLossTransientFocus()
            {
                //We have lost focus for a short time, but likely to resume so pause
                Pause(PauseReason.FocusLostTransient);
            }

            private void OnLossTransientCanDuckFocus()
            {
                //We have lost focus but should till play at a muted % volume
                _player.Volume = VOLUME_DUCK_FOCUS;
            }

            private void UpdateMediaSession()
            {
                SetMetadata(_player.Metadata);
                SetState(_player.State);

                _mediaService.UpdateNotification();
            }

            private void SetMetadata(MediaMetadata metadata)
            {
                MediaSession.SetMetadata(MediaMetadataCompat.FromMediaMetadata(metadata));
                _mediaService.Controller.SetMetadata(metadata);
            }

            private void SetState(PlayerState state)
            {
                long actions = PlaybackStateCompat.ActionPlayPause;

                if (_player.IsPlaying)
                {
                    actions |= PlaybackStateCompat.ActionPause | PlaybackStateCompat.ActionStop;
                }
                else
                {
                    actions |= PlaybackStateCompat.ActionPlay;
                }

                int playbackState = state switch
                {
                    PlayerState.Connecting => PlaybackStateCompat.StatePlaying,
                    PlayerState.Playing => PlaybackStateCompat.StatePlaying,
                    PlayerState.Paused => PlaybackStateCompat.StatePaused,
                    PlayerState.Stopped => PlaybackStateCompat.StateStopped,
                    _ => PlaybackStateCompat.StateNone
                };

                var stateBuilder = new PlaybackStateCompat.Builder()
                    .SetState(playbackState, PlaybackState.PlaybackPositionUnknown, 1)
                    .SetActions(actions);

                MediaSession.SetPlaybackState(stateBuilder.Build());
                _mediaService.Controller.SetState(state);
            }

            public void Dispose()
            {
                try
                {
                    Stop();
                    _player.Dispose();
                }
                catch (Exception e)
                {
                    Logger.LogError($"{nameof(MediaService)} Error: {e}");
                }
            }
        }
    }
}