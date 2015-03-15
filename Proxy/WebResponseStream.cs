using System;
using System.IO;
using System.Net;
using System.Net.Http;

namespace Proxy
{
    public class WebResponseStream
    {
        private readonly WebResponse _remoteResponse;
        private const int BufferSize = 65536;

        public WebResponseStream(WebResponse remoteResponse)
        {
            _remoteResponse = remoteResponse;
        }

        public async void WriteToStream(Stream outputStream, HttpContent content, TransportContext context)
        {
            try
            {
                using (var remoteStream = _remoteResponse.GetResponseStream())
                {
                    if (remoteStream == null)
                        throw new ArgumentNullException("null remote stream");

                    var buffer = new byte[BufferSize];
                    var loopCount = 0;

                    while (true)
                    {
                        var read = await remoteStream.ReadAsync(buffer, 0, BufferSize);

                        if (read <= 0)
                            break;

                        await outputStream.WriteAsync(buffer, 0, read);

                        loopCount++;
                        if (loopCount%32 == 0)
                            await outputStream.FlushAsync();
                    }

                    buffer = null;
                }

            }
            finally
            {
                outputStream.Close();
                _remoteResponse.Close();
            }
        }


    }
}
