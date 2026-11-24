using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GetCNPJ;
using GetCNPJ.Models;
using GetCNPJ.Validation;

namespace PackageValidation
{
    internal static class ConsumerChecks
    {
        internal const string Commit = "a069e0decc0163f78f9788d204189e4b0d8055e0";
        internal const string Numeric = "12345678000195";
        internal const string Alpha = "12ABC34501DE35";

        internal static void Require(bool condition, string check)
        {
            if (!condition) throw new InvalidOperationException(check);
        }

        internal static async Task RunAsync()
        {
            var assembly = typeof(CnpjClient).Assembly;
            Require(assembly.GetName().Name == "GetCNPJ" && assembly.GetName().Version == new Version(1, 0, 0, 0), "Identidade assembly");
            Require(assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion == "1.1.0+" + Commit, "Proveniência do pacote");
            Require(CnpjValidator.ValidateAndNormalize("12.345.678/0001-95") == Numeric, "Validação numérica");
            Require(CnpjValidator.ValidateAndNormalize("12.abc.345/01de-35") == Alpha, "Validação alfanumérica");
            Require(!CnpjValidator.IsValid("12ABC34501DE34"), "DV inválido");

            var scenarios = 1;
            foreach (var provider in new[] { "CNPJWS", "ReceitaWS", "BrasilAPI", "CNPJA" })
            foreach (var cnpj in new[] { Numeric, Alpha })
            foreach (var sync in new[] { false, true })
            {
                using var handler = new FakeHandler((request, _) =>
                {
                    Require(request.RequestUri?.AbsolutePath.EndsWith(cnpj, StringComparison.Ordinal) == true, "URL preserva identificador/letras");
                    return Task.FromResult(Reply(Json(provider, cnpj)));
                });
                using var http = new HttpClient(handler);
                using var client = new CnpjClient(http);
                var result = sync ? client.GetFromProvider(cnpj, provider) : await client.GetFromProviderAsync(cnpj, provider).ConfigureAwait(false);
                var data = Data(result);
                Require(data.Cnpj is string returned && CnpjValidator.ValidateAndNormalize(returned) == cnpj && data.Provedor == provider, "Identidade/mapeamento");
                Require(handler.Calls == 1 && handler.DisposedContents == 1, "Envio único/descarte resposta");
                Require(data.NomeFantasia == null && data.Email == null, "Nullable opcional");
                if (provider == "CNPJWS") CheckRichData(data);
                else Require(data.Endereco?.CodigoIbge == null && data.InscricoesEstaduais.Count == 0 &&
                    data.Simples?.OptanteInformado == null && data.Simples?.OptanteSimeiInformado == null, "Ausência não fabricada");
                scenarios++;
            }

            using (var handler = new FakeHandler((_, _) => Task.FromResult(Reply("{\"estabelecimento\":{\"cnpj\":\"" + Numeric + "\"}}"))))
            using (var http = new HttpClient(handler))
            using (var client = new CnpjClient(http))
            {
                var data = Data(await client.GetFromProviderAsync(Numeric, "CNPJWS").ConfigureAwait(false));
                Require(data.RazaoSocial == null && data.Endereco?.Logradouro == null && data.Simples == null && data.InscricoesEstaduais.Count == 0, "Resposta parcial");
                scenarios++;
            }

            using (var handler = new FakeHandler((_, _) => Task.FromResult(Reply(Json("CNPJWS", Numeric)))))
            using (var http = new HttpClient(handler))
            {
                var client = new CnpjClient(http);
                Data(client.Get(Numeric));
                Data(await client.GetAsync(Numeric).ConfigureAwait(false));
                client.Dispose(); client.Dispose();
                Require(!handler.Disposed && handler.Calls == 2 && handler.DisposedContents == 2, "Cliente externo preservado/reutilização");
                using (var other = new CnpjClient(http)) Data(other.Get(Numeric));
                Require(handler.Calls == 3, "Mesmo HttpClient após Dispose do wrapper");
                http.Dispose();
                Require(handler.Disposed, "Proprietário descarta transporte");
                scenarios++;
            }
            using (var own = new CnpjClient()) { own.Dispose(); own.Dispose(); scenarios++; } // Sem consulta.

            using (var handler = new FakeHandler((_, _) => Task.FromResult(Reply(Json("CNPJWS", Numeric)))))
            using (var http = new HttpClient(handler))
            using (var client = new CnpjClient(http))
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                await Canceled(client.GetAsync(Numeric, cancellation.Token)).ConfigureAwait(false);
                Require(handler.Calls == 0, "Cancelamento prévio sem envio");
                scenarios++;
            }

            var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var handler = new FakeHandler(async (_, token) =>
            {
                entered.TrySetResult(true);
                await Task.Delay(Timeout.Infinite, token).ConfigureAwait(false);
                throw new InvalidOperationException("Transporte não deveria concluir");
            }))
            using (var http = new HttpClient(handler))
            using (var client = new CnpjClient(http))
            using (var cancellation = new CancellationTokenSource())
            {
                var query = client.GetAsync(Numeric, cancellation.Token);
                await entered.Task.ConfigureAwait(false);
                cancellation.Cancel();
                await Canceled(query).ConfigureAwait(false);
                Require(handler.Calls == 1, "Cancelamento durante HTTP sem fallback");
                scenarios++;
            }

