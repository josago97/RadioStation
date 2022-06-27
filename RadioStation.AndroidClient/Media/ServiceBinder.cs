using Android.App;
using Android.OS;

namespace RadioStation.AndroidClient.Media
{
    public class ServiceBinder<T> : Binder where T : Service
    {
        public T Service { get; private set; }

        public ServiceBinder(T service)
        {
            Service = service;
        }
    }
}