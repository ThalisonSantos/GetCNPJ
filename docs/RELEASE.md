# GetCNPJ Community 1.1.0 — publicada e concluída

Estado em **2026-11-25: publicação e validação pós-publicação concluídas**. Disponível no [NuGet.org](https://www.nuget.org/packages/GetCNPJ.Community/1.1.0) e na [GitHub Release estável](https://github.com/ThalisonSantos/GetCNPJ/releases/tag/v1.1.0). Tag oficial **v1.1.0**, commit `abaf152d756aa8283a43bf178cdc31dc7c724fd4`. Publicação confirmada pelo mantenedor; API GitHub reconferida no encerramento: `draft=false`, `prerelease=false`, tag anotada resolvida para esse commit. Release publicada em 2026-11-25 às 12:29:34 UTC (09:29:34 America/Sao_Paulo), conforme API. CI final [45481606695](https://github.com/ThalisonSantos/GetCNPJ/actions/runs/45481606695) aprovado conforme mantenedor.

Este encerramento atualiza somente documentação em develop sincronizada com main, preservando histórico, fontes, versão, dependências, licença, tags e bytes publicados. **Não gerar outro pacote 1.1.0 nem mover/recriar a tag.** Implementação de1.2.0 não iniciada. Registros históricos de preparação abaixo não representam pendências atuais.

## Artefatos oficiais e integridade

Arquivo: `GetCNPJ.Community.1.1.0.nupkg`. O arquivo enviado e o distribuído pelo NuGet.org são distintos pela assinatura de repositório; não usar seus hashes como se fossem intercambiáveis.

| Artefato | Tamanho | SHA-256 |
|---|---|---|
| Definitivo original enviado | 227903 bytes | `210FDD0AADEA40680D03625F68E6D83E719D930DC3D55AAAAA914BFA2D1A22E0` |
| Público assinado pelo NuGet.org | 240996 bytes | `E81F066C26CA85877CE4B93EA1D439F0F9F83E0AD3F2870D5B55E8914337A883` |

RepositoryCommit: `abaf152d756aa8283a43bf178cdc31dc7c724fd4`. As cinco DLLs têm InformationalVersion `1.1.0+abaf152d756aa8283a43bf178cdc31dc7c724fd4`, AssemblyName GetCNPJ, AssemblyVersion1.0.0.0 e FileVersion1.1.0.0.

As **14 entradas originais** são idênticas byte a byte, incluindo os metadados OPC/nuspec, documentos, LICENSE e DLLs. O público tem15 entradas, com apenas `.signature.p7s` adicional. Nenhuma divergência de conteúdo original, dependências ou metadados identificada. Arquivos publicados não foram alterados; a documentação neles contida preserva o snapshot aprovado anterior ao upload, inclusive estados históricos de preparação.

## Geração definitiva e validação pós-publicação

| Etapa | Evidência / resultado |
|---|---|
| Fonte definitiva | Checkout dedicado limpo do commit aprovado abaf152; pack executado uma única vez, sem misturar alterações locais ou substituir candidatos |
| Auditoria antes da publicação | NuGet direto/transitivo sem vulnerabilidades conhecidas reportadas, solução e consumidores; não garante ausência de vulnerabilidades desconhecidas/futuras ou de runtime/Windows |
| Rebuild/testes anteriores à publicação | Cinco targets, zero avisos/erros;450 testes por framework executável (1800 aprovações),20 cenários offline e24 cenários por consumidor independente aprovados |
| Restore público pós-publicação | Exclusivamente https://api.nuget.org/v3/index.json, cache novo isolado, fonte offline implícita do SDK excluída; project.assets.json e .nupkg.metadata conferidos, sem fontes locais |
| Consumidores pós-publicação | **48 cenários aprovados**,24 net48/WinForms e24 net8; rebuilds zero avisos/erros, auditoria habilitada e HTTP somente simulado; DLLs consumidas comparadas com as publicadas |
| Assinatura | `dotnet nuget verify --all` aprovado com trust store padrão, tipo Repository, service index NuGet.org e owner ThalisonSantos, sem bypass de TLS/certificados/assinaturas |
| Comparação e proveniência |14 entradas originais idênticas, somente assinatura adicional; cinco DLLs e nuspec inspecionados, RepositoryCommit/InformationalVersion corretos |
| GitHub | Release publicada/estável, não draft/prerelease; tag anotada v1.1.0 confirmada no SHA oficial por leitura da API |

Os24 cenários por consumidor cobrem numérico/alfanumérico, quatro adaptadores sync/async, endereço/IBGE/IE/CNAEs/Simples e campos nullable, cancelamento, timeout/fallback, reutilização e descarte. WinForms exercitou STA/message loop/await/atualização de controle sem janela visível. Redirect Unsafe6.0.0.0 gerado no net48, HTTP da plataforma sem DLL local. Nenhum conflito nesse grafo isolado; não certifica todo grafo de aplicações externas.

Certificado de repositório NuGet.org: SHA-256 `1F4B311D9ACC115C8DC8018B5A49E00FCE6DA8E2855F9F014CA6F34570BC482D`. Content hash NuGet verificado: `15AH1g9XSmTK5+d03ykjgGwQqxaekPq1EjFKhGGzx8ul7fqZzuO+1eyi2z3AHVu7TjqS6lpoqy22OUD4HA1tkw==`; não confundir com o SHA-256 do ZIP completo.

Evidências locais preservadas (ignoradas pelo Git):

- `artifacts/Final-1.1.0-abaf152d756aa8283a43bf178cdc31dc7c724fd4`: pacote original, manifest.json, VALIDATION.md, logs e TRX da geração definitiva.
- `artifacts/PostPublication-1.1.0-abaf152d756aa8283a43bf178cdc31dc7c724fd4`: pacote público em cache-public-only, manifest.json, content-comparison.json, consumer-provenance.json, signature-verification-public-only.log e logs finais dos48 cenários.

Esses ensaios ocorreram antes deste encerramento documental e não são anunciados como novas execuções nesta tarefa. Falhas TLS iniciais e fonte SDK implícita foram registradas e não contadas como restore público exclusivo aprovado; reexecução autorizada com cache novo/fonte única passou. Nenhuma consulta cadastral real foi executada na geração definitiva ou no pós-publicação. Os dados essenciais dos relatórios locais estão consolidados acima para consulta sem os diretórios ignorados.

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

## Histórico — validação da preparação de 2026-11-24, anterior à geração definitiva

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

Naquela etapa, essas validações cobriam o código funcional inalterado e a preparação local; ainda não eram CI do commit final ou inspeção/teste do definitivo. As etapas posteriores foram executadas e estão registradas acima. Os resultados históricos de consumidores H03 não foram usados como substituto dos ensaios do arquivo definitivo ou público.

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

Company comercial antiga e PackageIcon não são reaplicados. Logo permanece fora da distribuição por falta de comprovação específica de autorização. A consulta404 anterior é evidência histórica, superada pela publicação confirmada acima. A auditoria não justifica atualizar dependência por mera novidade: nenhum defeito novo comprovado exigiu mudança.

Não instalar GetCNPJ e GetCNPJ.Community simultaneamente. API CLR/namespaces/assembly preservados; a comparação H01 encontrou307 entradas públicas iguais em net48. Referências opcionais e retornos Task<CnpjData?> foram anotados corretamente; consumidores/overrides podem precisar de ajustes de nullable ao recompilar. Listas continuam inicializadas. Flags nullable distinguem desconhecido de negativo; bools e ToString legados preservam suas projeções. Propriedades aditivas podem aparecer na serialização do consumidor. Respostas parciais não certificam completude/fiscalidade. Detalhes atuais nos modelos, README e PROVIDERS.md; matriz/achados históricos H01 em RELEASE_HISTORY.md.

## Candidatos anteriores — evidências preservadas

| Candidato | Caminho | Tamanho | SHA-256 |
|---|---|---|---|
| T07 histórico, não contém H01 | artifacts/packages/GetCNPJ.Community.1.1.0.nupkg | 229798 bytes | `04F2FB78207597463E4BFC71B38AEB57AA4E648B0DF56E7B5407E2F475F44EB6` |
| H03 para homologação local | artifacts/H03/packages/GetCNPJ.Community.1.1.0.nupkg | 234174 bytes | `3DD07A96B6B29411B554496C79E1064B45AEEEBDB668AA324A9699B35F5A34FE` |

T07 registrou RepositoryCommit8652242894604cf28c96121ea2bee67b51fa0a7a e InformationalVersion1.1.0+esseSHA, base que não continha as alterações T07 locais. H03 foi exportado do commit aprovado a069e0decc0163f78f9788d204189e4b0d8055e0 antes de editar os documentos; nuspec e cinco DLLs confirmaram esse commit/InformationalVersion1.1.0+a069e0decc0163f78f9788d204189e4b0d8055e0. Cada ZIP tinha14 entradas, documentos/DLLs comparados byte a byte com sua fonte/build e metadados inspecionados. H03 validou24 cenários por consumidor net48/WinForms/net8, cache isolado/hash/origem e redirect Unsafe6.0.0.0, com HTTP falso. Não foram copiados ou alterados esses candidatos para formar um definitivo.

A confirmação geral do mantenedor no SysEmpresa não informa hash instalado; não é prova adicional de correspondência de bytes desses arquivos. [RELEASE_HISTORY.md](RELEASE_HISTORY.md) preserva integralmente os registros anteriores, naturezas dos1275 avisos resolvidos e evidências de pack/inspeção/consumidores. Seus comandos/estados antigos são históricos e não devem ser usados para gerar/publicar1.1.0 agora.

## Encerramento e manutenção documental

**T01–T07, H01, H02, H02.1 e H03 concluídas nos respectivos escopos; versão1.1.0 publicada e encerrada.** Revisões externas parciais permanecem assim classificadas. H02.1 confirma operação pontual dos quatro provedores, sem garantia de cobertura fiscal, SLA ou todos os cadastros; transição real após falha não observada, coberta com HTTP simulado. Framework4.8 foi executado sobre4.8.1, netstandard compilado. O relato manual do SysEmpresa não contém métricas ou detalhes individuais; não houve nova execução presumida.

Validação documental de2026-11-25: restore auditado aprovado (falha de acesso NU1900 no sandbox seguida de reexecução autorizada), rebuild Release cinco targets com zero avisos/erros e450 testes por net48/net481/net8/net9 (1800 aprovações, zero falhas/ignorados), host net9 local. Evidências em artifacts/DocumentationClosure-1.1.0. Mensagem VSTest ambiental já conhecida mantida; links locais, estrutura dos seis badges, JSON do endpoint, exemplos C# preservados e diff conferidos. Os20 cenários offline e48 cenários de consumidores não foram repetidos nesta etapa; permanecem evidências anteriores. O novo endpoint do badge alfanumérico exige envio autorizado da documentação a develop para ser servido publicamente.

[Notas definitivas](RELEASE_NOTES_1.1.0.md) sincronizadas com a release publicada. [PUBLICATION.md](PUBLICATION.md) preserva o roteiro histórico anterior e não deve ser reexecutado para1.1.0. Os auxiliares H03 rastreados continuam específicos daquele candidato; as adaptações de validação final foram feitas somente em cópias isoladas, preservando checagens e fontes aprovados.

O workflow continua somente restore/build/test, sem publicação automática. A documentação atual de develop é posterior ao commit/tag/bytes publicados, não altera o snapshot do pacote e não autoriza sua regeneração. Alterações documentais locais dependem de revisão e de autorizações futuras para commit/push/merge; nenhuma dessas operações foi executada no encerramento. Candidatos/hashes históricos, original e público preservados;1.2.0 não iniciada.
