namespace GetCNPJ.Providers.Base
{
    internal static class ProviderFormatting
    {
        // Compõe somente partes conhecidas: não cria uma descrição vazia ou um código zero.
        internal static string? Join(string? first, string? second, string separator)
        {
            if (string.IsNullOrWhiteSpace(first))
                return string.IsNullOrWhiteSpace(second) ? null : second;
            if (string.IsNullOrWhiteSpace(second))
                return first;
            return first + separator + second;
        }
    }
}
