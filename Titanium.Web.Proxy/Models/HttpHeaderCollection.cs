using System.Collections.Generic;

namespace Titanium.Web.Proxy.Models
{
    public class HttpHeaderCollection : Dictionary<string, HttpHeader>
    {
        public HttpHeaderCollection()
        {
            
        }

        public HttpHeaderCollection(IEnumerable<HttpHeader> headers)
        {
            if (headers == null)
            {
                return;
            }

            foreach (var httpHeader in headers)
            {
                Add(httpHeader);
            }
        }

        public void Add(HttpHeader header)
        {
            if (header == null)
            {
                return;
            }

            this[header.Name] = header;
        }
    }
}