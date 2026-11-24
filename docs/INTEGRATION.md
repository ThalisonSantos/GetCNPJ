# Homologação manual do GetCNPJ Community no SysEmpresa

Estado em **2026-11-24: preparada para homologação manual**. O SysEmpresa não foi aberto, alterado ou executado nesta tarefa. Os resultados abaixo são de consumidores independentes; somente o mantenedor poderá confirmar a integração no seu ambiente. Versão mantida em1.1.0, sem publicação no NuGet.org ou consultas cadastrais novas.

## Candidato aprovado e pré-requisitos

Use exclusivamente o candidato H03, separado do pacote histórico T07:

- Arquivo: `D:\Meus Projetos\GetCNPJ\artifacts\H03\packages\GetCNPJ.Community.1.1.0.nupkg`.
- Tamanho: **234174 bytes**.
- SHA-256: **`3DD07A96B6B29411B554496C79E1064B45AEEEBDB668AA324A9699B35F5A34FE`**.
- Fonte: commit aprovado `a069e0decc0163f78f9788d204189e4b0d8055e0`.
- InformationalVersion das cinco DLLs: `1.1.0+a069e0decc0163f78f9788d204189e4b0d8055e0`; AssemblyVersion preservada em1.0.0.0.

Confirmar o hash com `Get-FileHash <arquivo> -Algorithm SHA256` antes de copiar/instalar. Não selecionar `artifacts/packages`, que contém o candidato antigo da T07 com a mesma versão. Não regenerar/substituir o arquivo depois da conferência sem novo registro de hash/proveniência.

O SysEmpresa precisa ter target compatível; para o caso legado aqui validado, .NET Framework4.8 e referência System.Net.Http da plataforma. O runtime local de teste foi4.8.1, que substitui4.8; não foi testada máquina isolada com somente4.8. Validado também consumidor net8.0. Os cinco assets continuam netstandard2.0/net48/net481/net8.0/net9.0, sem targets novos. Os exemplos compilados usam C# com nullable habilitado; se o projeto legado usa compilador anterior, adaptar a sintaxe sem alterar a semântica dos campos opcionais.

## Fonte NuGet local e instalação no Visual Studio

1. Em uma cópia/branch de homologação do SysEmpresa, registrar as referências atuais, versão da biblioteca anterior, framework, arquitetura, App.config e dependências antes de mudar. O mantenedor executará esses passos; nada foi modificado automaticamente.
2. Copiar o nupkg conferido para uma pasta exclusiva ou usar a pasta H03 acima. Não misturar candidatos1.1.0 diferentes na mesma fonte.
3. No Visual Studio, abrir **Ferramentas → Gerenciador de Pacotes NuGet → Configurações do Gerenciador de Pacotes → Origens de Pacote**. Adicionar `GetCNPJ-H03-local` com o caminho da pasta que contém o nupkg, habilitar e aplicar. Manter NuGet.org para dependências públicas; a biblioteca vem da fonte local.
4. No projeto SysEmpresa, abrir **Gerenciar Pacotes NuGet**, selecionar a origem local, procurar `GetCNPJ.Community`, escolher exatamente1.1.0 e instalar. Não habilitar prévias. Alternativa no Console do Gerenciador: `Install-Package GetCNPJ.Community -Version 1.1.0 -Source "D:\Meus Projetos\GetCNPJ\artifacts\H03\packages" -ProjectName SysEmpresa`. Ajustar nome do projeto/pasta ao ambiente real.
5. Restaurar, recompilar e conferir o grafo e a cópia de `GetCNPJ.dll` usada pelo executável. O namespace continua `GetCNPJ`; o nome do assembly também.

