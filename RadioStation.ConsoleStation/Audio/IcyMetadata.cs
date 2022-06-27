using System;
using System.Collections.Generic;
using System.Text;

namespace RadioStation.ConsoleStation.Audio
{
    public class IcyMetadata : IEquatable<IcyMetadata>
    {
        public string Title { get; private set; }
        public string Url { get; private set; }
        public byte[] RawData { get; private set; }

        public IcyMetadata(string title, string url)
        {
            Title = title;
            Url = url;
            RawData = GetRawData();
        }

        private byte[] GetRawData()
        {
            List<byte> rawData = new List<byte>();

            string data = $"StreamTitle='{Title}';StreamUrl='{Url}';";
            byte[] dataBytes = Encoding.Default.GetBytes(data);

            int totalSize = (int)Math.Ceiling(dataBytes.Length / 16f);
            rawData.Add((byte)totalSize);
            rawData.AddRange(dataBytes);
            rawData.AddRange(new byte[16 * totalSize - dataBytes.Length]);

            return rawData.ToArray();
        }

        public bool Equals(IcyMetadata other)
        {
            return Title == other.Title && Url == other.Url;
        }
    }
}
