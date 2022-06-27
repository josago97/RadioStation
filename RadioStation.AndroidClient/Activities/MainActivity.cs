using System;
using Android.App;
using Android.Content.PM;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using RadioStation.AndroidClient.Media;

namespace RadioStation.AndroidClient.Activities
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, ScreenOrientation = ScreenOrientation.Portrait, LaunchMode = LaunchMode.SingleInstance)]
    public class MainActivity : AppCompatActivity
    {
        private ImageButton _playPauseButton;
        private TextView _infoText;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            _playPauseButton = FindViewById<ImageButton>(Resource.Id.btn_play_pause);
            _infoText = FindViewById<TextView>(Resource.Id.info_text);

            _playPauseButton.Click += OnPlayPauseButtonClick;

            MediaManager.Init();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnResume()
        {
            base.OnResume();
            Subscribe();
            UpdateUI();
        }

        protected override void OnPause()
        {
            base.OnPause();
            Unsubscribe();
        }

        private void Subscribe()
        {
            MediaManager.Controller.MetadataUpdated += OnMetadataUpdated;
            MediaManager.Controller.StateChanged += OnPlayerStateChange;
        }

        private void Unsubscribe()
        {
            MediaManager.Controller.MetadataUpdated -= OnMetadataUpdated;
            MediaManager.Controller.StateChanged -= OnPlayerStateChange;
        }

        private void OnMetadataUpdated(MediaMetadata metadata)
        {
            UpdateUI();
        }

        private void OnPlayerStateChange(PlayerState state)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (MediaManager.Controller.IsPlaying)
            {
                _playPauseButton.SetImageResource(Resource.Mipmap.ic_pause_btn);
                _infoText.Visibility = ViewStates.Visible;

                if (MediaManager.Controller.State == PlayerState.Connecting)
                {
                    _infoText.Text = "Conectando...";
                }
                else if (MediaManager.Controller.IsPlaying)
                {
                    MediaMetadata metadata = MediaManager.Controller.Metadata;

                    if (metadata != null)
                    {
                        string song = metadata.GetString(MediaMetadata.MetadataKeyDisplayTitle);
                        string artist = metadata.GetString(MediaMetadata.MetadataKeyDisplaySubtitle);
                        _infoText.Text = $"{artist} - {song}";
                        _infoText.Selected = true;
                    }
                    else
                    {
                        _infoText.Text = string.Empty;
                    }
                }
            }
            else
            {
                _playPauseButton.SetImageResource(Resource.Mipmap.ic_play_btn);
                _infoText.Visibility = ViewStates.Invisible;
            }
        }

        private void OnPlayPauseButtonClick(object sender, EventArgs e)
        {
            if (MediaManager.Controller.IsPlaying)
            {
                MediaManager.Controller.Pause();
            }
            else
            {
                MediaManager.Controller.Play();
            }
        }
    }
}