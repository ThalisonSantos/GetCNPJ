using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GetCNPJ.Tests
{
    internal sealed class AsyncStubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _respond;
        private int _requestCount;
        public int RequestCount => Volatile.Read(ref _requestCount);

        public AsyncStubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
        {
            _respond = respond;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _requestCount);
            return _respond(request, cancellationToken);
        }
    }
}
