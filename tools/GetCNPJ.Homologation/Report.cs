using System.Globalization;
using System.Text;
using GetCNPJ.Models;
using GetCNPJ.Validation;

namespace GetCNPJ.Homologation;

internal static class Report
{
    internal static bool IdentityMatches(string requested, CnpjData? data) =>
        data != null && CnpjValidator.IsValid(data.Cnpj) && CnpjValidator.ValidateAndNormalize(data.Cnpj) == requested;

    internal static Dictionary<string, string> Available(CnpjData data) => new()
    {
        ["CNPJ"] = Text(data.Cnpj), ["Razão social"] = Text(data.RazaoSocial), ["Fantasia"] = Text(data.NomeFantasia),
        ["Situação"] = Text(data.Situacao), ["Endereço"] = Text(data.Endereco?.Logradouro), ["CEP"] = Text(data.Endereco?.Cep),
        ["IBGE"] = Text(data.Endereco?.CodigoIbge), ["CNAE principal"] = Text(data.AtividadePrincipal?.Codigo),
        ["CNAEs secundários"] = Count(data.AtividadesSecundarias.Count), ["IE"] = Count(data.InscricoesEstaduais.Count),
        ["Simples"] = Option(data.Simples?.OptanteInformado), ["MEI"] = Option(data.Simples?.OptanteSimeiInformado),
        ["Capital"] = data.CapitalSocial?.ToString(CultureInfo.InvariantCulture) ?? "ausente",
        ["Abertura"] = Date(data.DataAbertura), ["Data situação"] = Date(data.DataSituacao), ["Atualização"] = Date(data.UltimaAtualizacao)
    };

    internal static void Write(TextWriter output, string requested, string? expectedName, string mode,
        CnpjResult? result, string? failure, TraceHandler trace, TimeSpan elapsed)
    {
        output.WriteLine($"UTC: {DateTime.UtcNow:O}; runtime: {Environment.Version}; SO: {Environment.OSVersion}; modo: {mode}");
        output.WriteLine($"Tempo total: {elapsed.TotalMilliseconds:F1} ms; tentativas de envio: {trace.Attempts.Count}");
        foreach (var attempt in trace.Attempts)
        {
            output.WriteLine($"Tentativa {attempt.Provider}: HTTP {attempt.Status?.ToString() ?? "indisponível"}; {attempt.Milliseconds:F1} ms; erro transporte/JSON: {attempt.FailureType ?? "nenhum"}");
            foreach (var field in attempt.Fields) output.WriteLine($"  Fonte/{field.Key}: {field.Value}");
        }
        output.WriteLine($"Sucesso biblioteca: {result?.Success == true}; provedor: {result?.Data?.Provedor ?? "nenhum"}; erro terminal: {failure ?? "nenhum"}");
        if (trace.Stopped != null) output.WriteLine("PARAR: " + trace.Stopped);
        if (result != null)
        {
            foreach (var error in result.Errors)
                output.WriteLine($"Erro {error.ProviderName}: {Category(error.Exception)}; tipos: {Types(error.Exception)}");
            if (!result.Success && result.Errors.Count == 0) output.WriteLine("Falha sem dados (mensagem/corpo externo omitidos).");
        }
        var data = result?.Data;
        output.WriteLine("Correspondência CNPJ: " + (data == null ? "não verificável: sem dados" : IdentityMatches(requested, data) ? "CONFERE" : "DIVERGENTE/INVÁLIDO"));
        if (data == null) return;
        var fields = Available(data);
        output.WriteLine("Razão social: " + Safe(data.RazaoSocial));
        output.WriteLine("Nome fantasia: " + Safe(data.NomeFantasia));
        output.WriteLine("Situação: " + Safe(data.Situacao));
        output.WriteLine("CEP: " + Safe(data.Endereco?.Cep) + "; IBGE: " + Safe(data.Endereco?.CodigoIbge));
        output.WriteLine("CNAE principal: " + Safe(data.AtividadePrincipal?.Codigo));
        foreach (var field in fields) output.WriteLine($"Modelo/{field.Key}: {field.Value}");
        foreach (var registration in data.InscricoesEstaduais)
            output.WriteLine($"IE: número {Text(registration.Inscricao)}; UF {Safe(registration.Estado)}; ativa {Option(registration.AtivoInformado)}; atualização {Date(registration.DataAtualizacao)}");
        output.WriteLine("Telefones/e-mail/sócios: valores omitidos; corpos JSON e mensagens externas não são impressos nem gravados.");
        var checks = Check(requested, data, expectedName);
        output.WriteLine(checks.Count == 0 ? "Checagens locais: sem inconsistência detectada (não certifica cadastro/fiscalidade)." : "Checagens locais:");
        foreach (var check in checks) output.WriteLine("  " + check);
        var source = trace.Attempts.LastOrDefault(a => a.Provider == data.Provedor);
        if (source != null)
        {
            foreach (var field in source.Fields)
                if ((field.Value == "presente" || field.Value.EndsWith("explícito", StringComparison.Ordinal)) &&
                    (fields[field.Key] == "ausente" || fields[field.Key] == "desconhecido"))
                    output.WriteLine($"CONFERIR MAPEAMENTO: fonte/{field.Key} preenchida, modelo ausente; confirmar significado e tipo antes de concluir incompatibilidade.");
        }
        if (data.RazaoSocial == null || data.Situacao == null || data.Endereco?.Logradouro == null)
            output.WriteLine("RESPOSTA INCOMPLETA: algum campo cadastral central está ausente; não equivale a informação negativa.");
        output.WriteLine("Lista IE vazia não comprova não contribuinte; flags desconhecidas não são negativas. Situações podem estar defasadas.");
    }

