using System.Linq;
using System.Net;
using System.Threading.Tasks;
using RadioStation.ConsoleStation.Audio;

namespace RadioStation.ConsoleStation
{
    public class WebListener
    {
        private HttpListener _httpListener;
        private Radio _radio;

        public string[] Prefixes { get; private set; }

        public WebListener(Radio radio)
        {
            _radio = radio;

            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add($"http://*:{Settings.Port}/");

            Prefixes = _httpListener.Prefixes.ToArray();
        }

        public void Start()
        {
            _httpListener.Start();
            Task.Run(Listen);
        }

        private void Listen()
        {
            while (true)
            {
                try
                {
                    HttpListenerContext context = _httpListener.GetContext();
                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;

                    //response.SendChunked = true;
                    response.StatusCode = (int)HttpStatusCode.OK;
                    response.ContentType = "audio/mpeg";
                    response.AddHeader("Cache-control", "no-cache");
                    response.AddHeader("Pragma", "no-cache");

                    bool requireMetadata = AddIcyMetadataHeaders(request, response);

                    Listener listener = new Listener(response.OutputStream, requireMetadata);
                    _radio.AddListener(listener);
                }
                catch { }
            }
        }

        private bool AddIcyMetadataHeaders(HttpListenerRequest request, HttpListenerResponse response)
        {
            bool requireMetadata = request.Headers["Icy-MetaData"] == "1";

            response.AddHeader("icy-br", Radio.AUDIO_BIT_RATE.ToString());
            response.AddHeader("icy-genre", Settings.Genre);
            response.AddHeader("icy-name", Settings.Name);
            response.AddHeader("icy-pub", "0");
            response.AddHeader("icy-url", request.UserHostName);

            if (requireMetadata)
            {
                response.AddHeader("icy-metaint", Radio.METADATA_BYTE_RATE.ToString());
            }

            return requireMetadata;
        }
    }
}
