using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Proxy
{
    public class SslCertificateWebClient
    {
        private readonly List<SslCertificate> _knownCertificates;

        public SslCertificateWebClient(SslCertificate sslCertificate)
        {
            _knownCertificates = new List<SslCertificate>()
            {
                sslCertificate
            };
        }

        public WebResponse SendPostRequest(string url, string parameters, int timeout)
        {
            var request = (HttpWebRequest) WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Timeout = timeout*1000;

            if (url.StartsWith("https://"))
            {
                return SendSslRequest(request, parameters);
            }

            return SendRequest(request, parameters);
        }

        private WebResponse SendSslRequest(HttpWebRequest request, string requestBody)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            request.ServerCertificateValidationCallback = RemoteCertificateValidationCallback;

            return SendRequest(request, requestBody);
        }


        private WebResponse SendRequest(HttpWebRequest request, string requestBody)
        {
            var content = Encoding.UTF8.GetBytes(requestBody);

            using (var requestStream = request.GetRequestStream())
            {
                requestStream.Write(content, 0, content.Length);
                requestStream.Flush();
            }

            var response = request.GetResponse();

            return response;
        }

        private bool RemoteCertificateValidationCallback(object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            var request = (HttpWebRequest) sender;
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            bool hasMatch = _knownCertificates.Any(x => x.IsMatch(request.Host, certificate));

            return hasMatch;
        }
    }

    public struct SslCertificate
    {
        public string Host { get; set; }
        public string Thumbprint { get; set; }

        public SslCertificate(string host, string thumbprint) : this()
        {
            Host = host;
            Thumbprint = thumbprint;
        }

        public bool IsMatch(string certificateHost, X509Certificate certificate)
        {
            var certificateThumbprint = string.Join(" ", certificate.GetCertHash().Select(x => x.ToString("x2")));

            return (Host.Equals(certificateHost, StringComparison.InvariantCultureIgnoreCase)) 
                && (Thumbprint.Equals(certificateThumbprint, StringComparison.InvariantCultureIgnoreCase));

        }
    }
}
