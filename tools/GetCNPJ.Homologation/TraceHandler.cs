using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace GetCNPJ.Homologation;

internal sealed record Attempt(string Provider)
{
    internal int? Status { get; set; }
    internal double Milliseconds { get; set; }
    internal string? FailureType { get; set; }
    internal Dictionary<string, string> Fields { get; } = new();
}

internal sealed class TraceHandler : DelegatingHandler
{
    private readonly IReadOnlyList<Endpoint> _allowed;
    private readonly CancellationTokenSource _cancellation;
    private readonly HashSet<Uri> _sent = new();
    internal List<Attempt> Attempts { get; } = new();
    internal string? Stopped { get; private set; }

    internal TraceHandler(HttpMessageHandler inner, IReadOnlyList<Endpoint> allowed, CancellationTokenSource cancellation) : base(inner)
    {
        _allowed = allowed;
        _cancellation = cancellation;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (Stopped != null || _cancellation.IsCancellationRequested) throw new OperationCanceledException(_cancellation.Token);
        var endpoint = _allowed.SingleOrDefault(e => e.Url == request.RequestUri);
        if (request.Method != HttpMethod.Get || endpoint == null || !_sent.Add(endpoint.Url))
            throw new InvalidOperationException("Pedido fora do plano ou tentativa repetida.");
        var attempt = new Attempt(endpoint.Provider);
        Attempts.Add(attempt);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellation.Token);
        var elapsed = Stopwatch.StartNew();
        HttpResponseMessage? response = null;
        try
        {
            response = await base.SendAsync(request, linked.Token).ConfigureAwait(false);
            attempt.Status = (int)response.StatusCode;
            if (response.StatusCode == HttpStatusCode.TooManyRequests ||
                response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized ||
                ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400))
            {
                Stopped = "HTTP " + attempt.Status + ": interromper lote; sem contorno, retry ou redirecionamento.";
                _cancellation.Cancel();
                throw new OperationCanceledException(_cancellation.Token);
            }
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(linked.Token).ConfigureAwait(false);
                try
                {
                    using var document = JsonDocument.Parse(content);
                    foreach (var field in Fields.For(endpoint.Provider))
                        attempt.Fields[field.Key] = State(Find(document.RootElement, field.Value));
                }
                catch (JsonException) { attempt.FailureType = "JsonException (inspeção do contrato)"; }
            }
            return response;
        }
        catch (Exception ex)
        {
            response?.Dispose();
            attempt.FailureType = ex.GetType().Name;
            throw;
        }
        finally { attempt.Milliseconds = elapsed.Elapsed.TotalMilliseconds; }
    }

    private static JsonElement Find(JsonElement element, string path)
    {
        foreach (var part in path.Split('.'))
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(part, out element)) return default;
        }
        return element;
    }

    private static string State(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Undefined => "ausente",
        JsonValueKind.Null => "null",
        JsonValueKind.String when string.IsNullOrWhiteSpace(value.GetString()) => "vazio",
        JsonValueKind.Array when value.GetArrayLength() == 0 => "lista vazia",
        JsonValueKind.False => "negativo explícito",
        JsonValueKind.True => "positivo explícito",
        _ => "presente"
    };
}

internal static class Fields
{
    // Somente presença/tipo de campos conhecidos, nunca seus valores nem o JSON completo.
    internal static IReadOnlyDictionary<string, string> For(string provider)
    {
        var names = new[] { "CNPJ", "Razão social", "Fantasia", "Situação", "Endereço", "CEP", "IBGE", "CNAE principal", "CNAEs secundários", "IE", "Simples", "MEI", "Capital", "Abertura", "Data situação", "Atualização" };
        string[] paths = provider switch
        {
            "CNPJWS" => new[] { "estabelecimento.cnpj", "razao_social", "estabelecimento.nome_fantasia", "estabelecimento.situacao_cadastral", "estabelecimento.logradouro", "estabelecimento.cep", "estabelecimento.cidade.ibge_id", "estabelecimento.atividade_principal", "estabelecimento.atividades_secundarias", "estabelecimento.inscricoes_estaduais", "simples.simples", "simples.mei", "capital_social", "estabelecimento.data_inicio_atividade", "estabelecimento.data_situacao_cadastral", "atualizado_em" },
            "ReceitaWS" => new[] { "cnpj", "nome", "fantasia", "situacao", "logradouro", "cep", "codigo_ibge", "atividade_principal", "atividades_secundarias", "inscricoes_estaduais", "simples.optante", "simei.optante", "capital_social", "abertura", "data_situacao", "ultima_atualizacao" },
            "BrasilAPI" => new[] { "cnpj", "razao_social", "nome_fantasia", "descricao_situacao_cadastral", "logradouro", "cep", "codigo_municipio_ibge", "cnae_fiscal", "cnaes_secundarios", "inscricoes_estaduais", "opcao_pelo_simples", "opcao_pelo_mei", "capital_social", "data_inicio_atividade", "data_situacao_cadastral", "ultima_atualizacao" },
            "CNPJA" => new[] { "taxId", "company.name", "alias", "status.text", "address.street", "address.zip", "address.municipality", "mainActivity", "sideActivities", "registrations", "company.simples.optant", "company.simei.optant", "company.equity", "founded", "statusDate", "updated" },
            _ => throw new ArgumentOutOfRangeException(nameof(provider))
        };
        var fields = names.Select((name, i) => (name, path: paths[i])).ToDictionary(x => x.name, x => x.path);
        // Não atribuir campos de modalidades comerciais ao contrato público observado.
        if (provider == "ReceitaWS") { fields.Remove("IBGE"); fields.Remove("IE"); }
        if (provider == "BrasilAPI") { fields.Remove("IE"); fields.Remove("Atualização"); }
        if (provider == "CNPJA") fields.Remove("IE");
        return fields;
    }
}
