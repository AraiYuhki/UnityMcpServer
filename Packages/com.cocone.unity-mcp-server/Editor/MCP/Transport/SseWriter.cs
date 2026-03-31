using System;
using System.Net;
using System.Text;

namespace UnityMcp.Transport
{
    /// <summary>
    /// Writes Server-Sent Events to an HttpListenerResponse stream.
    /// Sets appropriate headers and formats events per the SSE specification.
    /// </summary>
    public class SseWriter
    {
        private readonly HttpListenerResponse response;
        private int eventCounter;

        public SseWriter(HttpListenerResponse response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            this.response = response;
            response.ContentType = "text/event-stream";
            response.Headers.Add("Cache-Control", "no-cache");
            response.Headers.Add("Connection", "keep-alive");
        }

        /// <summary>
        /// Writes a single SSE event with auto-incremented id and event type "message".
        /// </summary>
        public void WriteEvent(string data)
        {
            eventCounter++;
            var eventText = $"event: message\nid: {eventCounter}\ndata: {data}\n\n";
            var buffer = Encoding.UTF8.GetBytes(eventText);
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Flush();
        }

        /// <summary>
        /// Closes the underlying response output stream.
        /// </summary>
        public void Close()
        {
            response.OutputStream.Close();
        }
    }
}
