# GetCNPJ Community — preparação de release 1.1.0

Verificação: **2026-11-24**. T07 concluída como preparação/validação **local**. **Nenhuma publicação, tag, GitHub Release, commit, push ou merge foi realizada.** Versão 1.2.0 não iniciada. Este procedimento não autoriza publicação; ela depende de autorização específica do mantenedor.

## Identidade e atribuição

| Campo | Valor |
|---|---|
| PackageId / versão | `GetCNPJ.Community` / `1.1.0` |
| Title | GetCNPJ Community |
| Mantenedor do fork independente | Thalison Santos |
| Autor original | Bruno da Cunha Felipe, projeto [azumamagus/GetCNPJ](https://github.com/azumamagus/GetCNPJ) |
| Authors no nuspec | Bruno da Cunha Felipe,Thalison Santos |
| RepositoryUrl / PackageProjectUrl | https://github.com/ThalisonSantos/GetCNPJ |
| RepositoryType / licença | git / MIT |
| Namespace / assembly | `GetCNPJ` / `GetCNPJ.dll` |
| AssemblyVersion / FileVersion | `1.0.0.0` preservada / `1.1.0.0` |
| Targets | netstandard2.0, net48, net481, net8.0, net9.0 |

Não instalar `GetCNPJ` e `GetCNPJ.Community` simultaneamente, inclusive por dependências transitivas: contêm a mesma DLL e os mesmos tipos. A migração troca o PackageReference, mantendo os namespaces. Não há mudança funcional de código C# na T07. Informações fiscais desconhecidas, campos aditivos e endurecimentos anteriores continuam documentados em [PROVIDERS.md](PROVIDERS.md) e CHANGELOG.md.

`LICENSE` foi preservado **byte a byte**, inclusive `Copyright (c) 2025 Bruno da Cunha Felipe`, e incluído integralmente no pacote. SHA-256 do arquivo original/preservado: `779E6F6DB397233CCD7AF0E7926846990E118613FB4BB2DA6A5BA9F18E4044F0`. O nuspec usa `license type="expression"` = MIT; o SDK também gera o link padrão licenses.nuget.org/MIT. Nenhum aviso de copyright original foi removido/substituído.

Company FeTx foi removida por não representar o fork. A atribuição original permanece em Authors, Copyright, LICENSE, descrição e README. A propriedade Company gerada pelo SDK deriva de Authors, sem FeTx. O logotipo foi adicionado no histórico pelo commit `56956eb4d114cd26bc5a0a4672b4c16fcbcf16eb`, mas não foi localizada autorização específica de reutilização da identidade visual. Por decisão conservadora solicitada pelo mantenedor, foi retirado de PackageIcon, conteúdo NuGet e README. `logo.png` permanece somente no Git como arquivo histórico. Reintrodução depende de comprovação de autorização ou identidade visual própria; não foi criada nova imagem nesta tarefa.

## Verificação do nome no NuGet.org

Em 2026-11-24, 17:06:21–22 UTC (14:06:21–22 America/Sao_Paulo), consultas HTTP de leitura retornaram:

| Fonte oficial | Resultado |
|---|---|
| [Página GetCNPJ.Community](https://www.nuget.org/packages/GetCNPJ.Community) | HTTP 404 |
| [Índice de versões do pacote](https://api.nuget.org/v3-flatcontainer/getcnpj.community/index.json) | HTTP 404 |

Evidência local: `artifacts/t07-package-id.json`. Primeira tentativa falhou por TLS no sandbox; a repetição com acesso de rede autorizado obteve os códigos acima. O [guia oficial de autoria NuGet](https://learn.microsoft.com/en-us/nuget/create-packages/package-authoring-best-practices) recomenda conferir a existência da página do ID. A evidência indica ausência de pacote público nas consultas realizadas; **não reserva o nome**, não comprova ausência de restrições/prefixos reservados e não garante aceitação futura pelo servidor. Não foi testado upload para verificar o nome. Repetir a conferência quando a publicação for autorizada.

## Resultado de validação local

Ambiente: Windows 10.0.26200 x64, SDK 11.0.100/MSBuild 18.12.2. Runtime .NET 8.0.34; runtime .NET 9.0.23 local em `artifacts/dotnet9`; .NET Framework 4.8.1 instalado (Release 533509). O SDK 11 utilizado não adiciona frameworks ao pacote. Workflow existente `.github/workflows/main.yml` preservado integralmente: restore/build/test, sem pack/push/segredo de publicação.

| Etapa | Resultado / evidência |
|---|---|
| Restore + NuGetAudit/all | Aprovado após repetição com rede autorizada. Tentativa sandbox teve NU1900 e não foi considerada auditoria concluída. `artifacts/t07-restore.log`. |
| Diretas/transitivas | Inventário `artifacts/t07-packages.json`; `dotnet list ... --vulnerable --include-transitive --format json`: saída 0, nenhum pacote vulnerável reportado nos dois projetos. `artifacts/t07-vulnerable.json`. Falha TLS inicial não foi contada como resultado negativo. |
| Build Release/rebuild | Cinco targets compilados, **1275 avisos**, zero erros. `artifacts/t07-build.log` e `t07-build-final.log`. Nenhum aviso suprimido. |
| Testes | **360 aprovados por target executável**, zero falhas/ignorados, **1440 execuções**. Casos/expectativas da T06 intactos. TRX em `TestResults/T07/`; execução final em `FinalSolution/` e `FinalNet9/`. |
| Comando global de testes | Saída 1 pela ausência de runtime 9 global; net48/net481/net8 passaram. net9 passou separadamente com DotNetHostPath local, sem roll-forward. Aviso VSTEST_ARTIFACTS_DIRECTORY do SDK11/Test SDK17.12 não impediu descoberta/execução. |
| dotnet pack | Aprovado, sem avisos na execução final. Tentativas iniciais falharam com NU5039 por exclusão indevida do README; lista explícita corrigida para avaliação externa dos múltiplos targets. |
| Inspeção | 14 entradas: cinco assemblies, cinco documentos e quatro arquivos de metadados NuGet/OPC. Nuspec/identidade/dependências e bytes dos documentos conferidos. `artifacts/t07-inspection.log`, `t07-package-manifest.txt`, `t07-package.nuspec`. |
| Consumidores independentes | Projetos net48 e net8, apenas PackageReference (sem ProjectReference/cópia de fontes da biblioteca). Restore de fonte local com cache isolado, build e execução aprovados, ambos com zero avisos/erros de build. |

Os cinco assemblies dentro do ZIP são idênticos aos arquivos compilados em `bin/Release/<target>/GetCNPJ.dll` e mantêm nome GetCNPJ/AssemblyVersion 1.0.0.0. Não há renomeação implícita da DLL pelo novo PackageId.

### Revisão dos 1275 avisos

Foram inventariados os diagnósticos por arquivo, linha, coluna, código, target **e mensagem**. O console repete diagnósticos no resumo; essa repetição foi descartada, mas mensagens de propriedades diferentes no mesmo construtor foram preservadas. Resultado: **255 diagnósticos por target × cinco targets = 1275**. CSV integral local: `artifacts/t07-warnings.csv`. Todos são de nulabilidade, nos modelos/DTOs/mapeamentos/base existentes, não no código dos testes.

| Código | Por target | Total | Natureza / risco residual |
|---|---:|---:|---|
| CS8618 | 225 | 1125 | Propriedades de referência não inicializadas em DTOs e modelos, inclusive objetos preenchidos pela desserialização e resultados com dados/erro opcionais. Campo ausente pode permanecer null apesar de anotação não nullable; consumidor pode sofrer NullReferenceException se confiar só na anotação. |
| CS8601 | 22 | 110 | Mapeamento de fontes opcionais para propriedades não nullable (endereço, email, descrições etc.). Ausência legítima não deve virar informação inventada; assinatura/anotação atual não representa perfeitamente essa ausência. |
| CS8603 | 5 | 25 | Base e quatro FetchDataAsync podem retornar null para ausência de dados, embora declarados não nullable. Serviço trata esse resultado; consumidores diretos de provedores precisam considerar ausência. |
| CS8625 | 1 | 5 | `CnpjResult.CreateError(..., errors = null)` aceita argumento padrão null em referência não nullable; implementação converte em lista vazia, mas anotação é incoerente. |
| CS8629 | 1 | 5 | `codigo.Value` no Select da BrasilAPI após Where com HasValue. Fluxo entre lambdas não é reconhecido pelo compilador; há filtro antes do acesso no fluxo atual, sem compartilhamento do DTO entre consultas. Preservar o filtro em alterações futuras. |
| CS8604 | 1 | 5 | CEP opcional do CNPJá passado a FormatCep não nullable. Método verifica IsNullOrWhiteSpace antes de acessar comprimento; comportamento tolera null, anotação não expressa isso. |

A maioria descreve diferença entre modelos públicos/DTOs e dados opcionais, mas **não é seguro declarar todos inofensivos**: campos podem ser null em respostas parciais e afetar consumidores ou alterações futuras. Testes simulados cobrem cenários específicos, não todo JSON/handler externo possível. Nenhum Nullable/NoWarn/pragma foi alterado; não houve inicialização artificial, mudança de contrato ou refatoração global para zerar o número. Tratar adequadamente a nulabilidade requer tarefa separada e revisão de compatibilidade, fora da versão 1.2.0 nesta sessão.

## Conteúdo do pacote e dependências

```text
GetCNPJ.Community.nuspec
lib/netstandard2.0/GetCNPJ.dll
lib/net48/GetCNPJ.dll
lib/net481/GetCNPJ.dll
lib/net8.0/GetCNPJ.dll
lib/net9.0/GetCNPJ.dll
README.md
LICENSE
CHANGELOG.md
docs/PROVIDERS.md
docs/DEPENDENCIES.md
_rels/.rels
[Content_Types].xml
package/services/metadata/core-properties/nuget.psmdcp
```

Dependências declaradas no nuspec: System.Text.Json >= 8.0.6 somente em netstandard2.0/net48/net481; net8/net9 com grupos vazios (componentes fornecidos pelo framework). System.Net.Http como frameworkAssembly apenas em net48/net481. Sem dependências de testes, referência ao projeto original ou pacote GetCNPJ. Grafo transitivo/justificativas em [DEPENDENCIES.md](DEPENDENCIES.md).

Nenhum arquivo de teste, fonte C#, PDB, log, TRX, credencial, NuGet.Config, bin/obj temporário, script de validação ou logo incluído. Itens de conteúdo e None são excluídos por padrão de empacotamento e somente os documentos acima são explicitamente incluídos; assemblies vêm dos outputs padrão. `IncludeSource=false`; `GeneratePackageOnBuild=false`. **Este RELEASE.md não é empacotado**, pois contém hash/evidências do próprio nupkg; isso evita referência circular. Documentos de acompanhamento/release permanecem no repositório.

### Artefato final inspecionado e consumido

- Caminho relativo: `artifacts/packages/GetCNPJ.Community.1.1.0.nupkg`
- Caminho absoluto: `D:\Meus Projetos\GetCNPJ\artifacts\packages\GetCNPJ.Community.1.1.0.nupkg`
- Tamanho: **229798 bytes**.
- SHA-256: **`04F2FB78207597463E4BFC71B38AEB57AA4E648B0DF56E7B5407E2F475F44EB6`**.

Hash verificado também no nupkg extraído para o cache dos consumidores, garantindo que receberam exatamente esse arquivo da fonte local. Arquivos `artifacts/` e `TestResults/` são ignorados pelo Git. Nenhum nupkg foi publicado ou versionado.

O nuspec registra `repository commit=8652242894604cf28c96121ea2bee67b51fa0a7a`, base da árvore local; InformationalVersion é `1.1.0+8652242894604cf28c96121ea2bee67b51fa0a7a`. **Esse commit não contém as alterações locais da T07.** Portanto o pacote é candidato de preparação, não uma release com rastreabilidade final para commit aprovado. Uma geração futura pode produzir outro tamanho/hash; nunca reutilizar este hash para outro arquivo.

## Procedimento de geração e inspeção

Executar na raiz, em develop/feature/*/fix/* autorizada; conferir árvore de trabalho sem sobrescrever alterações de terceiros. Não executar estes comandos como publicação implícita:

```powershell
git status --short
git branch --show-current
dotnet restore GetCNPJ.sln --force-evaluate --no-http-cache -p:NuGetAudit=true -p:NuGetAuditMode=all
dotnet list GetCNPJ.sln package --include-transitive --format json
dotnet list GetCNPJ.sln package --vulnerable --include-transitive --format json
dotnet build GetCNPJ.sln -c Release -t:Rebuild
dotnet test GetCNPJ.sln -c Release --no-build --settings tests/test.runsettings --logger trx --results-directory TestResults
dotnet pack GetCNPJ.csproj -c Release --no-build --no-restore --output artifacts/packages
Get-Item artifacts/packages/GetCNPJ.Community.1.1.0.nupkg | Select-Object FullName,Length
Get-FileHash artifacts/packages/GetCNPJ.Community.1.1.0.nupkg -Algorithm SHA256
```

Falhas NU1900/NU1301/TLS impedem concluir a auditoria; saída 0 de restore com aviso de auditoria não equivale a auditoria aprovada. Não ignorar vulnerabilidades nem falhas de descoberta. Nesta máquina, repetir testes por target, usando o host local para net9 conforme [tests/README.md](../tests/README.md):

```powershell
dotnet test GetCNPJ.sln -c Release --no-build -f net9.0 --settings tests/test.runsettings --logger trx --results-directory TestResults -- "RunConfiguration.DotNetHostPath=D:/Meus Projetos/GetCNPJ/artifacts/dotnet9/dotnet.exe"
```

Abrir o nupkg como ZIP, ler o nuspec e conferir lista de entradas, grupos de dependências, cinco DLLs e identidade acima; comparar os bytes de LICENSE/README/CHANGELOG/docs com os originais. Qualquer arquivo inesperado deve ser investigado antes da release. Não descompactar caminhos arbitrários de ZIP sem validar destino. A inspeção nesta sessão usou script local `artifacts/Inspect-T07Package.ps1` com lista explícita; a política de scripts exigiu execução com liberação apenas no processo (`powershell -NoProfile -ExecutionPolicy Bypass -File ...`), sem alteração da política da máquina.

## Reprodução dos consumidores independentes

Foram criados dois projetos em `artifacts/T07Consumers/FrameworkConsumer` e `ModernConsumer`, separados da solução e excluídos da compilação/empacotamento da biblioteca. Nenhum ProjectReference nem fonte de implementação do GetCNPJ foi usado. Para repetir, criar os seguintes arquivos (nomes dos csproj conforme as pastas).

`FrameworkConsumer/FrameworkConsumer.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net48</TargetFramework>
    <AutoGenerateBindingRedirects>true</AutoGenerateBindingRedirects>
  </PropertyGroup>
  <ItemGroup><PackageReference Include="GetCNPJ.Community" Version="1.1.0" /></ItemGroup>
</Project>
```

Para `ModernConsumer/ModernConsumer.csproj`, usar o mesmo modelo com `TargetFramework=net8.0` (AutoGenerateBindingRedirects não é necessário). Em ambas as pastas colocar `Program.cs` com validação somente local:

```csharp
using System;
using GetCNPJ.Validation;
internal static class Program
{
    private static void Main()
    {
        var numeric = CnpjValidator.ValidateAndNormalize("12.345.678/0001-95");
        var alpha = CnpjValidator.ValidateAndNormalize("12.abc.345/01de-35");
        if (numeric != "12345678000195" || !CnpjValidator.IsValid(numeric)) throw new Exception("Numérico");
        if (alpha != "12ABC34501DE35" || !CnpjValidator.IsValid(alpha)) throw new Exception("Alfanumérico");
        if (CnpjValidator.Format(alpha) != "12.ABC.345/01DE-35") throw new Exception("Máscara");
        if (CnpjValidator.IsValid("12345678000194") || CnpjValidator.IsValid("12ABC34501DE34")) throw new Exception("DV");
        var assembly = typeof(CnpjValidator).Assembly;
        if (assembly.GetName().Name != "GetCNPJ" || assembly.GetName().Version.ToString() != "1.0.0.0") throw new Exception("Assembly");
        Console.WriteLine("APROVADO: " + numeric + "; " + alpha + "; " + assembly.Location);
    }
}
```

Em `artifacts/T07Consumers/NuGet.Config` (o caminho local é relativo a esse arquivo):

```xml
<configuration>
  <packageSources>
    <clear />
    <add key="t07-local" value="../packages" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <clear />
    <packageSource key="t07-local"><package pattern="GetCNPJ.Community" /></packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  </packageSourceMapping>
  <config><add key="globalPackagesFolder" value="./final-packages" /></config>
</configuration>
```

O padrão exato garante GetCNPJ.Community da fonte local; NuGet.org fornece apenas as dependências. Usar **cache novo** para cada pacote refeito com a mesma versão, evitando consumir cópia anterior sem perceber. O cache novo nesta sessão foi `final-packages` e `.nupkg.metadata` confirmou origem `D:\Meus Projetos\GetCNPJ\artifacts\packages`; SHA-256 do nupkg no cache corresponde ao artefato final.

```powershell
dotnet restore artifacts/T07Consumers/FrameworkConsumer/FrameworkConsumer.csproj --configfile artifacts/T07Consumers/NuGet.Config -p:NuGetAudit=true -p:NuGetAuditMode=all
dotnet restore artifacts/T07Consumers/ModernConsumer/ModernConsumer.csproj --configfile artifacts/T07Consumers/NuGet.Config -p:NuGetAudit=true -p:NuGetAuditMode=all
dotnet build artifacts/T07Consumers/FrameworkConsumer/FrameworkConsumer.csproj -c Release --no-restore
dotnet build artifacts/T07Consumers/ModernConsumer/ModernConsumer.csproj -c Release --no-restore
& artifacts/T07Consumers/FrameworkConsumer/bin/Release/net48/FrameworkConsumer.exe
dotnet artifacts/T07Consumers/ModernConsumer/bin/Release/net8.0/ModernConsumer.dll
```

Ambos retornaram saída 0 e validações aprovadas, incluindo rejeição dos DVs inválidos. `project.assets.json` selecionou `lib/net48/GetCNPJ.dll` e `lib/net8.0/GetCNPJ.dll`, respectivamente. Logs em `artifacts/t07-consumer-net48-*` e `t07-consumer-net8-*`; fontes e caches temporários ignorados pelo Git. Primeira restauração Framework falhou por TLS para assinatura do repositório NuGet, repetida com rede autorizada sem desabilitar verificação de assinatura/auditoria. O consumidor Framework executou no runtime 4.8.1 instalado; não equivale a máquina isolada com somente 4.8. Não houve consumo separado do nupkg por projetos net481/net9/netstandard; esses assemblies foram inspecionados, compilados e os targets executáveis testados na solução.

## Limitações e pendências antes de publicar

- T06 foi validada remotamente pelo mantenedor na execução [45443368700](https://github.com/ThalisonSantos/GetCNPJ/actions/runs/45443368700), commit 8652242894604cf28c96121ea2bee67b51fa0a7a. Revisão ChatGPT **parcial**, sem bloqueios nos trechos vistos; não anunciar revisão integral. T07 ainda sem validação remota/revisão do mantenedor nesta sessão.
- Nenhum provedor real foi consultado. BrasilAPI/ReceitaWS possuem confirmação documental alfanumérica; CNPJ.WS/CNPJá não confirmados. **Não há suporte operacional alfanumérico comprovado por teste real autorizado em nenhum dos quatro.** Termos, cotas, defasagem, cobertura e restrições comerciais em PROVIDERS.md continuam aplicáveis.
- Persistem os 1275 avisos descritos; modelos não nullable podem conter null. API pública/namespace/assembly e 360 casos por target preservados; testes não demonstram ausência de toda regressão possível.
- Auditoria NuGet não reportou vulnerabilidades conhecidas nessa data, sem garantir ausência de falhas não catalogadas ou segurança do Windows/runtime. Manter runtimes corrigidos; não estender suporte oficial dos frameworks por inferência.
- Logotipo permanece fora do pacote. Nome do pacote não reservado nem upload testado. Revalidar disponibilidade/permissões da conta na ocasião autorizada.
- Candidato gerado a partir de árvore com alterações locais não commitadas; hash/proveniência documentados. Quando houver autorização para versionamento/release, gerar novamente a partir da revisão aprovada e registrar o novo hash, repetindo validação/consumo. Não alterar silenciosamente o artefato aprovado.

## Publicação manual futura — somente após autorização específica

O workflow atual **não publica** e não deve receber credenciais/etapas de push como consequência desta tarefa. Publicação futura é ação separada: confirmar pacote/versão, acesso da conta NuGet, evidências, hash do arquivo aprovado e autorização explícita. Não se exige nem executa merge/tag/GitHub Release automaticamente.

Com autorização específica já concedida para **aquele arquivo/versão**, o mantenedor poderá executar manualmente com NuGet 7.6+/SDK 10.0.300+ (inclui o SDK 11 usado nesta validação), que lê `NUGET_API_KEY` do ambiente sem expor a chave como argumento. Este comando **não foi executado**:

```powershell
# Somente após autorização. Chave fornecida ao processo por meio seguro, nunca no Git/log.
dotnet nuget push "./artifacts/packages/GetCNPJ.Community.1.1.0.nupkg" --source "https://api.nuget.org/v3/index.json"
```

Usar arquivo exato, sem wildcard; não imprimir/gravar a chave, não habilitar publicação automática e não assumir que uma falha/duplicidade é sucesso. Conferir depois a página do pacote, versão, metadados e instalação pública. Orientação oficial: [publicação NuGet.org](https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package). A publicação efetiva está fora do escopo e permanece pendente.
