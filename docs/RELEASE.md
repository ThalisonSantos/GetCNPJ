# GetCNPJ Community — preparação final da release 1.1.0

Estado em **2026-11-24: preparação local concluída; publicação pendente de autorizações separadas**. Nenhum pacote definitivo foi gerado. Estas alterações documentais ainda precisam de revisão, commit aprovado e CI antes da geração final em árvore limpa. Versão mantida em1.1.0; sem código funcional/dependências/frameworks/CI/licença alterados, consultas cadastrais novas ou funcionalidades1.2.0.

## H03 concluída e evidência manual do SysEmpresa

Mantenedor informou revisão ChatGPT e [GitHub Actions45475015057](https://github.com/ThalisonSantos/GetCNPJ/actions/runs/45475015057), commit `2e676ae4055cff9ab0497f2d644b8c2c305c7f0b`. Também confirmou que **instalou, compilou e testou GetCNPJ.Community1.1.0 no SysEmpresa, funcionando normalmente, com percepção de melhoria em relação à biblioteca original**.

Trata-se de **relato de homologação manual pelo mantenedor**. Não foram informados testes individuais, configuração de ambiente, hash do pacote instalado ou métricas. Não se afirma ganho mensurado de desempenho, aprovação de cada fluxo/controle/campo fiscal, ou nova consulta por provedor. O agente não executou nem modificou o SysEmpresa. Os24 cenários por consumidor e ensaio WinForms detalhados da H03 são evidência independente, não detalhamento presumido desse relato. H03 concluída nesse escopo; a distribuição pública é etapa distinta.

## Conclusão das tarefas nos seus escopos

| Tarefa | Estado e alcance |
|---|---|
| T01 | Concluída: infraestrutura de testes/CI, sem publicação automática; cobertura ampliada nas etapas seguintes |
| T02 | Concluída: validação/normalização/formatação numérica/alfanumérica e DVs; validade local não comprova cadastro |
| T03 | Concluída: aquisição atômica no limiter embutido, segurança concorrente e compatibilidade externa; par legado continua não atômico |
| T04 | Concluída: timeout/fallback/cancelamento, preservando prioridades, falhas e consultas dirigidas sem fallback/retry |
| T05 | Concluída: recursos HTTP, propriedade/reutilização/Dispose e testes simulados |
| T06 | Concluída: dependências/contratos/mapeamentos/fontes; revisão externa foi parcial, revisão técnica local complementada na H01; limites documentais preservados |
| T07 | Concluída como identidade/preparação/pack/inspeção/consumidores; não inclui publicação, candidatos preservados |
| H01 | Concluída: nulabilidade/qualidade e revisão técnica local T06, zero avisos sem supressão; contratos CLR preservados |
| H02 | Concluída: console manual e quatro numéricas dirigidas autorizadas; complemento separado H02.1 |
| H02.1 | Concluída: seis consultas complementares autorizadas, operação alfanumérica pontual nos quatro serviços; transição real após falha não observada |
| H03 | Concluída: candidato do commit aprovado/consumidores/guia, revisão/CI informados e confirmação manual geral do mantenedor |

Revisões, execuções remotas e commits de cada etapa permanecem em [PROGRESS.md](../PROGRESS.md), [AUDIT.md](../AUDIT.md), [ROADMAP.md](../ROADMAP.md) e [HOMOLOGATION.md](HOMOLOGATION.md). Não se declara revisão integral onde o escopo foi parcial. [INTEGRATION.md](INTEGRATION.md) mantém o guia e distingue orientações de testes efetivamente relatados.

## Validação final desta preparação

Base develop/HEAD `2e676ae4055cff9ab0497f2d644b8c2c305c7f0b`, árvore limpa antes das alterações. Ambiente: Windows10.0.26200 x64, SDK11.0.100/MSBuild18.12.2, runtime8.0.34/Framework4.8.1 e host9.0.23 local. O SDK não adiciona targets10/11.

| Etapa | Resultado / evidência |
|---|---|
| Nome NuGet | [Página oficial](https://www.nuget.org/packages/GetCNPJ.Community) e [flat container](https://api.nuget.org/v3-flatcontainer/getcnpj.community/index.json): HTTP404 em2026-11-24 às17:09:35 America/Sao_Paulo. Nenhum pacote público encontrado; nome não reservado nem aceitação/permissão garantida. artifacts/ReleasePreparation/package-id.json |
| Restore/auditoria direta e transitiva | Aprovados, sem vulnerabilidades conhecidas reportadas para a solução. Restore inicial NU1900 no sandbox não foi considerado válido; repetido com rede autorizada sem avisos. restore-authorized.log, vulnerabilities.json e inventário packages.json na mesma pasta |
| Rebuild Release completo | Cinco targets, **zero avisos e zero erros**; build.log |
| Testes | **450 por net48/net481/net8/net9**,1800 execuções, zero falhas/ignorados; test-*.log e TestResults/ReleasePreparation |
| Ferramenta manual | Build zero avisos/erros e **20 cenários offline** aprovados; console-build.log/console-selftest.log. Sem requisições cadastrais |
| Metadados/licença | Revisados e mantidos conforme tabela abaixo; LICENSE hash779E6F6DB397233CCD7AF0E7926846990E118613FB4BB2DA6A5BA9F18E4044F0 |
| Candidatos | Arquivos T07/H03 reconferidos com os mesmos hashes/tamanhos; nenhum novo nupkg |
| Procedimento/notas | [PUBLICATION.md](PUBLICATION.md) e [RELEASE_NOTES_1.1.0.md](RELEASE_NOTES_1.1.0.md) preparados localmente; sem GitHub draft externo/tag/upload |

Framework4.8 foi executado sobre4.8.1; não houve máquina isolada com apenas4.8. netstandard2.0 compilado, sem runtime próprio. net9 usou DotNetHostPath local; comando global conhecido por abortar na ausência de runtime9 global não foi repetido. Aviso VSTest preexistente de VSTEST_ARTIFACTS_DIRECTORY permanece sem supressão; não é diagnóstico de compilação. Auditoria não cobre runtime/Windows nem falhas ainda não catalogadas.

Estas validações cobrem o código funcional inalterado e o estado local de preparação; **não são CI do futuro commit final nem inspeção/teste de um nupkg que ainda não foi gerado**. Não atribuir os resultados de consumidores H03 a um novo arquivo sem repetir os ensaios nele.

## Metadados e compatibilidade revisados

| Campo | Valor |
|---|---|
| PackageId / Version / Title | GetCNPJ.Community /1.1.0 /GetCNPJ Community |
| Autor original / mantenedor | Bruno da Cunha Felipe /Thalison Santos |
| Authors no nuspec | Bruno da Cunha Felipe,Thalison Santos |
| RepositoryUrl / PackageProjectUrl | https://github.com/ThalisonSantos/GetCNPJ |
| RepositoryType / PackageLicenseExpression | git /MIT |
| Copyright | Copyright (c) 2025 Bruno da Cunha Felipe, conforme LICENSE original íntegra |
| Namespace / assembly | GetCNPJ /GetCNPJ.dll |
| AssemblyVersion / FileVersion | 1.0.0.0 /1.1.0.0 |
| Targets | netstandard2.0, net48, net481, net8.0, net9.0 |
| Conteúdo explícito | Cinco DLLs, README/LICENSE/CHANGELOG/docs de provedores/dependências e metadados OPC/NuGet; sem ferramentas/testes/fontes/logs/config/credenciais/temp/logo/PDB |
| Dependências | STJ8.0.6 somente nos legados; JSON/HTTP do framework nos modernos; System.Net.Http frameworkAssembly no Framework |

Company comercial antiga e PackageIcon não são reaplicados. Logo permanece fora da distribuição por falta de comprovação específica de autorização. NuGet não reserva o nome por consulta404; revalidar na ocasião autorizada. A auditoria não justifica atualizar dependência por mera novidade: nenhum defeito novo comprovado exigiu mudança.

Não instalar GetCNPJ e GetCNPJ.Community simultaneamente. API CLR/namespaces/assembly preservados; a comparação H01 encontrou307 entradas públicas iguais em net48. Referências opcionais e retornos Task<CnpjData?> foram anotados corretamente; consumidores/overrides podem precisar de ajustes de nullable ao recompilar. Listas continuam inicializadas. Flags nullable distinguem desconhecido de negativo; bools e ToString legados preservam suas projeções. Propriedades aditivas podem aparecer na serialização do consumidor. Respostas parciais não certificam completude/fiscalidade. Detalhes atuais nos modelos, README e PROVIDERS.md; matriz/achados históricos H01 em RELEASE_HISTORY.md.

## Candidatos anteriores — evidências preservadas

| Candidato | Caminho | Tamanho | SHA-256 |
|---|---|---|---|
| T07 histórico, não contém H01 | artifacts/packages/GetCNPJ.Community.1.1.0.nupkg | 229798 bytes | `04F2FB78207597463E4BFC71B38AEB57AA4E648B0DF56E7B5407E2F475F44EB6` |
| H03 para homologação local | artifacts/H03/packages/GetCNPJ.Community.1.1.0.nupkg | 234174 bytes | `3DD07A96B6B29411B554496C79E1064B45AEEEBDB668AA324A9699B35F5A34FE` |

T07 registrou RepositoryCommit8652242894604cf28c96121ea2bee67b51fa0a7a e InformationalVersion1.1.0+esseSHA, base que não continha as alterações T07 locais. H03 foi exportado do commit aprovado a069e0decc0163f78f9788d204189e4b0d8055e0 antes de editar os documentos; nuspec e cinco DLLs confirmaram esse commit/InformationalVersion1.1.0+a069e0decc0163f78f9788d204189e4b0d8055e0. Cada ZIP tinha14 entradas, documentos/DLLs comparados byte a byte com sua fonte/build e metadados inspecionados. H03 validou24 cenários por consumidor net48/WinForms/net8, cache isolado/hash/origem e redirect Unsafe6.0.0.0, com HTTP falso. Não foram copiados ou alterados esses candidatos para formar um definitivo.

A confirmação geral do mantenedor no SysEmpresa não informa hash instalado; não é prova adicional de correspondência de bytes desses arquivos. [RELEASE_HISTORY.md](RELEASE_HISTORY.md) preserva integralmente os registros anteriores, naturezas dos1275 avisos resolvidos e evidências de pack/inspeção/consumidores. Seus comandos/estados antigos são históricos e não devem ser usados para gerar/publicar1.1.0 agora.

## Próxima execução — somente após autorização

Seguir o [checklist definitivo](PUBLICATION.md): revisão/aprovação destas alterações → commit/push autorizados → merge autorizado → CI do SHA resultante em main → checkout limpo do commit congelado → restore/auditoria/rebuild/testes/offline → geração do nupkg definitivo → inspeção/proveniência/hash → consumidores independentes do mesmo arquivo → aprovação/tagv1.1.0 no mesmo SHA → NuGet/indexação/verificação pública → GitHub Release com notas em português e o mesmo arquivo/checksum.

**Não gerar pacote definitivo da árvore local com alterações não commitadas.** Não usar hash de candidato ou CI da H03 como identificação/validação do artefato futuro. Mudança após congelamento exige novo commit aprovado/CI e repetição das etapas; não reescrever tag ou substituir nupkg publicado.

Os auxiliares H03 em tools/GetCNPJ.PackageValidation têm SHA/caminhos específicos de a069e0d e H03. PUBLICATION.md explica a preparação de cópias isoladas para o commit/fonte/cache do pacote futuro, mantendo checagens de identidade e sem editar a fonte congelada. Esse ajuste futuro não foi executado nem anunciado como validação do definitivo. A licença/conteúdo do pacote serão reconferidos no arquivo real.

O workflow permanece sem publicação automática. As notas são arquivo local, não GitHub Release criada. Identificação/tamanho/SHA-256 do definitivo e CI do futuro commit permanecem pendentes. Nenhuma publicação, commit, push, merge, tag, GitHub Release ou funcionalidade1.2.0 foi realizada nesta preparação.
