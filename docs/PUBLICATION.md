# Checklist definitivo e sequência de publicação — 1.1.0

Preparado em **2026-11-24**. Este é um roteiro para execução futura, **não uma autorização**. Nesta preparação não houve commit, push, merge, tag, geração do pacote definitivo, GitHub Release ou upload NuGet. O workflow continua apenas restore/build/test. Cada operação externa depende da autorização separada do mantenedor.

## Evidências disponíveis e limites

- [x] T01–T07, H01, H02, H02.1 e H03 concluídas nos escopos de [ROADMAP.md](../ROADMAP.md), com evidências em AUDIT.md/PROGRESS.md.
- [x] Revisão ChatGPT e CI H03 informados pelo mantenedor: execução45475015057, commit2e676ae4055cff9ab0497f2d644b8c2c305c7f0b.
- [x] Homologação manual relatada: mantenedor instalou, compilou e testou1.1.0 no SysEmpresa, funcionando normalmente, com percepção de melhoria ante a original. Não foram informados casos individuais, métricas, configuração ou hash instalado; não presumir esses dados.
- [x] Metadados revisados: PackageId GetCNPJ.Community, Version1.1.0, Title GetCNPJ Community, namespace/assembly GetCNPJ, AssemblyVersion1.0.0.0, FileVersion1.1.0.0, autores Bruno da Cunha Felipe/Thalison Santos, URLs do fork, RepositoryType git e MIT. Logo e referência comercial antiga fora dos metadados/distribuição; créditos/copyright originais intactos.
- [x] Verificação oficial NuGet em2026-11-24 às17:09:35 America/Sao_Paulo: página do ID e flat container retornaram404. Nenhum pacote público encontrado; **não reserva o nome**, não comprova permissões da conta/prefixo ou aceitação futura. Evidência artifacts/ReleasePreparation/package-id.json.
- [x] Auditoria final direta/transitiva sem vulnerabilidades conhecidas reportadas; rebuild cinco targets zero avisos/erros,450 testes por runtime executável (1800) e20 cenários offline aprovados. Resultados/ambiente em RELEASE.md. Não equivale à validação do futuro commit final ou de um pacote ainda não gerado.
- [x] Candidatos T07/H03 e hashes preservados; nenhum deles foi declarado pacote definitivo.
- [x] [Notas da primeira GitHub Release](RELEASE_NOTES_1.1.0.md) preparadas em português, ainda rascunho local.

## Ordem recomendada — somente com as autorizações correspondentes

1. **Revisar e aprovar esta preparação.** O mantenedor decide o commit e autoriza commit/push das alterações. Conferir README/CHANGELOG/metadados, licença e notas. Não marcar publicação como ocorrida antes de acontecer.
2. **Validar develop e autorizar o merge para main.** Obter CI aprovado do commit enviado e revisão do PR. Com autorização específica, executar merge. Não há merge/push nesta tarefa.
3. **Validar o CI do commit resultante em main.** Usar o SHA real após merge/squash/rebase; não reutilizar SHA de develop ou os commits dos candidatos. Conferir testes descobertos450 por target e cinco builds, não apenas o indicador verde. Registrar execução CI do SHA exato.
4. **Congelar o commit final aprovado.** Abrir checkout separado desse SHA, com árvore limpa (incluindo não rastreados). Preservar os arquivos da revisão e a versão1.1.0. Nenhuma edição de docs/versão/ferramentas dentro do checkout entre build e pack. Logs/saídas só em diretório ignorado novo, separado dos candidatos históricos.
5. **Gerar uma única vez o pacote definitivo desse commit**, após autorização para geração: restore/auditoria, rebuild completo,450 testes por runtime,20 cenários offline e pack na sequência abaixo. Falha/aviso de auditoria ou compilação impede prosseguir. Não gerar a partir das alterações locais desta preparação.
6. **Inspecionar integralmente o arquivo gerado.** Confirmar conteúdo/nuspec/bytes/proveniência conforme checklist abaixo; registrar tamanho e SHA-256 reais. Não copiar hash de candidato, substituir bytes ou remontar ZIP após conferir.
7. **Testar o mesmo arquivo em consumidores independentes.** Fonte local exclusiva/cache novo, net48/WinForms e net8, com HTTP sintético. Conferir hash em cache/origem, DLL carregada, cancelamento/timeout/descarte/nullable/mapeamentos/redirects. Tratar a limitação das ferramentas H03 descrita abaixo. Qualquer ajuste na biblioteca/documentos empacotados exige novo commit aprovado/CI/build/pack/inspeção/consumidores; não alterar versão ou corrigir arquivo publicado por conta própria.
8. **Aprovar o artefato exato e autorizar a tag.** Criar e enviar `v1.1.0` somente se não existir e se apontar ao SHA congelado usado no nuspec/InformationalVersion/CI. Não mover, sobrescrever ou reaproveitar tag existente. Registrar commit/hash/tamanho/resultados em evidência externa ou artefato de validação, sem precisar modificar o commit já congelado.
9. **Autorizar e publicar no NuGet.org.** Revalidar ID/versão/permissões, conferir novamente SHA-256 do arquivo aprovado e realizar upload desse único arquivo. Não usar wildcard, `--skip-duplicate` ou publicar candidato antigo. Aguardar indexação, verificar metadados/versão públicos e instalar o pacote publicado em cache novo, mantendo ensaios de consulta somente sintéticos. Duplicidade/falha não é sucesso nem autorização para trocar PackageId/versão.
10. **Autorizar e publicar a primeira GitHub Release.** Selecionar a tag existente correta, título GetCNPJ Community1.1.0, revisar notas em português e anexar o mesmo nupkg aprovado/publicado e checksum/manifesto, sem logs sensíveis. Não deixar o GitHub criar tag em HEAD diferente. Conferir links NuGet/assets e a consistência das evidências. Se a publicação NuGet falhar, não anunciar conclusão da distribuição.

