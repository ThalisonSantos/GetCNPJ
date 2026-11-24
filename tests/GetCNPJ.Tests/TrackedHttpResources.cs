using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GetCNPJ.Tests
{
    internal sealed class TrackedHttpResponse : HttpResponseMessage
    {
        public int DisposeCalls { get; private set; }
        public TrackedContent TrackedContent { get; }

        public TrackedHttpResponse(HttpStatusCode status, string json)
            : this(status, new TrackedContent(json))
        {
        }

        public TrackedHttpResponse(HttpStatusCode status, TrackedContent content) : base(status)
        {
            TrackedContent = content;
            Content = TrackedContent;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) DisposeCalls++;
            base.Dispose(disposing);
        }
    }

    internal class TrackedContent : StreamContent
    {
        public int DisposeCalls { get; private set; }
        public TrackedStream Source { get; }
        public TrackedContent(string text) : this(new TrackedStream(Encoding.UTF8.GetBytes(text))) { }
        private TrackedContent(TrackedStream source) : base(source) { Source = source; }
        protected override void Dispose(bool disposing)
        {
            if (disposing) DisposeCalls++;
            base.Dispose(disposing);
        }
    }

    internal sealed class FailingTrackedContent : TrackedContent
    {
        public FailingTrackedContent() : base("conteúdo simulado") { }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            return Task.FromException(new IOException("falha de leitura simulada"));
        }
    }

    internal sealed class TrackedStream : MemoryStream
    {
        public bool IsDisposed { get; private set; }
        public TrackedStream(byte[] bytes) : base(bytes) { }
        protected override void Dispose(bool disposing)
        {
            if (disposing) IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    internal sealed class TrackedHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;
        public int Requests { get; private set; }
        public int DisposeCalls { get; private set; }
        public TrackedHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) { _respond = respond; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests++;
            return Task.FromResult(_respond(request));
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) DisposeCalls++;
            base.Dispose(disposing);
        }
    }

    internal sealed class TrackedHttpClient : HttpClient
    {
        public int DisposeCalls { get; private set; }
        public TrackedHttpClient(HttpMessageHandler handler) : base(handler) { }
        protected override void Dispose(bool disposing)
        {
            if (disposing) DisposeCalls++;
            base.Dispose(disposing);
        }
    }
}
