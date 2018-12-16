using System.Net;

namespace SendiT.Model.SendGrid
{
    public class SendResponse
    {
        public string MessageId { get; set; }

        public HttpStatusCode StatusCode { get; set; }
    }
}
