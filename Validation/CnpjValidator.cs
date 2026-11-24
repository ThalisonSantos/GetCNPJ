using GetCNPJ.Exceptions;

namespace GetCNPJ.Validation
{
    /// <summary>
    /// Validação local de CNPJ numérico ou alfanumérico, sem consultar cadastro ou provedor.
    /// </summary>
    public static class CnpjValidator
    {
        /// <summary>
        /// Aceita 14 posições ou a máscara XX.XXX.XXX/XXXX-XX, remove a máscara e
        /// espaços externos e converte letras ASCII para maiúsculas. Valida os dois DVs.
        /// </summary>
        /// <exception cref="InvalidCnpjException">Formato, caracteres ou DVs inválidos.</exception>
        public static string ValidateAndNormalize(string? cnpj)
        {
            if (!TryNormalize(cnpj, out var normalized))
                throw new InvalidCnpjException("CNPJ deve conter 12 caracteres ASCII de A a Z ou de 0 a 9 e dois dígitos verificadores numéricos, com ou sem a máscara padrão");

            if (!HasValidCheckDigits(normalized))
                throw new InvalidCnpjException("CNPJ com dígitos verificadores inválidos");

            return normalized;
        }

        /// <summary>
        /// Verifica formato e DVs. Não comprova existência cadastral nem suporte de APIs externas.
        /// </summary>
        public static bool IsValid(string? cnpj)
        {
            return TryNormalize(cnpj, out var normalized) && HasValidCheckDigits(normalized);
        }

        /// <summary>
        /// Aplica a máscara a valores de 14 posições, preservando todos os caracteres.
        /// Mantém outros valores inalterados, para compatibilidade com retornos dos provedores.
        /// Esta operação de apresentação não valida os DVs.
        /// </summary>
        public static string? Format(string? cnpj)
        {
            if (cnpj == null || string.IsNullOrWhiteSpace(cnpj) || cnpj.Length != 14)
                return cnpj;

            return $"{cnpj.Substring(0, 2)}.{cnpj.Substring(2, 3)}.{cnpj.Substring(5, 3)}/{cnpj.Substring(8, 4)}-{cnpj.Substring(12, 2)}";
        }

        private static bool TryNormalize(string? cnpj, out string normalized)
        {
            normalized = string.Empty;
            if (cnpj == null || string.IsNullOrWhiteSpace(cnpj))
                return false;

            var value = cnpj.Trim();
            if (value.Length == 18)
            {
                if (value[2] != '.' || value[6] != '.' || value[10] != '/' || value[15] != '-')
                    return false;

                value = value.Substring(0, 2) + value.Substring(3, 3) + value.Substring(7, 3)
                    + value.Substring(11, 4) + value.Substring(16, 2);
            }

            if (value.Length != 14)
                return false;

            var characters = value.ToCharArray();
            for (var i = 0; i < characters.Length; i++)
            {
                var character = characters[i];
                // Converter somente ASCII impede aceitar letras Unicode por equivalência de caixa.
                if (character >= 'a' && character <= 'z')
                    character = (char)(character - 'a' + 'A');

                var isDigit = character >= '0' && character <= '9';
                var isLetter = character >= 'A' && character <= 'Z';
                if (!isDigit && !(i < 12 && isLetter))
                    return false;

                characters[i] = character;
            }

            normalized = new string(characters);
            return true;
        }

        private static bool HasValidCheckDigits(string cnpj)
        {
            var allSame = true;
            for (var i = 1; i < cnpj.Length && allSame; i++)
                allSame = cnpj[i] == cnpj[0];

            // Preserva a rejeição de sequências numéricas repetidas, inclusive todos os zeros.
            return !allSame
                && cnpj[12] - '0' == CalculateCheckDigit(cnpj, 12)
                && cnpj[13] - '0' == CalculateCheckDigit(cnpj, 13);
        }

        private static int CalculateCheckDigit(string cnpj, int length)
        {
            // Manual RFB/Serpro, pp. 3–4: valor ASCII - 48; pesos 2..9 da direita para a esquerda.
            var sum = 0;
            var weight = 2;
            for (var i = length - 1; i >= 0; i--)
            {
                sum += (cnpj[i] - '0') * weight;
                weight = weight == 9 ? 2 : weight + 1;
            }

            var remainder = sum % 11;
            return remainder < 2 ? 0 : 11 - remainder;
        }
    }
}
