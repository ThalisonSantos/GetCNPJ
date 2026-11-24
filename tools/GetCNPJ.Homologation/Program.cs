using System.Diagnostics;
using System.Text;
using GetCNPJ.Validation;

namespace GetCNPJ.Homologation;

internal static class Program
{
    internal static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        try
        {
            if (args.SequenceEqual(new[] { "--self-test" })) return await SelfTest.RunAsync();
            if (args.Length == 0 || args.Contains("--help"))
            {
                Console.WriteLine("Homologação manual GetCNPJ Community (sem consultas por padrão).");
                Console.WriteLine("--cnpj <CNPJ> --provider auto|CNPJWS|ReceitaWS|BrasilAPI|CNPJA --mode async|sync");
                Console.WriteLine("[--expected-name <razão social>] [--execute]");
                Console.WriteLine("--execute exige autorização específica prévia do mantenedor e confirmação CONSULTAR no stdin.");
                Console.WriteLine("--self-test executa somente HTTP sintético, sem rede.");
                return 0;
            }
            var options = Options.Parse(args);
            var cnpj = CnpjValidator.ValidateAndNormalize(options.Cnpj);
            var endpoints = Endpoints.For(cnpj, options.Provider);
            Console.WriteLine($"CNPJ: {cnpj}; modo: {options.Mode}; seleção: {options.Provider}");
            foreach (var endpoint in endpoints) Console.WriteLine($"GET {endpoint.Url} ({endpoint.Provider})");
            Console.WriteLine($"Uma consulta lógica, no máximo {endpoints.Count} requisições, sem retry/redirecionamento/healthcheck.");
            Console.WriteLine("Aguardar pelo menos 21 s entre consultas manuais; parar o lote ao receber bloqueio ou 429.");
            if (!options.Execute)
            {
                Console.WriteLine("PRÉVIA: zero requisições. Use --execute somente após autorização específica.");
                return 0;
            }
            Console.WriteLine("Após autorização do mantenedor para este CNPJ/endpoints/orçamento, digite CONSULTAR:");
            if (Console.ReadLine() != "CONSULTAR")
            {
                Console.WriteLine("Não confirmado: zero requisições.");
                return 1;
            }
            using var cancellation = new CancellationTokenSource();
            ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; cancellation.Cancel(); };
            Console.CancelKeyPress += cancel;
            try
            {
                using var transport = new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false };
                using var trace = new TraceHandler(transport, endpoints, cancellation);
                using var http = new HttpClient(trace) { Timeout = TimeSpan.FromSeconds(30) };
                http.DefaultRequestHeaders.UserAgent.ParseAdd("GetCNPJ.Community-Homologation/1.1.0");
                using var client = new CnpjClient(http);
                var elapsed = Stopwatch.StartNew();
                GetCNPJ.Models.CnpjResult? result = null;
                string? failure = null;
                try
                {
                    result = await QueryAsync(client, options, cnpj, cancellation.Token);
                }
                catch (Exception ex)
                {
                    // Mensagens podem incorporar JSON/contatos do servidor: exibir somente o tipo.
                    failure = ex.GetType().Name;
                }
                elapsed.Stop();
                Report.Write(Console.Out, cnpj, options.ExpectedName, options.Mode, result, failure, trace, elapsed.Elapsed);
                if (trace.Stopped != null) return 3;
                return result?.Success == true && Report.IdentityMatches(cnpj, result.Data) ? 0 : 2;
            }
            finally { Console.CancelKeyPress -= cancel; }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Falha local de configuração/validação: " + ex.GetType().Name);
            return 1;
        }
    }

    internal static Task<GetCNPJ.Models.CnpjResult> QueryAsync(CnpjClient client, Options options, string cnpj, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (options.Mode == "sync")
            return Task.FromResult(options.Provider == "auto" ? client.Get(cnpj) : client.GetFromProvider(cnpj, options.Provider));
        return options.Provider == "auto" ? client.GetAsync(cnpj, token) : client.GetFromProviderAsync(cnpj, options.Provider, token);
    }
}

internal sealed record Options(string Cnpj, string Provider, string Mode, string? ExpectedName, bool Execute)
{
    internal static Options Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var execute = false;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--execute")
            {
                if (execute) throw new ArgumentException("Opção duplicada");
                execute = true;
                continue;
            }
            if (!new[] { "--cnpj", "--provider", "--mode", "--expected-name" }.Contains(args[i]) || i + 1 == args.Length)
                throw new ArgumentException("Opção desconhecida ou sem valor");
            if (!values.TryAdd(args[i], args[++i])) throw new ArgumentException("Opção duplicada");
        }
        if (!values.TryGetValue("--cnpj", out var cnpj)) throw new ArgumentException("CNPJ obrigatório");
        var provider = values.TryGetValue("--provider", out var p) ? p : "auto";
        provider = new[] { "auto", "CNPJWS", "ReceitaWS", "BrasilAPI", "CNPJA" }.Single(x => x.Equals(provider, StringComparison.OrdinalIgnoreCase));
        var mode = values.TryGetValue("--mode", out var m) ? m.ToLowerInvariant() : "async";
        if (mode != "sync" && mode != "async") throw new ArgumentException("Modo inválido");
        return new Options(cnpj, provider, mode, values.TryGetValue("--expected-name", out var name) ? name : null, execute);
    }
}

internal sealed record Endpoint(string Provider, Uri Url);

internal static class Endpoints
{
    internal static IReadOnlyList<Endpoint> For(string cnpj, string provider)
    {
        var all = new[]
        {
            new Endpoint("CNPJWS", new Uri("https://publica.cnpj.ws/cnpj/" + cnpj)),
            new Endpoint("ReceitaWS", new Uri("https://receitaws.com.br/v1/cnpj/" + cnpj)),
            new Endpoint("BrasilAPI", new Uri("https://brasilapi.com.br/api/cnpj/v1/" + cnpj)),
            new Endpoint("CNPJA", new Uri("https://open.cnpja.com/office/" + cnpj))
        };
        return all.Where(e => provider == "auto" || e.Provider == provider).ToArray();
    }
}
