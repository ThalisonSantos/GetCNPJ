using System.Net;
using GetCNPJ.Models;

namespace GetCNPJ.Homologation;

internal static class SelfTest
{
    // Só fixtures sintéticos. Nenhum HttpClientHandler de rede é criado neste caminho.
    internal static async Task<int> RunAsync()
    {
        const string cnpj = "12345678000195";
        var count = 0;
        foreach (var mode in new[] { "sync", "async" })
        foreach (var provider in new[] { "CNPJWS", "ReceitaWS", "BrasilAPI", "CNPJA" })
        {
            using var cancellation = new CancellationTokenSource();
            using var transport = new FakeHandler(_ => Reply(HttpStatusCode.OK, Json(provider)));
            using var trace = new TraceHandler(transport, Endpoints.For(cnpj, provider), cancellation);
            using var http = new HttpClient(trace);
            using var client = new CnpjClient(http);
            var result = await Program.QueryAsync(client, new Options(cnpj, provider, mode, null, false), cnpj, cancellation.Token);
            Require(result.Success && Report.IdentityMatches(cnpj, result.Data), "Identidade/sucesso");
            Require(trace.Attempts.Count == 1 && transport.Calls == 1, "Orçamento");
            var writer = new StringWriter();
            Report.Write(writer, cnpj, null, mode, result, null, trace, TimeSpan.Zero);
            Require(!writer.ToString().Contains("SEGREDO_SINTETICO"), "Redação");
            count++;
        }
        foreach (var mode in new[] { "sync", "async" })
        foreach (var status in new[] { HttpStatusCode.TooManyRequests, HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized, HttpStatusCode.Redirect })
        {
            using var cancellation = new CancellationTokenSource();
            using var transport = new FakeHandler(_ => Reply(status, "SEGREDO_SINTETICO"));
            using var trace = new TraceHandler(transport, Endpoints.For(cnpj, "auto"), cancellation);
            using var http = new HttpClient(trace);
            using var client = new CnpjClient(http);
            try { await Program.QueryAsync(client, new Options(cnpj, "auto", mode, null, false), cnpj, cancellation.Token); }
            catch (OperationCanceledException) { }
            Require(transport.Calls == 1 && trace.Stopped != null, "Parar bloqueio sem fallback HTTP");
            count++;
        }
        foreach (var mode in new[] { "sync", "async" })
        {
            using var cancellation = new CancellationTokenSource();
            using var transport = new FakeHandler(request => request.RequestUri?.Host == "publica.cnpj.ws"
                ? Reply(HttpStatusCode.ServiceUnavailable, "SEGREDO_SINTETICO") : Reply(HttpStatusCode.OK, Json("ReceitaWS")));
            using var trace = new TraceHandler(transport, Endpoints.For(cnpj, "auto"), cancellation);
            using var http = new HttpClient(trace);
            using var client = new CnpjClient(http);
            var result = await Program.QueryAsync(client, new Options(cnpj, "auto", mode, null, false), cnpj, cancellation.Token);
            Require(result.Success && result.Data?.Provedor == "ReceitaWS" && transport.Calls == 2, "Fallback ordenado");
            count++;
        }
        var issue = new CnpjData { Cnpj = "12ABC34501DE35", Endereco = new Endereco { Cep = "erro", CodigoIbge = "1234" }, CapitalSocial = -1,
            Simples = new SimplesNacional { OptanteInformado = false, OptanteSimeiInformado = true } };
        Require(Report.Check(cnpj, issue, null).Count == 5, "Checagens de consistência");
        Require(!Options.Parse(new[] { "--cnpj", cnpj }).Execute, "Sem rede por padrão");
        Console.WriteLine($"SELF-TEST APROVADO: {count + 2} cenários locais; zero requisições de rede.");
        return 0;
    }

    private static string Json(string provider) => provider switch
    {
        "CNPJWS" => """{"razao_social":"EMPRESA SINTETICA","estabelecimento":{"cnpj":"12345678000195","email":"SEGREDO_SINTETICO","telefone1":"SEGREDO_SINTETICO"},"socios":[{"nome":"SEGREDO_SINTETICO"}]}""",
        "CNPJA" => """{"taxId":"12345678000195","company":{"name":"EMPRESA SINTETICA","members":[{"person":{"name":"SEGREDO_SINTETICO"}}]},"emails":[{"address":"SEGREDO_SINTETICO"}]}""",
        "BrasilAPI" => """{"cnpj":"12345678000195","razao_social":"EMPRESA SINTETICA","email":"SEGREDO_SINTETICO","qsa":[{"nome_socio":"SEGREDO_SINTETICO"}]}""",
        _ => """{"cnpj":"12345678000195","nome":"EMPRESA SINTETICA","email":"SEGREDO_SINTETICO","qsa":[{"nome":"SEGREDO_SINTETICO"}]}"""
    };
    private static HttpResponseMessage Reply(HttpStatusCode status, string json) => new(status) { Content = new StringContent(json) };
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        internal int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            return Task.FromResult(respond(request));
        }
    }
}
