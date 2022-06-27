using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media.Session;
using AndroidX.Core.App;
using Xamarin.Essentials;
using static AndroidX.Media.App.NotificationCompat;

namespace RadioStation.AndroidClient.Media
{
    public class NotificationHandler
    {
        private static readonly string NOTIFICATION_CHANNEL_ID = $"{AppInfo.Name}Notifications";
        private const int NOTIFICATION_ID = 1;

        private MediaService _mediaService;
        private Context _context;
        private NotificationManagerCompat _notificationManager;

        public bool NotificationIsActive { get; private set; }

        public NotificationHandler(MediaService mediaService)
        {
            _mediaService = mediaService;
            _context = mediaService;
            _notificationManager = NotificationManagerCompat.From(mediaService);
        }

        public void UpdateNotification()
        {
            //Android es muy chulo y a veces da error porque no encuentra el canal de notificaciones creado en el constructor
            if (_notificationManager.GetNotificationChannel(NOTIFICATION_CHANNEL_ID) == null)
                CreateNotificationChannel();

            _mediaService.StartForeground(NOTIFICATION_ID, CreateNotification());
            NotificationIsActive = true;
        }

        public void ShowTemporalNotification()
        {
            NotificationChannel channel = new NotificationChannel(NOTIFICATION_CHANNEL_ID, $"Temporary", NotificationImportance.Default);

            _notificationManager.CreateNotificationChannel(channel);

            Notification notification = new Notification.Builder(_context, NOTIFICATION_CHANNEL_ID)
                    .SetContentTitle("")
                    .SetContentText("").Build();

            _mediaService.StartForeground(NOTIFICATION_ID, notification);
            _mediaService.StopForeground(true);
        }

        private void CreateNotificationChannel()
        {
            NotificationChannel channel = new NotificationChannel(NOTIFICATION_CHANNEL_ID, $"{AppInfo.Name} Player", NotificationImportance.Low);
            channel.Description = $"{AppInfo.Name} Notifications";
            channel.EnableLights(false);
            channel.EnableVibration(false);
            channel.LockscreenVisibility = NotificationVisibility.Public;

            _notificationManager.CreateNotificationChannel(channel);
        }

        private Notification CreateNotification()
        {
            var intent = _context.PackageManager.GetLaunchIntentForPackage(_context.PackageName);
            var pendingIntent = PendingIntent.GetActivity(_context, NOTIFICATION_ID, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            var style = new MediaStyle()
            .SetMediaSession(_mediaService.SessionToken)
            .SetShowActionsInCompactView(0);

            var notificationBuilder = new NotificationCompat.Builder(_context, NOTIFICATION_CHANNEL_ID);
            Bitmap artwork = Settings.Artwork;

            notificationBuilder.SetStyle(style)
                .SetContentTitle(AppInfo.Name)
                .SetSmallIcon(Resource.Mipmap.ic_stat_iconodesdentao)
                .SetLargeIcon(Bitmap.CreateScaledBitmap(artwork, 128, 128, false))
                .SetContentIntent(pendingIntent)
                .SetVisibility((int)NotificationVisibility.Public)
                .SetWhen(Java.Lang.JavaSystem.CurrentTimeMillis())
                .SetOngoing(true);

            PlayerState playerState = _mediaService.Controller.State;
            SetPlaybackState(notificationBuilder, playerState);

            return notificationBuilder.Build();
        }

        private void SetPlaybackState(NotificationCompat.Builder notification, PlayerState state)
        {
            bool isPlaying = state == PlayerState.Playing || state == PlayerState.Connecting;

            if (isPlaying)
            {
                notification.SetUsesChronometer(true);
                notification.AddAction(GenerateActionCompat(Resource.Drawable.exo_icon_pause, "Pause", PlaybackState.ActionPause));

                if (state == PlayerState.Connecting)
                {
                    notification.SetContentText("Conectando...");
                }
            }
            else
            {
                notification.SetUsesChronometer(false);
                notification.SetContentText(string.Empty);
                notification.AddAction(GenerateActionCompat(Resource.Drawable.exo_icon_play, "Play", PlaybackState.ActionPlay));
                notification.SetDeleteIntent(CreateIntent(PlaybackState.ActionStop));
            }
        }

        private NotificationCompat.Action GenerateActionCompat(int icon, string title, long action)
        {
            PendingIntent pendingIntent = CreateIntent(action);

            return new NotificationCompat.Action.Builder(icon, title, pendingIntent).Build();
        }

        private PendingIntent CreateIntent(long action)
        {
            Intent intent = new Intent(_mediaService, _mediaService.GetType());
            intent.SetAction(action.ToString());

            return PendingIntent.GetService(_mediaService, NOTIFICATION_ID, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        }
    }
}