using Android.Content;
using Android.Media;
using Android.Support.V4.Media.Session;

namespace RadioStation.AndroidClient.Media
{
    public class BecomingNoisyReceiver : BroadcastReceiver
    {
        private static IntentFilter noisyIntentFilter = new IntentFilter(AudioManager.ActionAudioBecomingNoisy);

        private Context _context;
        private MediaControllerCompat _mediaController;
        private bool _isRegisted;

        public BecomingNoisyReceiver(Context context, MediaSessionCompat.Token mediaSession)
        {
            _context = context;
            _mediaController = new MediaControllerCompat(context, mediaSession);
            _isRegisted = false;
        }

        public void Register()
        {
            if (!_isRegisted)
            {
                _context.RegisterReceiver(this, noisyIntentFilter);
                _isRegisted = true;
            }
        }

        public void Unregister()
        {
            if (_isRegisted)
            {
                _context.UnregisterReceiver(this);
                _isRegisted = false;
            }
        }

        public override void OnReceive(Context context, Intent intent)
        {
            _mediaController.GetTransportControls().Stop();
        }
    }
}