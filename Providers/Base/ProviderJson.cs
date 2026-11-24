using System;
using System.Text.Json;

namespace GetCNPJ.Providers.Base
{
    internal static class ProviderJson
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // HTTP 200 pode conter um envelope de erro, inclusive com um identificador.
        internal static T? Deserialize<T>(string content) where T : class
        {
            using var document = ParseDocument(content);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    var value = property.Value;
                    if (property.Name.Equals("status", StringComparison.OrdinalIgnoreCase))
                    {
                        if ((value.ValueKind == JsonValueKind.String && string.Equals(value.GetString(), "ERROR", StringComparison.OrdinalIgnoreCase)) ||
                            (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var status) && status >= 400))
                            throw new InvalidOperationException("O provedor informou erro no JSON retornado.");
                    }
                    if ((property.Name.Equals("error", StringComparison.OrdinalIgnoreCase) || property.Name.Equals("errors", StringComparison.OrdinalIgnoreCase)) && HasError(value))
                        throw new InvalidOperationException("O provedor informou erro no JSON retornado.");
                }
            }
            return JsonSerializer.Deserialize<T>(content, Options);
        }

        private static JsonDocument ParseDocument(string content)
        {
            try { return JsonDocument.Parse(content); }
            catch (JsonException ex)
            {
                // Mantém o tipo público emitido pelo antigo JsonSerializer.Deserialize.
                throw new JsonException("JSON inválido retornado pelo provedor.", ex);
            }
        }

        private static bool HasError(JsonElement value)
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.Null:
                case JsonValueKind.False: return false;
                case JsonValueKind.String: return !string.IsNullOrWhiteSpace(value.GetString());
                case JsonValueKind.Array: return value.GetArrayLength() > 0;
                default: return true;
            }
        }
    }
}
