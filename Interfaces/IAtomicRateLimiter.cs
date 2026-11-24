using System.Threading;
using System.Threading.Tasks;

namespace GetCNPJ.Interfaces
{
    /// <summary>
    /// Capacidade opcional de aguardar e registrar uma requisição em uma única operação.
    /// Implementações existentes de IRateLimiter não precisam implementar esta interface.
    /// </summary>
    public interface IAtomicRateLimiter : IRateLimiter
    {
        /// <summary>
        /// Aguarda uma vaga e registra sua aquisição atomicamente.
        /// Não chamar RecordRequest depois desta operação.
        /// </summary>
        Task AcquireAsync(string providerName, CancellationToken cancellationToken = default);
    }
}
