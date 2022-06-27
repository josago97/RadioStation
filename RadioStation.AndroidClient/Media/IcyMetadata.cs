using System;
using System.Text;
using System.Text.RegularExpressions;

namespace RadioStation.AndroidClient.Media
{
    public class IcyMetadata : IEquatable<IcyMetadata>
    {
        private const string STREAM_KEY_NAME = "streamtitle";
        private const string STREAM_KEY_URL = "streamurl";

        public string StreamTitle { get; private set; }
        public string StreamUrl { get; private set; }

        public IcyMetadata(string title, string url)
        {
            StreamTitle = title;
            StreamUrl = url;
        }

        public IcyMetadata(byte[] rawData)
        {
            ReadData(rawData);
        }

        private void ReadData(byte[] data)
        {
            string metadata = Encoding.Default.GetString(data);

            foreach (Match match in Regex.Matches(metadata, "(.+?)='(.*?)';"))
            {
                string key = match.Groups[1].Value.ToLower();
                string value = match.Groups[2].Value;

                switch (key)
                {
                    case STREAM_KEY_NAME:
                        StreamTitle = value;
                        break;

                    case STREAM_KEY_URL:
                        StreamUrl = value;
                        break;
                }
            }
        }

        public bool Equals(IcyMetadata other)
        {
            return other != null
                && other.StreamTitle == StreamTitle
                && other.StreamUrl == StreamUrl;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IcyMetadata);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator !=(IcyMetadata left, IcyMetadata right) => !(left == right);
        public static bool operator ==(IcyMetadata left, IcyMetadata right) => left switch
        {
            null => right is null,
            _ => left.Equals(right)
        };
    }
}