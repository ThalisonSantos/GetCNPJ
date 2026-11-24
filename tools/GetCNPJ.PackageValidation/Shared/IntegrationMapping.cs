using System;
using System.Collections.Generic;
using System.Linq;
using GetCNPJ.Models;
using GetCNPJ.Validation;

namespace PackageValidation
{
    // Rascunho de integração, sem persistência e sem inferir condição de contribuinte.
    public sealed class CustomerDraft
    {
        public string Cnpj { get; }
        public string? RazaoSocial { get; }
        public string? NomeFantasia { get; }
        public string? Logradouro { get; }
        public string? Cep { get; }
        public string? CodigoIbge { get; }
        public string? CnaePrincipal { get; }
        public bool? Simples { get; }
        public bool? Mei { get; }
        public IReadOnlyList<InscricaoEstadual> InscricoesNaUf { get; }
        public CustomerDraft(CnpjData data, string uf)
        {
            if (data.Cnpj == null) throw new ArgumentException("CNPJ retornado obrigatório");
            Cnpj = CnpjValidator.ValidateAndNormalize(data.Cnpj);
            RazaoSocial = data.RazaoSocial;
            NomeFantasia = data.NomeFantasia;
            Logradouro = data.Endereco?.Logradouro;
            Cep = data.Endereco?.Cep;
            CodigoIbge = data.Endereco?.CodigoIbge;
            CnaePrincipal = data.AtividadePrincipal?.Codigo;
            Simples = data.Simples?.OptanteInformado;
            Mei = data.Simples?.OptanteSimeiInformado;
            InscricoesNaUf = data.InscricoesEstaduais
                .Where(ie => string.Equals(ie.Estado, uf.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }
}
