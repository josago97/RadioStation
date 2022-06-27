using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadioStation.ConsoleStation.Audio
{
    public class Listener
    {
        private const int MAX_REQUESTS = 5;
        private const int METADATA_BYTE_RATE = Radio.METADATA_BYTE_RATE;

        private List<byte[]> _requests;
        private IcyMetadata _metadata;
        private CancellationTokenSource _sendCancellationSource;
        private Task _currentSendTask;
        private int _bytesToMetadata;

        public bool IsListening { get; private set; }
        public bool RequireMetadata { get; private set; }
        public Stream Stream { get; private set; }

        public Listener(Stream stream, bool requireMetadata)
        {
            Stream = stream;
            RequireMetadata = requireMetadata;
            IsListening = true;

            _requests = new List<byte[]>();
            _currentSendTask = Task.CompletedTask;
            _bytesToMetadata = METADATA_BYTE_RATE;
        }

        public void UpdateMetadata(IcyMetadata metadata)
        {
            _metadata = metadata;
        }

        public void SendData(byte[] data)
        {
            if (!IsListening) return;

            if (RequireMetadata)
                SendWithMetadata(data);
            else
                Send(data);
        }

        private void SendWithMetadata(byte[] data)
        {
            List<byte> totalData = new List<byte>();
            int bytesToSend = data.Length;
            int offset = 0;

            while (_bytesToMetadata < bytesToSend)
            {
                totalData.AddRange(new ArraySegment<byte>(data, offset, _bytesToMetadata));
                totalData.AddRange(_metadata.RawData);
                bytesToSend -= _bytesToMetadata;
                offset += _bytesToMetadata;
                _bytesToMetadata = METADATA_BYTE_RATE;
            }

            totalData.AddRange(new ArraySegment<byte>(data, offset, bytesToSend));
            _bytesToMetadata -= bytesToSend;

            Send(totalData.ToArray());
        }

        private void Send(byte[] data)
        {
            _requests.Add(data);

            if (_currentSendTask.IsCompleted)
            {
                byte[] totalData = _requests.SelectMany(x => x).ToArray();
                _requests.Clear();
                _sendCancellationSource = new CancellationTokenSource();
                CancellationToken cancellationToken = _sendCancellationSource.Token;

                _currentSendTask = Task.Run(async () =>
                {
                    try
                    {
                        await Stream.WriteAsync(totalData, 0, totalData.Length, cancellationToken);
                        await Stream.FlushAsync(cancellationToken);
                    }
                    catch
                    {
                        IsListening = false;
                    }
                }, cancellationToken);
            }
            else if (_requests.Count > MAX_REQUESTS)
            {
                _sendCancellationSource.Cancel();
            }
        }
    }
}