Uma eventual atualização documental após publicação é outra alteração rastreável; não altera o nupkg já publicado, sua tag ou o hash aprovado. Não regenerar silenciosamente1.1.0 para substituir bytes.

## Geração futura com guardas de commit e árvore limpa

**Trecho de referência não executado nesta preparação.** Executar em checkout dedicado, do SHA aprovado após CI. Substituir a marcação abaixo por um SHA completo real; o trecho aborta com a marcação ou árvore suja. O diretório de saída novo fica em artifacts para não contaminar o Git.

```powershell
$ErrorActionPreference = 'Stop'
$releaseCommit = '<SHA_FINAL_APROVADO_EM_MAIN>'
if ($releaseCommit -notmatch '^[0-9a-f]{40}$') { throw 'Informar SHA aprovado completo' }
if ((git rev-parse HEAD) -ne $releaseCommit) { throw 'HEAD não corresponde ao commit aprovado' }
if (git status --porcelain) { throw 'Árvore de trabalho não está limpa' }
$releaseRoot = Join-Path $PWD ('artifacts/Final-1.1.0-' + $releaseCommit)
if (Test-Path -LiteralPath $releaseRoot) { throw 'Saída já existe; não sobrescrever artefato' }
New-Item -ItemType Directory -Path $releaseRoot | Out-Null

# SDK/runtimes8/9 e referência Framework preparados previamente.
# Inspecionar relatórios: NU1900/NU1301, advisories ou warnings impedem o pack.
dotnet restore GetCNPJ.sln --force-evaluate --no-http-cache -p:NuGetAudit=true -p:NuGetAuditMode=all -p:TreatWarningsAsErrors=true
if ($LASTEXITCODE -ne 0) { throw 'Restore falhou' }
$auditText = dotnet list GetCNPJ.sln package --vulnerable --include-transitive --format json
if ($LASTEXITCODE -ne 0) { throw 'Auditoria falhou' }
$auditText | Set-Content -Encoding UTF8 (Join-Path $releaseRoot 'vulnerabilities.json')
$audit = ($auditText -join "`n") | ConvertFrom-Json
if ($audit.errors -or @($audit.projects).Count -ne 2) { throw 'Relatório de auditoria incompleto' }
foreach ($project in $audit.projects) {
    foreach ($framework in $project.frameworks) {
        foreach ($dependency in (@($framework.topLevelPackages) + @($framework.transitivePackages))) {
            if ($dependency.vulnerabilities) { throw 'Vulnerabilidade reportada; não prosseguir' }
        }
    }
}
dotnet build GetCNPJ.sln -c Release --no-restore -t:Rebuild -p:TreatWarningsAsErrors=true "-p:SourceRevisionId=$releaseCommit" "-p:RepositoryCommit=$releaseCommit"
if ($LASTEXITCODE -ne 0) { throw 'Rebuild falhou' }
foreach ($target in @('net48','net481','net8.0','net9.0')) {
    dotnet test GetCNPJ.sln -c Release -f $target --no-build --settings tests/test.runsettings --logger trx --results-directory (Join-Path $releaseRoot 'TestResults')
    if ($LASTEXITCODE -ne 0) { throw "Testes falharam: $target" }
}
dotnet restore tools/GetCNPJ.Homologation/GetCNPJ.Homologation.csproj -p:NuGetAudit=true -p:NuGetAuditMode=all -p:TreatWarningsAsErrors=true
if ($LASTEXITCODE -ne 0) { throw 'Restore console falhou' }
dotnet build tools/GetCNPJ.Homologation/GetCNPJ.Homologation.csproj -c Release --no-restore -p:TreatWarningsAsErrors=true "-p:SourceRevisionId=$releaseCommit" "-p:RepositoryCommit=$releaseCommit"
if ($LASTEXITCODE -ne 0) { throw 'Build console falhou' }
dotnet tools/GetCNPJ.Homologation/bin/Release/net8.0/GetCNPJ.Homologation.dll --self-test
if ($LASTEXITCODE -ne 0) { throw 'Cenários offline falharam' }

