# Dependências — auditoria T06

## Auditoria final de1.1.0 — 2026-11-24

Restore com NuGetAudit=true/Mode=all e listagens direta/transitiva/vulnerável da solução repetidos na preparação final. Nenhuma vulnerabilidade conhecida reportada, sem atualização de versões ou supressão de avisos; inventário `artifacts/ReleasePreparation/packages.json`, resultado `vulnerabilities.json`, restore aprovado `restore-authorized.log`. A primeira tentativa restrita retornou NU1900 e não foi considerada auditoria válida; acesso autorizado ao NuGet concluiu sem avisos. Auditoria não cobre componentes do runtime/Windows ou falhas ainda não catalogadas.

Metadados/referências condicionais revisados e mantidos: STJ8.0.6 somente nos legados, HTTP da plataforma, frameworks/contratos preservados. O inventário e as justificativas T06 abaixo permanecem históricos e compatíveis com o grafo final; não afirmam que toda versão é a mais recente. Procedimento de revalidação do futuro commit definitivo em [PUBLICATION.md](PUBLICATION.md).

## Registro histórico T06

Data: **2026-11-24**. Solução inteira, todos os targets e pacotes diretos/transitivos, incluindo infraestrutura de testes e referências implícitas do SDK. Fonte: NuGet.org (NuGet.Config). Inventário e relatórios locais: `artifacts/t06-packages-before.json`, `t06-packages-after.json`, `t06-vulnerable-before.json`, `t06-vulnerable-after.json` (não versionados; reproduzíveis pelos comandos abaixo).

## Decisões por framework

| Referência | netstandard2.0 | net48 / net481 | net8.0 / net9.0 | Justificativa |
|---|---|---|---|---|
| System.Text.Json | Pacote 8.0.6 | Pacote 8.0.6 | Fornecido pelo framework | Atualização estável de patch 8.0.5 → 8.0.6 no ramo 8, sem mudança de major desnecessária; no runtime moderno remover pacote redundante e acompanhar correções do runtime instalado. |
| System.Net.Http | Contrato NETStandard.Library | Assembly da plataforma, Reference explícita na biblioteca e nos testes | Fornecido pelo framework | Pacote 4.3.4 removido: os targets atuais já oferecem as APIs usadas; não há necessidade de distribuir implementação antiga nem seu grafo de dependências. |

