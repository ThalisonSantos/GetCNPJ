# GetCNPJ

[![NuGet](https://img.shields.io/nuget/v/GetCNPJ.svg)](https://www.nuget.org/packages/GetCNPJ/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

<img width="374" height="373" alt="image" src="https://github.com/user-attachments/assets/a52c4ae9-fd76-48a6-af33-1dda1de6765d" />


Biblioteca .NET para consulta de CNPJ em APIs públicas brasileiras, com suporte a múltiplos provedores e rate limiting integrado.

## 📋 Características

- ✅ **Multi-Target**: Suporta .NET Framework 4.8, 4.8.1, .NET 8, .NET 9 e .NET Standard 2.0
- 🔄 **Chain of Responsibility**: Tenta automaticamente outros provedores em caso de falha
- ⏱️ **Rate Limiting**: Controle automático de 3 requisições por minuto por provedor
- 🎯 **Retorno Padronizado**: Dados consistentes independente do provedor utilizado
- 📋 **Inscrição Estadual**: Suporte completo a inscrições estaduais (via CNPJ.WS)
- 🔌 **Múltiplos Provedores**:
  - **CNPJ.WS** (https://publica.cnpj.ws) - **Provedor padrão** ⭐ *Único que retorna inscrição estadual*
  - ReceitaWS (https://receitaws.com.br)
  - BrasilAPI (https://brasilapi.com.br)
  - CNPJA (https://open.cnpja.com)
- 🛡️ **Validação**: Validação local de CNPJ numérico e alfanumérico, incluindo dígitos verificadores
- ⚡ **Async/Await**: API totalmente assíncrona + métodos síncronos para compatibilidade
- 🎨 **Clean Code**: Seguindo padrões SOLID e melhores práticas

## CNPJ numérico e alfanumérico

A validação independente está em `GetCNPJ.Validation.CnpjValidator`, conforme o [manual de cálculo da Receita Federal/Serpro](https://www.gov.br/receitafederal/pt-br/centrais-de-conteudo/publicacoes/documentos-tecnicos/cnpj/manual-dv-cnpj.pdf). São aceitos 12 caracteres ASCII de `0–9` ou `A–Z`, seguidos de dois DVs numéricos, sem máscara ou na máscara padrão. CNPJs numéricos válidos continuam aceitos.

```csharp
using GetCNPJ.Validation;

// Exemplo do manual oficial; não indica existência de cadastro.
var cnpj = CnpjValidator.ValidateAndNormalize("12.abc.345/01de-35"); // 12ABC34501DE35
var valido = CnpjValidator.IsValid(cnpj);                           // true
var formatado = CnpjValidator.Format(cnpj);                       // 12.ABC.345/01DE-35
```

Como conveniência de entrada, letras ASCII minúsculas são convertidas para maiúsculas e espaços externos são removidos. Máscaras incorretas, espaços internos, acentos, caracteres Unicode e letras nas duas posições dos DVs são rejeitados com `InvalidCnpjException` por `ValidateAndNormalize`; `IsValid` retorna `false`. A normalização não descarta letras ou símbolos para tentar produzir outro identificador. Esse tratamento substitui a antiga remoção indiscriminada de caracteres não numéricos.

`Format` aplica apenas a máscara a strings de 14 posições e preserva os caracteres; não valida DVs nem comprova cadastro. Outros valores são mantidos para preservar o comportamento de apresentação existente.

Os adaptadores locais BrasilAPI, CNPJA, CNPJWS e ReceitaWS preservam as letras na URL e nos dados retornados, conforme testes com HTTP simulado. **O suporte a CNPJ alfanumérico nas APIs externas permanece não confirmado.** Validade local não comprova cadastro existente ou disponibilidade no provedor. Nenhuma consulta real foi usada nesses testes.

## 📦 Instalação

```bash
dotnet add package GetCNPJ
```

Ou via NuGet Package Manager:

```
Install-Package GetCNPJ
```

## 🚀 Uso Básico

### Consulta Simples

```csharp
using GetCNPJ;
using System;

// Criar cliente
using var client = new CnpjClient();

// Consultar CNPJ (numérico ou alfanumérico, com ou sem máscara; suporte externo depende da API)
var result = await client.GetAsync("03.312.791/0001-83");

if (result.Success)
{
    var data = result.Data;

    Console.WriteLine($"Razão Social: {data.RazaoSocial}");
    Console.WriteLine($"Nome Fantasia: {data.NomeFantasia}");
    Console.WriteLine($"CNPJ: {data.Cnpj}");
    Console.WriteLine($"Situação: {data.Situacao}");
    Console.WriteLine($"Endereço: {data.Endereco.EnderecoCompleto}");
    Console.WriteLine($"Email: {data.Email}");
    Console.WriteLine($"Telefones: {string.Join(", ", data.Telefones)}");

    // Inscrições Estaduais (disponível quando usa CNPJ.WS)
    if (data.InscricoesEstaduais != null && data.InscricoesEstaduais.Any())
    {
        Console.WriteLine("Inscrições Estaduais:");
        foreach (var ie in data.InscricoesEstaduais)
        {
            Console.WriteLine($"  - {ie}"); // Formato: Inscrição (UF) - Status
        }
    }

    Console.WriteLine($"Provedor usado: {data.Provedor}");
}
else
{
    Console.WriteLine($"Erro: {result.ErrorMessage}");

    // Exibir erros de cada provedor
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"- {error.ProviderName}: {error.ErrorMessage}");
    }
}
```

### Consulta com Provedor Específico

```csharp
using var client = new CnpjClient();

// Forçar uso de um provedor específico
var result = await client.GetFromProviderAsync("03312791000183", "BrasilAPI");

if (result.Success)
{
    Console.WriteLine($"Razão Social: {result.Data.RazaoSocial}");
}
```

### Consulta Síncrona

Para aplicações que não usam async/await, você pode usar os métodos síncronos:

```csharp
using var client = new CnpjClient();

// Método síncrono
var result = client.Get("03312791000183");

if (result.Success)
{
    Console.WriteLine($"Razão Social: {result.Data.RazaoSocial}");
    Console.WriteLine($"Nome Fantasia: {result.Data.NomeFantasia}");
}

// Provedor específico síncrono
var result2 = client.GetFromProvider("03312791000183", "ReceitaWS");
```

**Nota**: Os métodos síncronos bloqueiam a thread atual. Para aplicações modernas (ASP.NET Core, WPF, etc.), recomenda-se usar os métodos assíncronos.

### Configuração Customizada

```csharp
var options = new CnpjClientOptions
{
    MaxRequestsPerMinute = 5,           // Aumentar rate limit (use com cuidado!)
    Timeout = TimeSpan.FromSeconds(60), // Timeout customizado
    EnableReceitaWS = true,             // Habilitar/desabilitar provedores
    EnableBrasilAPI = true,
    EnableCNPJA = false                 // Desabilitar CNPJA, por exemplo
};

using var client = new CnpjClient(null, options);
var result = await client.GetAsync("03312791000183");
```

### HttpClient Customizado

```csharp
// Usar seu próprio HttpClient (recomendado em aplicações ASP.NET Core)
var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(30)
};

using var client = new CnpjClient(httpClient);
var result = await client.GetAsync("03312791000183");
```

## 📊 Modelo de Dados Retornado

```csharp
public class CnpjData
{
    public string Cnpj { get; set; }                    // CNPJ formatado
    public string RazaoSocial { get; set; }             // Razão social
    public string NomeFantasia { get; set; }            // Nome fantasia
    public DateTime? DataAbertura { get; set; }         // Data de abertura
    public string Situacao { get; set; }                // Situação cadastral
    public DateTime? DataSituacao { get; set; }         // Data da situação
    public string Tipo { get; set; }                    // MATRIZ/FILIAL
    public string Porte { get; set; }                   // Porte da empresa
    public string NaturezaJuridica { get; set; }        // Natureza jurídica
    public decimal? CapitalSocial { get; set; }         // Capital social
    public Endereco Endereco { get; set; }              // Endereço completo
    public AtividadeEconomica AtividadePrincipal { get; set; }
    public List<AtividadeEconomica> AtividadesSecundarias { get; set; }
    public List<Socio> QuadroSocietario { get; set; }   // QSA
    public List<string> Telefones { get; set; }         // Telefones
    public string Email { get; set; }                   // Email
    public List<InscricaoEstadual> InscricoesEstaduais { get; set; }  // Inscrições estaduais (CNPJ.WS)
    public SimplesNacional Simples { get; set; }        // Info Simples Nacional
    public DateTime? UltimaAtualizacao { get; set; }    // Data da atualização
    public string Provedor { get; set; }                // Provedor usado
}
```

## ⚙️ Padrões de Projeto Utilizados

- **Strategy Pattern**: Diferentes implementações de provedores de API
- **Chain of Responsibility**: Tentativa automática em múltiplos provedores
- **Factory Pattern**: Criação de instâncias dos provedores
- **Repository Pattern**: Abstração da lógica de consulta
- **DTO Pattern**: Padronização dos retornos
- **Rate Limiter Pattern**: Controle de requisições usando Sliding Window

## 🔒 Rate Limiting

A biblioteca usa uma janela deslizante de até 3 aquisições por minuto por provedor, por padrão. Os provedores de uma instância de `CnpjClient` compartilham o limiter, com filas independentes por nome. Instâncias diferentes do cliente têm limiters independentes; não há coordenação entre processos.

`SlidingWindowRateLimiter.AcquireAsync` espera uma vaga e registra a aquisição atomicamente. Cada registro expira após o intervalo configurado, inclusive no instante exato do fim da janela; chamadas que disputam a mesma vaga reavaliam a fila antes de entrar. O tempo decorrido vem de `Stopwatch`, sem usar horário UTC para expiração. Nenhum bloqueio síncrono é mantido durante as esperas assíncronas.

```csharp
var limiter = new GetCNPJ.RateLimiter.SlidingWindowRateLimiter(3, TimeSpan.FromMinutes(1));
await limiter.AcquireAsync("MeuProvedor", cancellationToken);
// A vaga já foi registrada. Não chamar RecordRequest para a mesma aquisição.
```

`Reset` limpa os registros do provedor e acorda as esperas para disputar a capacidade reiniciada. `GetAvailableRequests` e os métodos legados compartilham a proteção da fila. Cancelamento durante a espera não registra uma aquisição; uma aquisição já concluída permanece contada mesmo se o consumidor cancelar depois. Limites e intervalos devem ser positivos; o construtor lança `ArgumentOutOfRangeException` caso contrário. Intervalos positivos longos são atendidos por esperas em partes, e esperas menores que 1 ms são arredondadas para cima.

O contrato `IRateLimiter` mantém seus quatro membros. A nova interface opcional `IAtomicRateLimiter` permite que implementações externas ofereçam `AcquireAsync`. A base dos provedores usa essa capacidade quando disponível, sem registro adicional. Implementações antigas continuam sendo chamadas na ordem `WaitIfNeededAsync` e `RecordRequest`, uma vez cada; sua concorrência continua sob responsabilidade da implementação externa. O par legado de chamadas separadas não reserva atomicamente uma vaga; para concorrência com o limiter embutido, use `AcquireAsync`.

## 🛠️ Arquitetura

```
GetCNPJ/
├── Models/              # DTOs padronizados
├── Interfaces/          # Contratos da biblioteca
├── Exceptions/          # Exceções customizadas
├── Enums/              # Enumerações
├── Providers/
│   ├── Base/           # Classe base para providers
│   ├── ReceitaWS/      # Provider ReceitaWS
│   ├── BrasilAPI/      # Provider BrasilAPI
│   └── CNPJA/          # Provider CNPJA
├── Services/           # Serviço principal (Chain of Responsibility)
├── RateLimiter/        # Implementação do Rate Limiter
└── CnpjClient.cs       # Cliente principal
```

## 🧪 Tratamento de Erros

A biblioteca trata erros de forma elegante:

- **CNPJ Inválido**: Lança `InvalidCnpjException`
- **Provedor Indisponível**: Tenta o próximo provedor automaticamente
- **Rate Limit Excedido**: Aguarda automaticamente antes de fazer nova requisição
- **Timeout**: Configurável via `CnpjClientOptions`
- **Todos Provedores Falharam**: Retorna `CnpjResult` com `Success = false` e lista de erros

## 📝 Requisitos

- .NET Framework 4.8+ **ou**
- .NET 8+ **ou**
- .NET Standard 2.0+

## 🤝 Contribuindo

Contribuições são bem-vindas! Sinta-se à vontade para:

1. Fazer fork do projeto
2. Criar uma branch para sua feature (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abrir um Pull Request

## 📄 Licença

Este projeto está licenciado sob a licença MIT - veja o arquivo LICENSE para detalhes.

## 🔗 APIs Utilizadas

- [**CNPJ.WS**](https://publica.cnpj.ws) - **API padrão** ⭐ Único provedor que retorna inscrição estadual
- [ReceitaWS](https://receitaws.com.br) - API gratuita de consulta de CNPJ
- [BrasilAPI](https://brasilapi.com.br) - API pública brasileira
- [CNPJA](https://open.cnpja.com) - API aberta de CNPJ

**Nota**: Por padrão, a biblioteca usa o provedor CNPJ.WS como primeira opção (prioridade 1) pois é o único que fornece dados de inscrição estadual. Os demais provedores são utilizados como fallback em caso de falha.

## ⚠️ Avisos Importantes

- **Rate Limiting**: Respeite os limites de requisição das APIs públicas (3 req/min por padrão)
- **Uso Responsável**: Esta biblioteca é para uso educacional e em aplicações que respeitem os termos de uso das APIs
- **Dados Públicos**: Os dados retornados são públicos e provenientes da Receita Federal do Brasil
- **Sem Garantias**: Os provedores podem ter indisponibilidade ou mudanças sem aviso prévio

## 🎯 Roadmap

- [ ] Adicionar cache de consultas
- [ ] Suporte a mais provedores
- [ ] Métricas e telemetria
- [ ] Retry policy configurável
- [ ] Suporte a consulta em lote
- [ ] Logging integrado

## 📞 Suporte

Para reportar bugs ou solicitar features, abra uma issue no GitHub.

---

Desenvolvido com ❤️ seguindo as melhores práticas de Clean Code e SOLID
