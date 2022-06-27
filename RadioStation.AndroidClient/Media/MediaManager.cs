using Android.App;
using Android.Content;
using RadioStation.AndroidClient.Utils;

namespace RadioStation.AndroidClient.Media
{
    public static class MediaManager
    {
        private static bool _isInitialized = false;

        public static MediaController Controller { get; private set; }

        public static void Init()
        {
            if (!_isInitialized)
            {
                Controller = new MediaController();
                StartService();
            }
        }

        private static void StartService()
        {
            Context context = Application.Context;
            Intent intent = new Intent(context, typeof(MediaService));

            ServiceConnector<MediaService> connection = new ServiceConnector<MediaService>();
            connection.ServiceConnected += (component, binder) =>
            {
                Controller.Init(binder.Service.Controller);
            };

            _isInitialized = context.BindService(intent, connection, Bind.AutoCreate);

            Logger.LogInfo($"{nameof(MediaManager)} initialized: {_isInitialized}");
        }
    }
}