# Conferir também450 aprovações em cada TRX,20 cenários, ausência de warnings/advisories.
# Código0 de restore ou de listagem não substitui essa conferência.
if ((git rev-parse HEAD) -ne $releaseCommit -or (git status --porcelain)) { throw 'Fonte mudou durante validação' }
$packageDirectory = Join-Path $releaseRoot 'packages'
dotnet pack GetCNPJ.csproj -c Release --no-build --no-restore -p:TreatWarningsAsErrors=true "-p:RepositoryCommit=$releaseCommit" "-p:SourceRevisionId=$releaseCommit" --output $packageDirectory
if ($LASTEXITCODE -ne 0) { throw 'Pack falhou' }
$packagePath = Join-Path $packageDirectory 'GetCNPJ.Community.1.1.0.nupkg'
Get-Item -LiteralPath $packagePath | Select-Object FullName,Length
Get-FileHash -LiteralPath $packagePath -Algorithm SHA256
if ((git rev-parse HEAD) -ne $releaseCommit -or (git status --porcelain)) { throw 'Fonte final inconsistente' }
```

Esse trecho pressupõe runtime9 disponível globalmente e restaura o console explicitamente porque a solução não inclui essa ferramenta. No ambiente local atual, net9 usa `-- "RunConfiguration.DotNetHostPath=D:/Meus Projetos/GetCNPJ/artifacts/dotnet9/dotnet.exe"`; adaptar somente o comando daquele target conforme tests/README.md. Não aplicar roll-forward para simular teste em outro runtime.

Preservar relatórios e logs completos em saída ignorada, com código de saída e contagem, sem segredos. Se houver warnings/advisories ou revisão das fontes durante a validação, parar antes do pack e tratar a causa; não desabilitar auditoria/Nullable ou usar NoWarn. Build/pack devem carregar exatamente o mesmo SourceRevisionId, sem misturar DLLs de outra pasta ou revisão.

## Inspeção e consumidores do pacote definitivo

- [ ] PackageId GetCNPJ.Community, Version1.1.0, Title/autores/copyright/MIT/URLs/tags/notas corretos e sem identidade comercial/logo não autorizado.
- [ ] RepositoryCommit igual ao SHA final e InformationalVersion `1.1.0+<SHA_FINAL>` em **todas as cinco DLLs**, não apenas em uma. AssemblyName GetCNPJ, AssemblyVersion1.0.0.0, FileVersion1.1.0.0.
- [ ] ZIP com cinco assets netstandard2.0/net48/net481/net8.0/net9.0; cinco documentos README/LICENSE/CHANGELOG/docs/PROVIDERS.md/docs/DEPENDENCIES.md; metadados OPC/NuGet esperados. Investigar qualquer entrada extra; sem testes/ferramentas/fontes/logs/config/credenciais/temporários/logo/PDB.
- [ ] Comparar documentos byte a byte com o commit final, licença original íntegra e DLLs com o rebuild. Conferir nuspec integral e todos os grupos de dependências.
- [ ] STJ>=8.0.6 apenas nos legados; grupos modernos sem pacote JSON/HTTP redundante; HTTP frameworkAssembly nos targets Framework. Revalidar grafo direto/transitivo efetivamente resolvido.
- [ ] Registrar tamanho/hash SHA-256 do arquivo definitivo em manifesto; preservar T07/H03. Não atribuir o tamanho/hash existente ao arquivo novo.
- [ ] Consumidores só por PackageReference; sem ProjectReference/fontes da biblioteca, source mapping exato da fonte local e cache vazio novo. Conferir .nupkg.metadata, hash no cache e assets net48/net8 selecionados.
- [ ] Conferir instalação/compilação/execução net48/WinForms/net8, binding redirects e ausência de conflitos no grafo isolado; DV/URL numérico/alfanumérico, sync/async, campos opcionais/endereço/IBGE/IE/CNAEs/Simples, cancelamento/timeout/fallback, descarte/reutilização. HTTP exclusivamente simulado.

**Atenção às ferramentas H03 existentes:** `Inspect-Package.ps1`, `ConsumerChecks.Commit` e NuGet.Config foram feitos para o candidato a069e0d/fonte/cache H03, não são validadores genéricos do pacote definitivo. Não executar sem adaptar e depois interpretar a falha de proveniência como defeito do pacote. Não simplesmente remover as asserções de identidade.

Para a validação futura, exportar somente os auxiliares do commit final para uma pasta externa ao checkout ou sob a saída ignorada, por exemplo `git archive <SHA_FINAL> tools/GetCNPJ.PackageValidation`, e extrair em diretório novo. Nas **cópias**, configurar: SHA esperado completo em ConsumerChecks.Commit e no script de inspeção; PackagePath/ApprovedSource para o arquivo definitivo e fonte congelada; outputRoot da inspeção para a nova pasta (não H03/inspection); fonte local e globalPackagesFolder para os novos diretórios absolutos. Manter as checagens de cinco DLLs/24 cenários e hash/origem; revisar o ajuste antes de executar. A classe de leitura PE usa ConsumerChecks.Commit. Não editar o checkout congelado nem reempacotar para essa adaptação dos consumidores.

As modificações das cópias são de ferramentas de teste; não alteram os fontes da biblioteca ou os bytes do nupkg. Registrar configurações e resultados. O relato manual do SysEmpresa permanece evidência da H03; não inventar uma nova execução daquele sistema para o pacote definitivo. Se houver necessidade de nova validação no SysEmpresa, solicitar a execução ao mantenedor e registrar somente o que ele confirmar.

## Tag, NuGet e GitHub Release — pendências externas

- [ ] Aprovação do commit final e CI do SHA resultante em main.
- [ ] Aprovação da geração/inspeção/consumidores e do **arquivo/hash exatos**.
- [ ] Autorização específica de criação/envio de tag v1.1.0 no mesmo SHA; confirmar tag inexistente e não sobrescrever.
- [ ] Revalidar ID/versão/conta NuGet e autorizar upload; se já existir, verificar titularidade/conteúdo sem trocar identidade/versão automaticamente.
- [ ] Publicar o único nupkg aprovado e aguardar indexação; conferir metadados e instalação pública com cache novo, usando HTTP falso nos ensaios.
- [ ] Autorizar GitHub Release, selecionar a tag já conferida e revisar notas sem marcadores pendentes; anexar o mesmo arquivo/hash/manifesto e conferir links.

Na publicação manual futura, NuGet7.6+/SDK10.0.300+ permite fornecer `NUGET_API_KEY` ao processo por meio seguro, sem expor chave no comando/log. O comando a executar **somente após autorização para o arquivo exato** é `dotnet nuget push <CAMINHO_EXATO_DO_NUPKG_APROVADO> --source https://api.nuget.org/v3/index.json`. A marcação é deliberadamente incompleta; não foi executada. Não gravar chave no repositório, desabilitar TLS/assinaturas ou adicionar segredo/etapa de publicação ao workflow como parte desta preparação.

Fontes oficiais: [publicação NuGet](https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package), [gerenciamento de GitHub Releases](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository). A GitHub Release deve usar a tag exata, evitando criação automática em outra revisão; se optar por draft futuro, sua criação também exige autorização. Nenhuma dessas operações foi realizada aqui.