    internal static List<string> Check(string requested, CnpjData data, string? expectedName)
    {
        var problems = new List<string>();
        if (!IdentityMatches(requested, data)) problems.Add("CNPJ ausente, inválido ou divergente.");
        if (expectedName != null && NameKey(data.RazaoSocial) != NameKey(expectedName)) problems.Add("Razão social difere da referência pública; conferir abreviação/atualização.");
        if (data.Endereco?.Cep is string cep && !Digits(cep, 8)) problems.Add("CEP não tem oito dígitos válidos.");
        if (data.Endereco?.CodigoIbge is string ibge && (ibge.Length != 7 || !ibge.All(char.IsDigit))) problems.Add("Código IBGE municipal não tem sete dígitos; não confirma existência do código.");
        if (data.AtividadePrincipal?.Codigo is string main && !Digits(main, 7)) problems.Add("Formato do CNAE principal requer conferência.");
        if (data.AtividadesSecundarias.Any(a => a.Codigo is string code && !Digits(code, 7))) problems.Add("Formato de CNAE secundário requer conferência.");
        if (data.CapitalSocial < 0) problems.Add("Capital social negativo; conferir fonte.");
        if (data.Simples?.OptanteSimeiInformado == true && data.Simples.OptanteInformado == false) problems.Add("MEI informado positivo e Simples negativo: inconsistência fiscal aparente.");
        foreach (var date in new[] { data.DataAbertura, data.DataSituacao, data.UltimaAtualizacao })
            if (date > DateTime.UtcNow.AddDays(1)) problems.Add("Data futura; conferir relógio/fonte.");
        return problems;
    }

    private static bool Digits(string value, int count) => value.Count(char.IsDigit) == count && value.All(c => char.IsDigit(c) || " ./-".Contains(c));
    private static string NameKey(string? value) => new((value ?? "").Normalize(NormalizationForm.FormD).Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
    private static string Text(string? value) => value == null ? "ausente" : string.IsNullOrWhiteSpace(value) ? "vazio" : "presente";
    private static string Count(int count) => count == 0 ? "lista vazia (cobertura desconhecida)" : $"presente ({count} itens)";
    private static string Option(bool? value) => value switch { true => "sim (positivo informado)", false => "não (negativo informado)", null => "desconhecido" };
    private static string Date(DateTime? value) => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "ausente";
    private static string Safe(string? value) => value == null ? "(ausente)" : new string(value.Where(c => !char.IsControl(c)).Take(160).ToArray());
    private static string Types(Exception? error) => error == null ? "nenhuma" : error.GetType().Name + (error.InnerException == null ? "" : " > " + Types(error.InnerException));
    private static string Category(Exception? error)
    {
        if (error == null) return "sem dados";
        if (error is System.Text.Json.JsonException) return "desserialização/mapeamento";
        if (error is InvalidOperationException && error.Message.StartsWith("O provedor retornou CNPJ", StringComparison.Ordinal)) return "identidade rejeitada";
        if (error.InnerException != null) return Category(error.InnerException);
        return error is OperationCanceledException ? "cancelamento/timeout" : "falha HTTP/transporte/provedor";
    }
}