Fontes oficiais: [fontes locais](https://learn.microsoft.com/en-us/nuget/hosting-packages/local-feeds), [instalação pelo Visual Studio](https://learn.microsoft.com/en-us/nuget/consume-packages/install-use-packages-visual-studio). Os nomes de menus podem variar com idioma/versão.

Para reprodução com PackageReference e NuGet6+, usar um NuGet.Config dedicado, preservando eventuais fontes privadas de outros pacotes do SysEmpresa. Exemplo mínimo (caminho local deve ser ajustado):

```xml
<configuration>
  <packageSources>
    <clear />
    <add key="GetCNPJ-H03-local" value="D:\Meus Projetos\GetCNPJ\artifacts\H03\packages" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <clear />
    <packageSource key="GetCNPJ-H03-local"><package pattern="GetCNPJ.Community" /></packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  </packageSourceMapping>
  <config><add key="globalPackagesFolder" value="./.packages-h03-a069e0d" /></config>
</configuration>
```

O padrão exato faz a biblioteca ser restaurada da fonte local; `*` atende as dependências. Usar cache **novo e exclusivo**, pois candidatos diferentes têm o mesmo PackageId/versão e uma cópia em cache pode evitar nova resolução da fonte. Não apagar o cache global de outros projetos para isso. Conferir `.nupkg.metadata`, hash do nupkg em cache e InformationalVersion da DLL carregada. Ferramentas antigas podem ignorar source mapping; conferir compatibilidade do NuGet instalado. [Documentação oficial de source mapping](https://learn.microsoft.com/en-us/nuget/consume-packages/package-source-mapping).

Se o SysEmpresa usa `packages.config`, manter seu fluxo existente de instalação/restore e examinar as entradas de packages.config, HintPath e DLLs em bin; este ensaio H03 usou somente PackageReference, não homologou o fluxo packages.config. Não migrar o formato automaticamente só para instalar a biblioteca. Usar uma pasta de pacotes de homologação isolada e conferir o conteúdo efetivamente usado pelo executável.

## Substituição da biblioteca anterior

`GetCNPJ` e `GetCNPJ.Community` **não podem coexistir**, inclusive transitivamente: ambos expõem GetCNPJ.dll e os mesmos tipos. Antes de instalar Community, localizar e remover a referência ao pacote anterior e eventuais referências manuais/cópias antigas de GetCNPJ.dll no projeto de homologação. Se outro pacote traz GetCNPJ transitivamente, resolver essa origem antes de prosseguir; adicionar Community por cima não elimina o conflito. Não editar/renomear DLLs para mascarar duplicidade.

Revisar todos os projetos da solução que usam esses tipos; limpar/recompilar as saídas pelo Visual Studio e conferir o caminho de carregamento. A API CLR foi preservada, mas recompilar pode revelar avisos nullable corretos nos dados opcionais. Corrigir o uso com verificações de presença; não silenciar globalmente Nullable ou preencher dados desconhecidos com zero/false/vazio. Manter um registro das referências anteriores para reverter a instalação caso a homologação manual falhe.

## Dependências, assemblies e binding redirects

| Ambiente | Dependências relevantes | Conferência necessária |
|---|---|---|
| net48/net481/netstandard2.0 | System.Text.Json>=8.0.6 e transitivas | Comparar o grafo existente do SysEmpresa e o resolvido no restore; não forçar downgrade se outra dependência exige versão maior. Conferir APIs usadas por todas as bibliotecas. |
| net48/net481 | System.Net.Http da plataforma, sem pacote HTTP4.3.4 requerido pelo GetCNPJ | Evitar cópia manual de DLL de outro runtime. Não remover pacote HTTP existente de outra biblioteca sem analisar seu grafo. |
| net8/net9 | JSON/HTTP fornecidos pelo framework; grupo NuGet do GetCNPJ sem pacote redundante | Manter runtime corrigido; outras dependências do aplicativo podem ter seus próprios requisitos. |

No consumidor net48 foram carregados STJ assembly8.0.0.6, Encodings.Web8.0.0.0, Unsafe6.0.0.0 e Memory4.0.1.2; System.Net.Http veio da plataforma. Esses são resultados do grafo isolado, não versões a copiar manualmente no SysEmpresa. Versão NuGet não é necessariamente a versão do assembly.

Ativar geração de redirects no projeto executável Framework, se aplicável ao formato/projeto, e conferir o arquivo **gerado** `SysEmpresa.exe.config`:

```xml
<PropertyGroup>
  <AutoGenerateBindingRedirects>true</AutoGenerateBindingRedirects>
  <GenerateBindingRedirectsOutputType>true</GenerateBindingRedirectsOutputType>
</PropertyGroup>
```

O consumidor net48 gerou redirect de System.Runtime.CompilerServices.Unsafe para6.0.0.0. Isso não autoriza copiar esse redirect cegamente para outro grafo. Em FileLoadException/TypeLoadException/MissingMethodException, conferir nome/versão/publicKeyToken requisitados, caminho e versão carregados, duplicidades, assets/HintPath e config efetivo. Corrigir resolução/redirects no executável com versões compatíveis; não suprimir avisos de conflito ou ignorar auditoria/assinaturas NuGet. STJ de major diferente e APIs incompatíveis não são resolvidos apenas com redirect. Consulte [redirects oficiais](https://learn.microsoft.com/en-us/dotnet/framework/configure-apps/redirect-assembly-versions) e [DEPENDENCIES.md](DEPENDENCIES.md).

## Ciclo de vida e chamada assíncrona WinForms

Criar um CnpjClient para o ciclo da aplicação/serviço e reutilizá-lo entre formulários. Recriar a cada clique reinicia o limiter local e prejudica o compartilhamento. O HttpClient deve seguir o mesmo princípio. Se fornecido externamente, o wrapper não o descarta; o proprietário encerra ambos no desligamento, após cancelar/encerrar consultas em andamento. Se criado internamente pelo CnpjClient, este o descarta. Dispose repetido é seguro; não planejar novas consultas em um wrapper encerrado.

Exemplo de composição no ponto de entrada (adaptação pelo mantenedor, não executada no SysEmpresa):

```csharp
using (var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(30) })
using (var cnpj = new GetCNPJ.CnpjClient(http))
{
    Application.Run(new ConsultaCnpjForm(cnpj));
}
```

Com HttpClient externo, configure seu Timeout nele; `CnpjClientOptions.Timeout` configura apenas a instância criada internamente. O timeout é por tentativa HTTP, não prazo global para toda a cadeia/espera do limiter. Cancelamento explícito recebido via token interrompe novas tentativas; timeout interno permite o próximo provedor no modo automático. Consulta dirigida não faz fallback nem retry.

Exemplo completo de formulário compilado contra o nupkg: [ConsultaCnpjForm.cs](../tools/GetCNPJ.PackageValidation/FrameworkConsumer/ConsultaCnpjForm.cs). O formulário recebe o cliente compartilhado, usa `await _client.GetAsync(..., cancellation.Token)` no evento do botão, cancela pelo botão/fechamento, desabilita consultas simultâneas naquele formulário e guarda ausência/erro antes de usar Data. Não chama `.Result`, `.Wait()` ou `.GetAwaiter().GetResult()` na UI. No evento, manter o contexto para atualizar controles; ConfigureAwait(false) fica apropriado na camada interna da biblioteca. [Orientação Microsoft sobre async e bloqueio](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-scenarios).

Núcleo do tratamento, usado no exemplo:

```csharp
var result = await _client.GetAsync(textoCnpj, cancellation.Token);
if (!result.Success || result.Data == null)
{
    // Exibir indisponibilidade e permitir revisão manual, sem salvar cadastro parcial.
    return;
}
var draft = new CustomerDraft(result.Data, ufSelecionada);
// Exibir para confirmação do operador antes de persistir.
```

Tratar InvalidCnpjException como entrada inválida e OperationCanceledException com token cancelado como cancelamento solicitado; não exibir como cadastro inexistente. Resultado de falha ou ausência não prova inexistência nem rejeição fiscal. Não mostrar/gravar ErrorMessage/Exception.Message brutos: podem conter JSON, contatos e sócios. Registrar somente provedor, categoria/tipo de falha, duração e contexto mínimo necessário. Campos desconhecidos devem continuar desconhecidos; o operador decide se mantém o dado anterior ou faz conferência manual.

## Rascunho de cadastro, IE por UF e informações opcionais

Exemplo compilado para clientes/fornecedores: [IntegrationMapping.cs](../tools/GetCNPJ.PackageValidation/Shared/IntegrationMapping.cs). CustomerDraft não persiste nada, normaliza CNPJ preservando letras e guarda os campos opcionais e as IEs correspondentes à UF. Não representa o schema real do SysEmpresa.

| Destino sugerido | Origem / regra |
|---|---|
| Identificador | `CnpjValidator.ValidateAndNormalize(data.Cnpj)` após conferir presença. Usar string para manter letras/zeros; sem long/int ou Regex que retenha apenas dígitos. Banco/controles devem aceitar14 caracteres sem máscara ou18 com máscara. |
| Razão social/fantasia | Strings opcionais originais, sem inventar vazio para ausência; nome fantasia pode ser null ou vazio na fonte. |
| Endereço/CEP | `data.Endereco?.Logradouro/Numero/Complemento/Bairro/Municipio/Uf/Cep`; mapear campos individualmente. Não usar EnderecoCompleto para decidir completude cadastral. CEP em string; conferir máscara esperada pelo destino. |
| IBGE municipal | `data.Endereco?.CodigoIbge`, string nullable; não converter SIAFI em IBGE ou inferir pelo nome do município. |
| CNAEs | `AtividadePrincipal?.Codigo/Descricao`, lista AtividadesSecundarias; guardar zeros e considerar a máscara histórica da biblioteca. Lista vazia não autoriza apagar atividades anteriores automaticamente. |
| Simples/MEI | `Simples?.OptanteInformado` e `OptanteSimeiInformado`: true positivo informado, false negativo informado, null desconhecido. Não usar o bool legado para inferir negativa de dado ausente. |
| Capital/datas | `decimal?` e `DateTime?`; preservar null. Converter para o tipo do cadastro sem trocar cultura ou transformar ausência em zero/data padrão. |
| IE | Filtrar pela UF, mantendo número e `AtivoInformado` de cada item; não inferir condição de contribuinte de lista vazia. |

Para selecionar IE pela UF:

```csharp
var candidatas = data.InscricoesEstaduais
    .Where(ie => string.Equals(ie.Estado, uf.Trim(), StringComparison.OrdinalIgnoreCase))
    .ToList();
if (candidatas.Count == 1)
{
    var ie = candidatas[0];
    // ie.Inscricao e ie.AtivoInformado continuam opcionais; null = desconhecido.
}
// Zero: informação indisponível/sem itens; várias: pedir escolha ao operador.
```

IE não localizada, situação IE desconhecida e não contribuinte são estados distintos. A biblioteca não comprova não contribuinte com base em ausência, nem cria flag fiscal desse tipo. `AtivoInformado == false` significa inscrição informada como inativa, não prova não contribuinte. Evitar `InscricaoEstadual.ToString()` legado para classificar situação, pois usa o bool de compatibilidade; apresentar explicitamente desconhecida quando nullable for null.

Antes de gravar, revisar a correspondência do CNPJ retornado, campos obrigatórios do SysEmpresa, preservação de informações existentes, UF e situações. Um HTTP200/sucesso pode conter campos ausentes; CPF/CNPJ localmente válido não garante cadastro. Não copiar dados da matriz para filial por conveniência.

## APIs públicas e limites comerciais

Prioridade fixa: CNPJWS → ReceitaWS → BrasilAPI → CNPJA. Limiter padrão3 aquisições/minuto/provedor/instância, sem coordenação entre processos ou IP compartilhado. CNPJ.WS/ReceitaWS documentam3/min; CNPJá Open5/min/IP; BrasilAPI sem cota numérica confirmada nas fontes consultadas. Não aumentar a opção local como se isso concedesse autorização do provedor. Desabilitar clique duplicado, evitar consultas em massa, parar diante de401/403/429 e não criar retries para contornar limites.

Acesso público gratuito não garante uso comercial irrestrito, redistribuição, cobertura fiscal ou atualização. Rever os termos oficiais aplicáveis antes de colocar no fluxo comercial de clientes/fornecedores; documentação de fontes e restrições em [PROVIDERS.md](PROVIDERS.md). Inscrições estaduais não estão disponíveis em todos os endpoints públicos. Homologação alfanumérica H02.1 foi pontual, não SLA. Novas consultas reais de homologação exigem autorização específica; esta etapa H03 só utilizou HTTP sintético.

## Reprodução dos ensaios independentes e aceite manual

Em `tools/GetCNPJ.PackageValidation`, os projetos FrameworkConsumer(net48/WinForms) e ModernConsumer(net8) usam **somente PackageReference1.1.0**, sem ProjectReference nem fontes da biblioteca. NuGet.Config aponta para H03/packages, mapeamento exato e cache isolado H03/consumer-cache. Não é suíte padrão/CI e não entra no nupkg.

```powershell
dotnet restore tools/GetCNPJ.PackageValidation/FrameworkConsumer/FrameworkConsumer.csproj --configfile tools/GetCNPJ.PackageValidation/NuGet.Config
dotnet restore tools/GetCNPJ.PackageValidation/ModernConsumer/ModernConsumer.csproj --configfile tools/GetCNPJ.PackageValidation/NuGet.Config
dotnet build tools/GetCNPJ.PackageValidation/FrameworkConsumer/FrameworkConsumer.csproj -c Release --no-restore
dotnet build tools/GetCNPJ.PackageValidation/ModernConsumer/ModernConsumer.csproj -c Release --no-restore
& tools/GetCNPJ.PackageValidation/FrameworkConsumer/bin/Release/net48/FrameworkConsumer.exe
dotnet tools/GetCNPJ.PackageValidation/ModernConsumer/bin/Release/net8.0/ModernConsumer.dll
```

Passaram **24 cenários por consumidor**, incluindo numérico/alfanumérico, quatro provedores×dois modos×duas entradas, campos opcionais, mapeamento rico e rascunho por UF, cancelamento antes/durante HTTP, timeout com fallback/dirigido, compartilhamento/descarte e proveniência. FrameworkConsumer também exercitou STA, message loop WinForms real, await e atualização de controle sem janela visível. Isso não equivale a teste visual, designer, arquitetura de implantação ou execução do SysEmpresa. Logs em artifacts/H03/consumer-*.log. Dependências instaladas vieram de NuGet.org; handlers de consulta não abrem conexões reais.

Aceite ainda **pendente do mantenedor no SysEmpresa**: instalar o hash indicado, confirmar dependências/redirects carregados, recompilar sem conflitos, abrir formulário, verificar responsividade/cancelamento/fechamento, revisar o rascunho e persistência sem perda de dados, conferir IEs/UF e desconhecidos. Qualquer consulta real deve ter autorização específica. Registrar ambiente/versão do SysEmpresa, arquitetura, resultados e limitações, sem dados de clientes/credenciais. Até essa confirmação, H03 permanece **preparada para homologação manual**, não integração concluída.
