# GetCNPJ Community

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**Fork independente**, mantido por **Thalison Santos**, do [GetCNPJ original](https://github.com/azumamagus/GetCNPJ), desenvolvido por **Bruno da Cunha Felipe**. Repositório do fork: [ThalisonSantos/GetCNPJ](https://github.com/ThalisonSantos/GetCNPJ). Não é uma publicação do autor original nem possui afiliação oficial com a Receita Federal ou com os provedores.

Identidade aprovada: **GetCNPJ.Community 1.1.0**, **ainda não publicado nesta etapa**. O pacote gerado na T07 é candidato histórico e não contém as correções H01; deverá ser regenerado após homologação, a partir de commit aprovado. Namespace e assembly continuam **GetCNPJ**; não é necessário mudar os `using`. A licença MIT e o copyright original estão preservados integralmente em [LICENSE](LICENSE).

**Não instale GetCNPJ e GetCNPJ.Community simultaneamente**, nem por dependências transitivas: ambos fornecem `GetCNPJ.dll` e os mesmos namespaces/tipos, causando conflitos de compilação ou carregamento. Ao migrar, remova a referência ao pacote original e valide também o grafo de dependências. AssemblyVersion permanece 1.0.0.0 por compatibilidade; versão do pacote/arquivo é 1.1.0.

Biblioteca .NET para consulta de CNPJ em APIs públicas brasileiras, com suporte a múltiplos provedores e rate limiting integrado.

## 📋 Características

- ✅ **Multi-Target**: Suporta .NET Framework 4.8, 4.8.1, .NET 8, .NET 9 e .NET Standard 2.0
- 🔄 **Chain of Responsibility**: Tenta automaticamente outros provedores em caso de falha
- ⏱️ **Rate Limiting**: Padrão local de 3 aquisições por minuto por provedor; cotas externas variam por serviço/IP
- 🎯 **Retorno Padronizado**: Modelo comum, com disponibilidade/preenchimento variáveis por provedor
- 📋 **Inscrição Estadual**: Mapeamento dos dados disponibilizados pelo CNPJ.WS; ausência não comprova não contribuinte
- 🔌 **Múltiplos Provedores**:
  - **CNPJ.WS** (https://publica.cnpj.ws) - **Provedor padrão** ⭐ *IE documentada no endpoint público integrado*
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

Os adaptadores locais BrasilAPI, CNPJA, CNPJWS e ReceitaWS preservam letras nas URLs e respostas. Em 2026-11-24, **os quatro provedores tiveram suporte operacional alfanumérico confirmado por teste real autorizado** na H02.1, para a filial pública `00000000E08G12`: HTTP 200, CNPJ correspondente e mapeamento bem-sucedido. BrasilAPI/ReceitaWS também possuem confirmação documental explícita; para CNPJ.WS/CNPJá essa evidência documental continua não confirmada. As automáticas numérica/async e alfanumérica/sync encerraram no primeiro sucesso CNPJWS; transição real após falha não ocorreu, permanecendo coberta pelos testes simulados. A H02 original havia homologado quatro numéricas dirigidas. São observações pontuais, sem garantia de todos os registros, disponibilidade futura, atualização ou consistência fiscal. Validade local não comprova cadastro. Consulte a [matriz de provedores, contratos, fontes e termos](docs/PROVIDERS.md) e as [evidências e console de homologação manual](docs/HOMOLOGATION.md).

## Dados disponíveis e dependências

Respostas parciais com CNPJ válido e correspondente ao solicitado podem ser aceitas; sucesso não garante preenchimento completo. Identidade ausente/inválida/divergente, JSON inválido e envelopes explícitos de erro em HTTP 200 não são sucesso. No cliente, falhas permitem fallback conforme as regras existentes; consultas direcionadas continuam sem fallback.

Na H01, campos opcionais dos modelos e retornos dos provedores passaram a ter anotações nullable coerentes com a ausência já possível. As assinaturas CLR foram preservadas, mas consumidores/overrides podem precisar de checagens e anotações ao recompilar. Confira `result.Success` e `result.Data != null` antes de usar dados; endereço e outros campos continuam opcionais. Matriz e endurecimentos documentados em [docs/RELEASE.md](docs/RELEASE.md).

`Endereco.CodigoIbge` preserva o código municipal quando documentado, sem confundi-lo com SIAFI. Para distinguir informação fiscal desconhecida de negativa, use os novos campos nullable `SimplesNacional.OptanteInformado`, `OptanteSimeiInformado` e `InscricaoEstadual.AtivoInformado`: null = desconhecido, false = negativo informado, true = positivo informado. Os bools antigos continuam false quando desconhecidos por compatibilidade, e o `ToString()` de IE mantém o comportamento anterior. Lista de IE vazia nunca prova não contribuinte. Propriedades novas são independentes e podem aparecer ao serializar os modelos.

Os cinco frameworks foram preservados. System.Text.Json 8.0.6 é referência somente em netstandard2.0/net48/net481; net8/net9 usam o componente do framework. HTTP usa referências da plataforma, com HttpClient compartilhado. A [auditoria de dependências diretas e transitivas](docs/DEPENDENCIES.md) registra versões, justificativas e comandos; não houve vulnerabilidade conhecida reportada pelo NuGet na verificação de 2026-11-24. Consumidores devem manter runtimes atualizados.

## 📦 Instalação

Após publicação autorizada no NuGet.org (os comandos abaixo não indicam que ela já ocorreu):

```bash
dotnet add package GetCNPJ.Community --version 1.1.0
```

Ou via NuGet Package Manager:

```
Install-Package GetCNPJ.Community -Version 1.1.0
```

Para avaliar o pacote gerado nesta preparação usando uma fonte local:

```powershell
dotnet add package GetCNPJ.Community --version 1.1.0 --no-restore
dotnet restore --source "./artifacts/packages" --source "https://api.nuget.org/v3/index.json"
```

Execute no projeto consumidor e ajuste o caminho para a pasta do pacote; a segunda fonte resolve as dependências transitivas. Para garantir a origem local do pacote principal, use o mapeamento de fontes descrito no procedimento completo. Hashes, consumidores independentes, avisos e pendências em [docs/RELEASE.md no repositório](https://github.com/ThalisonSantos/GetCNPJ/blob/develop/docs/RELEASE.md). CI permanece somente build/test, sem publicação automática.

## 🚀 Uso Básico

### Consulta Simples

```csharp
using GetCNPJ;
using System;

// Criar cliente
using var client = new CnpjClient();

// Consultar CNPJ (numérico ou alfanumérico, com ou sem máscara; suporte externo depende da API)
var result = await client.GetAsync("03.312.791/0001-83");

if (result.Success && result.Data != null)
{
    var data = result.Data;

    Console.WriteLine($"Razão Social: {data.RazaoSocial}");
    Console.WriteLine($"Nome Fantasia: {data.NomeFantasia}");
    Console.WriteLine($"CNPJ: {data.Cnpj}");
    Console.WriteLine($"Situação: {data.Situacao}");
    Console.WriteLine($"Endereço: {data.Endereco?.EnderecoCompleto}");
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
// A aplicação cria e controla a vida deste HttpClient, reutilizado entre consultas.
using var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(30)
};

using var client = new CnpjClient(httpClient);
var result = await client.GetAsync("03312791000183");
```

`CnpjClient` usa uma única instância de HttpClient, compartilhada por todos os seus provedores e consultas. Reutilize o cliente entre consultas; não crie um HttpClient para cada requisição. Quando você fornece o HttpClient, ele é emprestado: a aplicação continua responsável pelo seu descarte, inclusive se o construtor de CnpjClient falhar. Se ele vier de um serviço/factory da aplicação, siga o ciclo de vida definido por esse serviço.

Sem HttpClient fornecido, CnpjClient cria e possui a instância. `Dispose` libera esse cliente e seu handler uma única vez; chamadas repetidas são seguras. Se a configuração ou a construção falhar depois da criação, a instância própria também será descartada. Use `using`/`Dispose` ao encerrar o ciclo de vida de CnpjClient.

Os provedores descartam cada HttpResponseMessage após ler e mapear a resposta, inclusive em erros HTTP, JSON inválido e fallback. O descarte inclui conteúdo e streams associados; o resultado contém somente dados mapeados. `IsAvailableAsync`, herdado da base pelos quatro provedores, também descarta suas respostas e preserva o retorno booleano existente. O buffering/timeout de GetAsync continua igual ao da T04. Referências: [descarte de respostas](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpresponsemessage.dispose?view=net-9.0) e [reutilização de HttpClient](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines).

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

## Timeout, fallback e cancelamento

`GetAsync`/`GetCnpjAsync` consultam os provedores em ordem de prioridade: CNPJWS, ReceitaWS, BrasilAPI e CNPJA na configuração padrão. Falha, ausência de dados ou timeout permitem uma tentativa no próximo provedor. Cada provedor é consultado uma vez, sem retry, e a primeira resposta com dados encerra a consulta. Se todos falharem, `Success` será `false` e `Errors` preservará nomes, mensagens e exceções das tentativas, incluindo timeouts.

`GetFromProviderAsync`/`GetCnpjFromProviderAsync` consultam somente o provedor escolhido. Um timeout retorna resultado de falha com o erro desse provedor; não ativa fallback para outros.

O estado do `CancellationToken` recebido do consumidor determina o cancelamento da consulta. Quando cancelado, a operação propaga `OperationCanceledException` (inclusive sua subclasse `TaskCanceledException`) e não inicia novas tentativas. O token é verificado antes e depois das consultas e repassado ao HTTP e ao rate limiter. Cancelamento tem precedência mesmo se um provedor retornar dados ou outro erro depois do pedido. Provedores externos devem observar o token durante seu trabalho; o cancelamento é cooperativo.

`OperationCanceledException`/`TaskCanceledException` sem cancelamento do token do consumidor são tratadas como falha do provedor, permitindo fallback. Elas podem representar timeout ou cancelamento interno; o tipo da exceção, isoladamente, não comprova a causa. Os tipos produzidos por timeout variam entre runtimes e handlers, conforme a [documentação de HttpClient.GetAsync](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient.getasync?view=net-9.0). Não é necessário que a exceção tenha um token igual ao token do consumidor ou uma TimeoutException interna.

`CnpjClientOptions.Timeout` configura o timeout HTTP por requisição do HttpClient criado pela biblioteca (30 segundos por padrão). Quando um HttpClient é fornecido, sua configuração de timeout é preservada. Esse limite não inclui a espera do rate limiter nem define um prazo global para a cadeia de fallback; o consumidor pode limitar a consulta inteira pelo seu token. A aquisição atômica continua sendo feita uma vez por tentativa; timeout após aquisição não desfaz o registro.

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
- **Timeout**: Falha do provedor; permite fallback em consulta automática e retorna erro em consulta direcionada
- **Cancelamento do consumidor**: Propaga cancelamento e encerra novas tentativas, inclusive durante espera do rate limiter
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

- [**CNPJ.WS**](https://publica.cnpj.ws) - **API padrão** ⭐ IE documentada no endpoint público integrado
- [ReceitaWS](https://receitaws.com.br) - API gratuita de consulta de CNPJ
- [BrasilAPI](https://brasilapi.com.br) - API pública brasileira
- [CNPJA](https://open.cnpja.com) - API aberta de CNPJ

**Nota**: CNPJ.WS continua como primeira opção (prioridade 1), seguido por ReceitaWS, BrasilAPI e CNPJá. Entre os endpoints públicos integrados, o CNPJ.WS documenta IE. Operações comerciais de IE oferecidas por outros serviços não estão integradas. Limites, recursos e condições constam em [docs/PROVIDERS.md](docs/PROVIDERS.md); a biblioteca não concede autorização de uso comercial dos serviços.

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

Para reportar bugs ou solicitar melhorias do fork, abra uma [issue em ThalisonSantos/GetCNPJ](https://github.com/ThalisonSantos/GetCNPJ/issues). Mantenedor: Thalison Santos. Autor original: Bruno da Cunha Felipe. Nenhum logotipo foi incluído nesta distribuição porque a autorização específica de reutilização não foi comprovada.

---

Desenvolvido com ❤️ seguindo as melhores práticas de Clean Code e SOLID
