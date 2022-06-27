using Android.App;
using Android.Content;
using Android.OS;
using System;

namespace RadioStation.AndroidClient.Media
{
    public class ServiceConnector<T> : Java.Lang.Object, IServiceConnection where T : Service
    {
        public event Action<ComponentName, ServiceBinder<T>> ServiceConnected;
        public event Action<ComponentName> ServiceDisconnected;

        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            ServiceConnected?.Invoke(name, (ServiceBinder<T>)service);
        }

        public void OnServiceDisconnected(ComponentName name)
        {
            ServiceDisconnected?.Invoke(name);
        }
    }
}