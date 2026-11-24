using System.Threading;
using System.Threading.Tasks;
using GetCNPJ.Models;

namespace GetCNPJ.Interfaces
{
    /// <summary>
    /// Interface para provedores de dados de CNPJ
    /// </summary>
    public interface ICnpjProvider
    {
        /// <summary>
        /// Nome do provedor
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Prioridade do provedor (quanto menor, maior a prioridade)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Consulta dados de um CNPJ
        /// </summary>
        /// <param name="cnpj">CNPJ numérico ou alfanumérico; suporte da API externa depende do provedor</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Dados do CNPJ ou null em caso de erro</returns>
        Task<CnpjData?> GetCnpjDataAsync(string cnpj, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica se o provedor está disponível
        /// </summary>
        /// <returns>True se disponível</returns>
        Task<bool> IsAvailableAsync();
    }
}
