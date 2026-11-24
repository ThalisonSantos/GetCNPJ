# GetCNPJ Community

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Fork independente do [GetCNPJ de Bruno da Cunha Felipe](https://github.com/azumamagus/GetCNPJ), mantido por **Thalison Santos** em [ThalisonSantos/GetCNPJ](https://github.com/ThalisonSantos/GetCNPJ). Licença [MIT](LICENSE) e copyright original preservados. Sem afiliação oficial com a Receita Federal ou com os provedores.

**GetCNPJ.Community 1.1.0 está preparada para publicação, ainda não publicada por esta preparação.** T01–T07, H01, H02, H02.1 e H03 foram concluídas nos escopos registrados. O mantenedor confirmou instalação, compilação e teste no SysEmpresa, funcionando normalmente, com percepção de melhoria em relação à biblioteca original. Esse relato não contém métricas ou detalhes de testes individuais. O pacote definitivo será gerado somente de commit final aprovado, com árvore limpa e CI validado; os candidatos anteriores permanecem identificados no [procedimento de release](https://github.com/ThalisonSantos/GetCNPJ/blob/develop/docs/RELEASE.md).

## Compatibilidade e instalação

| Item | Contrato |
|---|---|
| PackageId / versão | `GetCNPJ.Community` / `1.1.0` |
| Namespace / assembly | `GetCNPJ` / `GetCNPJ.dll` |
| AssemblyVersion | `1.0.0.0` preservada |
| Targets | `netstandard2.0`, `net48`, `net481`, `net8.0`, `net9.0` |
| Provedores, na ordem padrão | CNPJWS → ReceitaWS → BrasilAPI → CNPJA |

**Não instale GetCNPJ e GetCNPJ.Community simultaneamente**, inclusive por dependências transitivas: ambos fornecem a mesma DLL e os mesmos tipos. Remova a referência ao original e confira o grafo antes de migrar. Anotações nullable corrigidas podem exigir checagens/ajustes de overrides ao recompilar; assinaturas CLR existentes foram preservadas.

Após publicação autorizada no NuGet.org:

```powershell
dotnet add package GetCNPJ.Community --version 1.1.0
```

Até lá, a instalação local deve selecionar o **candidato H03**, em `artifacts/H03/packages`, com hash conferido, fonte mapeada e cache isolado conforme o [guia de integração](https://github.com/ThalisonSantos/GetCNPJ/blob/develop/docs/INTEGRATION.md). Não usar a pasta histórica `artifacts/packages`. Esses candidatos não substituem o futuro pacote definitivo.

## Validação local numérica e alfanumérica

```csharp
using GetCNPJ.Validation;

// Vetor oficial RFB/Serpro; não comprova existência de cadastro.
var cnpj = CnpjValidator.ValidateAndNormalize("12.abc.345/01de-35"); // 12ABC34501DE35
var valido = CnpjValidator.IsValid(cnpj);
var formatado = CnpjValidator.Format(cnpj); // 12.ABC.345/01DE-35
```

A [especificação oficial](https://www.gov.br/receitafederal/pt-br/centrais-de-conteudo/publicacoes/documentos-tecnicos/cnpj/manual-dv-cnpj.pdf) usa 12 posições ASCII de `0–9`/`A–Z` e dois DVs numéricos. Numéricos anteriores e zeros iniciais continuam válidos. Aceita identificador sem máscara ou com máscara padrão; minúsculas ASCII viram maiúsculas e espaços externos são tolerados. Caracteres proibidos/máscara incorreta são rejeitados por `ValidateAndNormalize` com `InvalidCnpjException`; `IsValid` retorna false. Letras não são removidas. `Format` é apresentação, sem validar DV/cadastro, e preserva null.

## Consulta e campos opcionais

Os exemplos com GetAsync/Get fazem rede se executados. As validações automatizadas do projeto usam somente HTTP simulado. Reutilize o mesmo cliente durante o ciclo da aplicação; não crie um por clique/consulta.

```csharp
using System;
using System.Threading;
using GetCNPJ;
using GetCNPJ.Exceptions;

using var client = new CnpjClient(); // Em aplicação real, compartilhar entre consultas.
using var cancellation = new CancellationTokenSource();
try
{
    var result = await client.GetAsync("00000000000191", cancellation.Token);
    if (!result.Success || result.Data == null)
    {
        Console.WriteLine("Consulta indisponível; conferir cadastro manualmente.");
        foreach (var failure in result.Errors)
            Console.WriteLine($"Provedor: {failure.ProviderName}; tipo: {failure.Exception?.GetType().Name}");
        return;
    }

    var data = result.Data;
    Console.WriteLine(data.RazaoSocial ?? "Razão social não informada");
    Console.WriteLine(data.Endereco?.CodigoIbge ?? "IBGE não informado");
    bool? simples = data.Simples?.OptanteInformado; // null = desconhecido.
    Console.WriteLine(simples switch { true => "Optante", false => "Não optante informado", null => "Opção desconhecida" });
    foreach (var ie in data.InscricoesEstaduais)
    {
        var situacao = ie.AtivoInformado switch { true => "ativa", false => "inativa", null => "desconhecida" };
        Console.WriteLine($"UF: {ie.Estado}; situação IE: {situacao}");
    }
}
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
    Console.WriteLine("Consulta cancelada pelo consumidor.");
}
catch (InvalidCnpjException)
{
    Console.WriteLine("CNPJ inválido.");
}
```

Consulta dirigida: `await client.GetFromProviderAsync(cnpj, "BrasilAPI", token)`. Os métodos legados `Get(cnpj)` e `GetFromProvider(cnpj, "ReceitaWS")` continuam disponíveis; verifique Success e Data também nesses caminhos. Na UI WinForms/WPF use await para manter responsividade; [exemplo de formulário compilado e integração SysEmpresa](https://github.com/ThalisonSantos/GetCNPJ/blob/develop/docs/INTEGRATION.md).

| Informação | Tratamento |
|---|---|
| Strings/objetos opcionais de CnpjData | Podem ser null: razão/fantasia/situação/endereço/atividade principal/Simples e demais campos anotados no [modelo atual](https://github.com/ThalisonSantos/GetCNPJ/blob/develop/Models/CnpjData.cs) |
| Datas/capital | `DateTime?` / `decimal?`; ausência permanece null |
| Listas | Inicializadas; vazias quando não há itens mapeados, sem comprovar cobertura fiscal |
| IBGE | `Endereco.CodigoIbge`, string nullable; não inferido de SIAFI/nome do município |
| Simples / MEI | `OptanteInformado` / `OptanteSimeiInformado`: true positivo, false negativo informado, null desconhecido |
| Situação IE | `AtivoInformado`, bool nullable; os bools e ToString legados preservam suas projeções de compatibilidade |

IE vazia ou ausente não comprova não contribuinte; IE inativa tampouco equivale a essa condição. Filtre IEs pela UF e peça escolha quando houver várias. Campos aditivos podem aparecer na serialização feita pelo consumidor. Sucesso com identidade correspondente pode conter dados parciais; não preencher desconhecidos com zero/false/vazio nem sobrescrever cadastro automaticamente. Não registrar contatos, sócios ou mensagens/corpos HTTP brutos; ErrorMessage/Exception.Message podem incorporar esses dados.

## Rate limiter, timeout, fallback e recursos HTTP

- Padrão local: três aquisições/minuto/provedor/instância. O limiter usa janela deslizante com tempo monotônico, filas independentes e aquisição/registro atômicos em `AcquireAsync`, sem registro duplicado. Reset/GetAvailableRequests são concorrentes seguros; limites/intervalos não positivos são rejeitados.
- `IRateLimiter` preserva seus quatro membros. Implementações externas antigas continuam no par WaitIfNeededAsync/RecordRequest, que não é atômico; `IAtomicRateLimiter` oferece adesão opcional ao caminho atômico. Após AcquireAsync não chamar RecordRequest para a mesma aquisição.
- Consulta automática tenta na ordem padrão, uma vez por provedor e sem retry; sucesso encerra. Timeout/falha permite o próximo; consulta dirigida não faz fallback. Falha de todos preserva Errors.
- Token cancelado pelo consumidor propaga OperationCanceledException/TaskCanceledException e encerra novas tentativas, inclusive durante a espera do limiter. Cancelamento interno sem esse sinal é falha do provedor. O timeout HTTP não é prazo global da cadeia/espera.
- CnpjClientOptions.Timeout configura o HttpClient interno. Se externo, configure Timeout no próprio HttpClient. A opção MaxRequestsPerMinute não amplia cotas do provedor; conservar o padrão e considerar o tráfego total do IP.
- Uma instância HttpClient é compartilhada pelos provedores. HttpClient externo nunca é descartado pelo wrapper; seu proprietário cuida dele. A instância interna é descartada com o CnpjClient, inclusive em falha de construção. Dispose repetido é seguro.
- Respostas/conteúdos/streams são descartados após uso, inclusive em erro, desserialização, disponibilidade e fallback.

```csharp
using System.Net.Http;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
using var client = new CnpjClient(http);
// Reutilizar ambos; encerrar na ordem client e depois http.
```

## Evidências e limites dos provedores

Em 2026-11-24 os quatro endpoints públicos tiveram resposta alfanumérica com identidade correspondente em **teste real autorizado**, limitado a um cadastro público. BrasilAPI/ReceitaWS também possuem confirmação documental explícita; CNPJ.WS/CNPJá não têm essa confirmação documental nas fontes consultadas. As automáticas reais encerraram no primeiro sucesso CNPJWS; transição após falha permanece coberta por simulação. Não há garantia de todos os registros, disponibilidade futura, atualização ou consistência fiscal.

[PROVIDERS.md](https://github.com/ThalisonSantos/GetCNPJ/blob/develop/docs/PROVIDERS.md) diferencia documentação, operação, cotas, campos e termos comerciais. CNPJ.WS documenta IE no endpoint integrado; os outros não oferecem esse recurso no contrato público usado. Limiter por instância não coordena processos/IP compartilhado e não evita todo429 externo. Acesso gratuito não concede uso comercial/redistribuição irrestritos. A suíte/CI não consultam cadastros reais.

System.Text.Json8.0.6 é referência apenas em netstandard2.0/net48/net481; net8/net9 usam o componente do framework. System.Net.Http vem da plataforma, sem pacote4.3.4 requerido pela biblioteca. Versões e auditoria em [DEPENDENCIES.md](https://github.com/ThalisonSantos/GetCNPJ/blob/develop/docs/DEPENDENCIES.md). Consumidores devem manter runtimes corrigidos e revisar conflitos/redirects no Framework.

## Validação, release e contribuição

Build dos cinco targets e **450 testes por framework executável** (net48/net481/net8/net9), mais **20 cenários offline** da ferramenta manual. Framework4.8 foi executado sobre4.8.1; netstandard é compilado, sem runtime próprio. Evidências e limites em [tests/README.md](https://github.com/ThalisonSantos/GetCNPJ/blob/develop/tests/README.md), [HOMOLOGATION.md](https://github.com/ThalisonSantos/GetCNPJ/blob/develop/docs/HOMOLOGATION.md) e [RELEASE.md](https://github.com/ThalisonSantos/GetCNPJ/blob/develop/docs/RELEASE.md). A homologação manual do SysEmpresa é relato do mantenedor, sem cenário individual ou métrica presumidos.

CI realiza restore/build/test sem pack/publicação automática. Notas e checklist de1.1.0 são rascunhos locais até autorização das etapas externas. Planejamento futuro exclusivamente em [ROADMAP.md](https://github.com/ThalisonSantos/GetCNPJ/blob/develop/ROADMAP.md); nenhuma funcionalidade1.2.0 é parte desta preparação.

Para contribuir, ler AGENTS.md, trabalhar em develop/feature/*/fix/* e abrir revisão no [repositório do fork](https://github.com/ThalisonSantos/GetCNPJ). Relatos de falhas devem omitir dados pessoais/segredos. Mantenedor: Thalison Santos. Autor original: Bruno da Cunha Felipe. Logotipo fora da distribuição por ausência de comprovação de autorização específica.
