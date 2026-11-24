using System;

namespace GetCNPJ.Models
{
    /// <summary>
    /// Informações sobre o Simples Nacional
    /// </summary>
    public class SimplesNacional
    {
        /// <summary>Opção informada pelo provedor; null significa desconhecida. Optante preserva false nesse caso.</summary>
        public bool? OptanteInformado { get; set; }

        /// <summary>Enquadramento SIMEI informado; null significa desconhecido. OptanteSimei preserva false nesse caso.</summary>
        public bool? OptanteSimeiInformado { get; set; }

        /// <summary>
        /// Indica se é optante pelo Simples Nacional
        /// </summary>
        public bool Optante { get; set; }

        /// <summary>
        /// Data de opção pelo Simples Nacional
        /// </summary>
        public DateTime? DataOpcao { get; set; }

        /// <summary>
        /// Data de exclusão do Simples Nacional
        /// </summary>
        public DateTime? DataExclusao { get; set; }

        /// <summary>
        /// Indica se é optante pelo SIMEI
        /// </summary>
        public bool OptanteSimei { get; set; }
    }
}
