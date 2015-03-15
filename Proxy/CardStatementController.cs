using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Web.Http;

namespace Proxy
{
    public class CardStatementController : ApiController
    {
        public HttpResponseMessage Post(CardStatementDto model)
        {
            var sslThumbprint = ConfigurationManager.AppSettings["PCI_DSS_Ssl_Thumbprint"];
            var sslServer = ConfigurationManager.AppSettings["PCI_DSS_Ssl_Server"];
            var serviceUrl = ConfigurationManager.AppSettings["PCI_DSS_ServiceUrl"];
            int serviceTimeout;

            if (!int.TryParse(ConfigurationManager.AppSettings["PCI_DSS_ServiceTimeout"], out serviceTimeout))
                serviceTimeout = 10;

            var client = new SslCertificateWebClient(new SslCertificate(sslServer, sslThumbprint));

            return SendRequest(client, serviceUrl, model, serviceTimeout);

        }

        private HttpResponseMessage SendRequest(SslCertificateWebClient client, string serviceUrl, CardStatementDto model, int serviceTimeout)
        {
            var parameters = string.Format("login={0}&password={1}&guid={2}&agid={3}&abstract_period={4}",
                model.Login, model.Password, model.Guid, model.Agid, model.Abstract_period);

            var requestInfo = string.Format(
                "User: {0} getting card statement for account {1} on date {2} from '{3}': ", model.Guid,
                model.Agid, model.Abstract_period, serviceUrl);

            try
            {
                var remoteResponse = client.SendPostRequest(serviceUrl, parameters, serviceTimeout);
                return ProcessRemoteResponse(remoteResponse);
            }
            catch (WebException ex)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(string.Format("Request: {0}, Web exception: status: {1}, exception: {2}", requestInfo, ex.Status, ex)),
                    ReasonPhrase = ex.Status.ToString()
                };
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(string.Format("Request: {0}, Unexpected exception: {1}", requestInfo, ex)),
                    ReasonPhrase = "unexpected exception"
                };
            }
        }

        private void SetMessageHeaders(HttpResponseMessage message, WebResponse remoteResponse)
        {
            var contentType = new ContentType(remoteResponse.ContentType);

            message.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType.MediaType.ToLower());

            if (contentType.CharSet != null)
            {
                message.Content.Headers.ContentType.CharSet = contentType.CharSet;
            }
            if (remoteResponse.Headers["Content-Disposition"] != null)
            {
                message.Content.Headers.Add("Content-Disposition", remoteResponse.Headers["Content-Disposition"]);
            }
        }

        private HttpResponseMessage ProcessRemoteResponse(WebResponse remoteResponse)
        {
            if (null == remoteResponse)
                throw new ArgumentNullException("null response received");

            var webResponseStream = new WebResponseStream(remoteResponse);

            var message = new HttpResponseMessage();
            message.Content = new PushStreamContent((Action<Stream, HttpContent, TransportContext>)webResponseStream.WriteToStream);
            SetMessageHeaders(message, remoteResponse);

            return message;
        }

    }
}