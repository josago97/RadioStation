using Android.Util;
using Xamarin.Essentials;

namespace RadioStation.AndroidClient.Utils
{
    public static class Logger
    {
        private static string TAG = AppInfo.Name;

        public static void LogInfo(string tag, string message)
        {
            Log.Info(tag, message);
        }

        public static void LogInfo(string message)
        {
            LogInfo(TAG, message);
        }

        public static void LogWarn(string tag, string message)
        {
            Log.Warn(tag, message);
        }

        public static void LogWarn(string message)
        {
            LogWarn(TAG, message);
        }

        public static void LogError(string tag, string message)
        {
            Log.Error(tag, message);
        }

        public static void LogError(string message)
        {
            LogError(TAG, message);
        }
    }
}