Fontes oficiais: [System.Text.Json 8.0.6 e frameworks/dependências](https://www.nuget.org/packages/System.Text.Json/8.0.6), [System.Net.Http 4.3.4](https://www.nuget.org/packages/System.Net.Http/4.3.4), [orientação Microsoft sobre HttpClient/assembly no Framework](https://github.com/microsoft/dotnet/issues/889). Compilação e testes de todos os targets executáveis verificam a troca de referências; não significa teste em instalação isolada do Framework 4.8.

Nenhuma vulnerabilidade conhecida foi reportada para o grafo anterior ou final pelo NuGet Audit nessa verificação. Não atribuímos vulnerabilidade ao System.Text.Json 8.0.5: o [aviso oficial CVE-2024-43485](https://github.com/dotnet/runtime/security/advisories/GHSA-8g4q-xg66-9fp4) lista 8.0.0–8.0.4 como afetadas e 8.0.5 como correção. A atualização para 8.0.6 é manutenção do ramo estável, não alegação de corrigir essa CVE em 8.0.5. Não foram usadas versões preview.

A auditoria NuGet não substitui atualizações de segurança do runtime/.NET Framework/Windows instalados pelo consumidor. No net8/net9, System.Text.Json e HTTP são componentes do framework; o aplicativo hospedeiro deve manter runtime corrigido. Manter esses targets não estende seu ciclo de suporte oficial. Sem novas dependências ou frameworks nesta tarefa.

## Inventário final consolidado e justificativas de manutenção

Versões resolvidas pelo SDK nesta máquina. “Legados” indica netstandard2.0/net48/net481; pacotes da biblioteca também aparecem transitivamente nos testes legados. Referências implícitas dependem da presença de targeting packs.

| Pacote | Versão(ões) | Uso / decisão |
|---|---|---|
| System.Text.Json | 8.0.6 | Direta nos legados; serialização JSON necessária. Atualizada conforme matriz. |
| Microsoft.Bcl.AsyncInterfaces | 8.0.0 | Transitiva de JSON nos legados; suporte de interfaces assíncronas. Mantida resolução compatível. |
| System.Buffers | 4.5.1 | Transitiva JSON/Memory nos legados; buffers. Mantida sem advisory reportado. |
| System.Memory | 4.5.5 | Transitiva JSON nos legados; API de memória. Mantida resolução compatível. |
| System.Numerics.Vectors | 4.4.0 (standard), 4.5.0 (Framework) | Transitiva de Memory; versões selecionadas para cada contrato. Sem pin global artificial. |
| System.Runtime.CompilerServices.Unsafe | 6.0.0 | Transitiva JSON/Memory nos legados. Mantida resolução compatível. |
| System.Text.Encodings.Web | 8.0.0 | Transitiva JSON nos legados; codificação. Mantida sem advisory reportado. |
| System.Threading.Tasks.Extensions | 4.5.4 | Transitiva JSON nos legados; suporte de tarefas. Mantida resolução compatível. |
| System.ValueTuple | 4.5.0 | Transitiva JSON em Framework. Mantida resolução do pacote, sem promover a direta. |
| NETStandard.Library | 2.0.3 | Implícita em netstandard2.0; define o contrato preservado. |
| Microsoft.NETCore.Platforms | 1.1.0 | Transitiva do contrato standard. Mantida, não representa runtime usado em produção. |
| Microsoft.NETFramework.ReferenceAssemblies | 1.0.3 | Implícita pelo SDK para net481 nesta máquina; permite compilar sem targeting pack global correspondente. |
| Microsoft.NETFramework.ReferenceAssemblies.net481 | 1.0.3 | Transitiva do pacote anterior; somente compilação. |
| Microsoft.NET.Test.Sdk | 17.12.0 | Direta, só testes; preserva suporte aos quatro targets e VSTest. Sem advisory reportado; nenhuma necessidade funcional de upgrade nesta tarefa. |
| xunit | 2.9.3 | Direta, só testes; versão estável v2 compatível com Framework, sem migração de runner nesta auditoria. |
| xunit.runner.visualstudio | 2.8.2 | Direta, só testes, PrivateAssets=all; runner compatível e validado. Mantido sem advisory reportado. |
| Microsoft.CodeCoverage | 17.12.0 | Transitiva do Test SDK em todos os testes. Mantida por compatibilidade com SDK selecionado. |
| Microsoft.TestPlatform.ObjectModel | 17.10.0 (Framework), 17.12.0 (modernos) | Transitiva runner/TestHost; resolução por target mantida e executada. |
| Microsoft.TestPlatform.TestHost | 17.12.0 | Transitiva Test SDK nos testes modernos. Mantida alinhada ao SDK. |
| Newtonsoft.Json | 13.0.1 | Transitiva do TestHost moderno, não da biblioteca. Mantida sem advisory reportado; não é usada no mapeamento de provedores. |
| System.Reflection.Metadata | 1.6.0 | Transitiva infraestrutura de testes. Mantida sem advisory reportado e sem pin desnecessário. |
| System.Collections.Immutable | 1.5.0 | Transitiva infraestrutura de testes Framework. Mantida sem advisory reportado. |
| xunit.abstractions | 2.0.3 | Transitiva xUnit, só testes. Mantida como parte do conjunto compatível. |
| xunit.analyzers | 1.18.0 | Transitiva xUnit, análise de testes. Mantida resolução do pacote. |
| xunit.assert | 2.9.3 | Transitiva xUnit, asserções; mantida alinhada à versão principal. |
| xunit.core | 2.9.3 | Transitiva xUnit; mantida alinhada. |
| xunit.extensibility.core | 2.9.3 | Transitiva xUnit; mantida alinhada. |
| xunit.extensibility.execution | 2.9.3 | Transitiva xUnit; mantida alinhada. |

Todos os pacotes mantidos passaram pela mesma auditoria direta/transitiva; não houve supressão de advisories nem versões fixadas para mascarar diagnóstico. Manutenção não significa que cada pacote é o mais recente, mas que a versão resolvida continua necessária/compatível e sem vulnerabilidade conhecida reportada nessa data. Bibliotecas de teste não viram novas dependências de produção.

Remoções do grafo após retirar System.Net.Http 4.3.4: `System.IO 4.3.0`, `System.Runtime 4.3.0`, `System.Security.Cryptography.Algorithms 4.3.0`, `System.Security.Cryptography.Encoding 4.3.0`, `System.Security.Cryptography.Primitives 4.3.0` e `System.Security.Cryptography.X509Certificates 4.3.0`. A remoção se justifica pela referência nativa aos frameworks, não por vulnerabilidade presumida desses pacotes.

## Reprodução e limites da evidência

```powershell
dotnet restore GetCNPJ.sln --force-evaluate --no-http-cache -p:NuGetAudit=true -p:NuGetAuditMode=all
dotnet list GetCNPJ.sln package --include-transitive --format json
dotnet list GetCNPJ.sln package --vulnerable --include-transitive --format json
dotnet build GetCNPJ.sln -c Release
dotnet test GetCNPJ.sln -c Release --no-build
```

`Directory.Build.props` habilita auditoria de diretas e transitivas em restore/CI, sem exclusões de severidade. Segundo a [documentação oficial do NuGet Audit](https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages), a fonte informa vulnerabilidades conhecidas, sujeitas a atualização. O relatório final retornou os dois projetos, fonte NuGet.org e nenhum pacote vulnerável, saída 0. Ausência de aviso não prova ausência de vulnerabilidades futuras ou não catalogadas.

Primeira tentativa de auditoria nesta sessão falhou por TLS no sandbox; repetida com rede autorizada e concluída. Inventário e auditoria final concluídos com saída 0; restore com auditoria total também aprovado. Compilação/testes e limitações de runtime registrados em AUDIT.md e PROGRESS.md.