            using (var handler = new FakeHandler(async (request, token) =>
            {
                if (request.RequestUri?.Host == "publica.cnpj.ws")
                    await Task.Delay(Timeout.Infinite, token).ConfigureAwait(false);
                return Reply(Json("ReceitaWS", Numeric));
            }))
            using (var http = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(100) })
            using (var client = new CnpjClient(http))
            {
                var data = Data(await client.GetAsync(Numeric).ConfigureAwait(false));
                Require(data.Provedor == "ReceitaWS" && handler.Calls == 2, "Timeout HTTP permite próximo provedor");
                var directed = await client.GetFromProviderAsync(Numeric, "CNPJWS").ConfigureAwait(false);
                Require(!directed.Success && directed.Errors.Count == 1 && handler.Calls == 3, "Timeout dirigido sem fallback");
                scenarios += 2;
            }
            Console.WriteLine("APROVADO: " + scenarios + " cenários; pacote " + assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion + "; zero HTTP real.");
        }

        internal static async Task UiQueryAsync()
        {
            using var handler = new FakeHandler(async (_, token) =>
            {
                await Task.Delay(10, token).ConfigureAwait(false);
                return Reply(Json("CNPJWS", Alpha));
            });
            using var http = new HttpClient(handler);
            using var client = new CnpjClient(http);
            Data(await client.GetAsync(Alpha)); // Captura contexto WinForms, como um evento de formulário.
        }

        private static async Task Canceled(Task<CnpjResult> operation)
        {
            try { await operation.ConfigureAwait(false); }
            catch (OperationCanceledException) { return; }
            throw new InvalidOperationException("Cancelamento esperado");
        }

        private static CnpjData Data(CnpjResult result) => result.Success && result.Data != null
            ? result.Data : throw new InvalidOperationException("Esperado sucesso com dados sintéticos");

        private static void CheckRichData(CnpjData data)
        {
            var draft = new CustomerDraft(data, "sp");
            Require(draft.Cnpj == CnpjValidator.ValidateAndNormalize(data.Cnpj ?? throw new InvalidOperationException("Identidade")) &&
                draft.CodigoIbge == "3550308" && draft.InscricoesNaUf.Count == 1 && draft.InscricoesNaUf[0].AtivoInformado == null &&
                draft.NomeFantasia == null && draft.Simples == true && draft.Mei == false, "Rascunho cadastro/UF/nullable");
            Require(data.Endereco?.Logradouro == "RUA TESTE" && data.Endereco.Cep == "00123-456" && data.Endereco.CodigoIbge == "3550308", "Endereço/CEP/IBGE");
            Require(data.AtividadePrincipal?.Codigo == "01.11-3-01" && data.AtividadesSecundarias.Single().Codigo == "01.11-3-02", "CNAEs/zeros");
            var sp = data.InscricoesEstaduais.Where(ie => ie.Estado == "SP").ToList();
            Require(sp.Count == 1 && sp[0].AtivoInformado == null && data.InscricoesEstaduais.Single(ie => ie.Estado == "RJ").AtivoInformado == false, "IE por UF/desconhecido versus negativo");
            Require(data.Simples?.OptanteInformado == true && data.Simples.OptanteSimeiInformado == false && data.CapitalSocial == 1234.56m, "Simples/MEI/capital");
        }

        private static string Json(string provider, string cnpj)
        {
            if (provider == "CNPJWS") return "{\"razao_social\":\"EMPRESA SINTETICA\",\"capital_social\":\"1234.56\",\"simples\":{\"simples\":\"Sim\",\"mei\":\"Não\"},\"estabelecimento\":{\"cnpj\":\"" + cnpj + "\",\"tipo_logradouro\":\"RUA\",\"logradouro\":\"TESTE\",\"cep\":\"00123456\",\"cidade\":{\"nome\":\"MUNICIPIO SINTETICO\",\"ibge_id\":3550308},\"estado\":{\"sigla\":\"SP\"},\"atividade_principal\":{\"subclasse\":\"0111301\"},\"atividades_secundarias\":[{\"subclasse\":\"0111302\"}],\"inscricoes_estaduais\":[{\"inscricao_estadual\":\"SINTETICA-SP\",\"ativo\":null,\"estado\":{\"sigla\":\"SP\"}},{\"inscricao_estadual\":\"SINTETICA-RJ\",\"ativo\":false,\"estado\":{\"sigla\":\"RJ\"}}]}}";
            if (provider == "CNPJA") return "{\"taxId\":\"" + cnpj + "\",\"company\":{\"name\":\"EMPRESA SINTETICA\"}}";
            return "{\"cnpj\":\"" + cnpj + "\",\"" + (provider == "BrasilAPI" ? "razao_social" : "nome") + "\":\"EMPRESA SINTETICA\"}";
        }

        private static HttpResponseMessage Reply(string json) => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };

        private sealed class FakeHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _reply;
            internal int Calls;
            internal int DisposedContents;
            internal bool Disposed;
            internal FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) => _reply = reply;
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Calls++;
                var response = await _reply(request, cancellationToken).ConfigureAwait(false);
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                response.Content.Dispose();
                response.Content = new CountingContent(json, () => DisposedContents++);
                return response;
            }
            protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
        }
        private sealed class CountingContent : StringContent
        {
            private readonly Action _disposed;
            internal CountingContent(string json, Action disposed) : base(json) => _disposed = disposed;
            protected override void Dispose(bool disposing) { if (disposing) _disposed(); base.Dispose(disposing); }
        }
    }
}
