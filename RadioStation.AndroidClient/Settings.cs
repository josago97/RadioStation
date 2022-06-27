using System;
using System.IO;
using Android.App;
using Android.Graphics;
using Sharplus.System;

namespace RadioStation.AndroidClient
{
    public static class Settings
    {
        public static readonly string RadioUrl;
        public static readonly Bitmap Artwork;

        static Settings()
        {
            using Stream stream = Application.Context.Assets.Open("Environment.env");
            EnvironmentUtils.LoadVariables(stream);

            RadioUrl = Environment.GetEnvironmentVariable("RADIO_URL");
            Artwork = GetArtwork();
        }

        private static Bitmap GetArtwork()
        {
            Bitmap appIcon = BitmapFactory.DecodeResource(Application.Context.Resources, Resource.Mipmap.ic_iconodesdentao);

            Bitmap artwork = Bitmap.CreateBitmap(appIcon.Width, appIcon.Height, appIcon.GetConfig());
            Canvas canvas = new Canvas(artwork);
            canvas.DrawColor(Color.White);
            canvas.DrawBitmap(appIcon, 0F, 0F, null);

            return artwork;
        }
    }
}