# Progresso do desenvolvimento

## Estado inicial — 2026-11-24
- [x] Fork criado e clonado pelo mantenedor.
- [x] Compilação original bem-sucedida, segundo o mantenedor.
- [x] Branch develop criada e confirmada no GitHub.
- [x] Auditoria estática inicial registrada em AUDIT.md.
- [x] Documentação inicial preparada para inclusão local.
- [x] Documentação adicionada e commitada pelo mantenedor em develop (bbc8a79).
- [x] T01 — Infraestrutura de testes e CI sem publicação (validada local e remotamente).
- [x] T02 — CNPJ alfanumérico (revisada e validada remotamente; suporte externo não confirmado).
- [x] T03 — Rate limiter (concluída localmente).
- [ ] T04 — Timeout e fallback.
- [ ] T05 — Recursos HTTP.
- [ ] T06 — Dependências e provedores.
- [ ] T07 — Identidade e release.

## Handoff inicial
- Agente: ChatGPT.
- Base analisada: ea643091f02534494b6b63887d923a474d904ad8, branch develop.
- Alterações: cinco documentos preparados; nenhum código C# alterado.
- Validação: nenhuma compilação ou teste executado pelo ChatGPT. A compilação original foi informada pelo mantenedor.
- Bloqueio: integração GitHub retornou HTTP 403 em tentativas de escrita. Documentos entregues em ZIP para inclusão local.
- Próximo passo: adicionar os documentos a develop e iniciar T01. Inspecionar workflow existente e inclusão de fontes no csproj da raiz antes de criar testes.
- Não publicar NuGet nem fazer merge em main nesta etapa.

## 2026-11-24 — Codex — T01
- Branch / commit de referência: develop / bbc8a79; árvore limpa no início.
- Estado: concluído; validação remota posterior informada pelo mantenedor na execução 45415710723 (ver confirmação abaixo).
- Objetivo e critérios de aceite: integrar testes determinísticos à solução, excluir suas fontes do projeto principal, executar build/test preservando os cinco targets e substituir publicação automática por CI de build/test.
- Escopo: infraestrutura e testes iniciais do comportamento existente com HTTP simulado; sem iniciar T02 nem alterar contratos públicos.
- Arquivos alterados: GetCNPJ.csproj, GetCNPJ.sln, .github/workflows/main.yml, NuGet.Config, tests/GetCNPJ.Tests/GetCNPJ.Tests.csproj, tests/GetCNPJ.Tests/CnpjClientTests.cs, tests/GetCNPJ.Tests/StubHttpMessageHandler.cs, tests/test.runsettings, tests/README.md, AUDIT.md, ROADMAP.md, PROGRESS.md e CHANGELOG.md.
- Decisões: xUnit v2/VSTest para os quatro targets executáveis; netstandard2.0 validado por compilação. Somente HTTP simulado e dados sintéticos. CI Windows com SDKs/runtimes 8 e 9, seleção explícita do SDK 9 e falha quando nenhum teste é encontrado. Sem pack/push/segredos de publicação. Geração automática de pacote no build desabilitada; API e dependências de produção intactas.
- Ambiente: Windows 10.0.26200 x64; SDK 11.0.100; MSBuild 18.12.2; runtime .NET 8.0.34; .NET Framework Release 533509 (4.8.1). Runtime 9.0.23 instalado apenas em artifacts/dotnet9; uso do SDK 11 local não significa suporte novo a net10/net11.
- Comandos e resultados reais:
  - `dotnet sln GetCNPJ.sln add tests/GetCNPJ.Tests/GetCNPJ.Tests.csproj`: aprovado; configurações extras x64/x86 geradas pelo SDK foram removidas para preservar a estrutura Any CPU existente.
  - `dotnet restore GetCNPJ.sln`: primeira tentativa falhou com NU1301/TLS e fonte privada herdada. `dotnet restore GetCNPJ.sln --source https://api.nuget.org/v3/index.json` com rede autorizada: aprovado. Após inclusão de NuGet.Config, comando obrigatório sem parâmetros extras: aprovado.
  - `dotnet build GetCNPJ.sln -c Release`: aprovado nos cinco targets, 1290 avisos de nulabilidade existentes e 0 erros.
  - `dotnet test GetCNPJ.sln -c Release --no-build`: saída 1 por ausência de runtime 9; três testes aprovados em cada um de net48, net481 e net8.0, execução net9.0 abortada.
  - Instalador oficial dotnet-install.ps1: primeira execução bloqueada pela política de scripts; execução autorizada com liberação apenas no processo instalou runtime 9 localmente. Definir DOTNET_ROOT_X64 não resolveu a seleção de runtime pelo VSTest; usada a opção RunConfiguration.DotNetHostPath.
  - Recompilação após instalar o runtime local: falhou (2106 erros, 6 avisos), pois o MSBuild encontrou DLLs em artifacts/ como CandidateAssemblyFiles para .NET Framework. Exclusão de artifacts/** dos itens padrão corrigiu a causa. `dotnet build GetCNPJ.sln -c Release -clp:ErrorsOnly`: aprovado com 1290 avisos e 0 erros; resumo em artifacts/build-release.log.
  - `dotnet test GetCNPJ.sln -c Release --no-build -f <target> --settings tests/test.runsettings --logger trx --results-directory TestResults`, para net48, net481 e net8.0: 3 aprovados, 0 falhas, 0 ignorados por target.
  - Mesmo comando para net9.0, acrescentando `-- RunConfiguration.DotNetHostPath="D:/Meus Projetos/GetCNPJ/artifacts/dotnet9/dotnet.exe"`: 3 aprovados, 0 falhas, 0 ignorados. Procedimento reutilizável em tests/README.md.
  - Teste da configuração de descoberta com `-f net8.0 --settings tests/test.runsettings --filter FullyQualifiedName~NoSuchTestForDiscoveryCheck`: saída 1 esperada quando não há testes correspondentes; proteção confirmada.
  - `dotnet msbuild GetCNPJ.csproj -p:TargetFramework=net8.0 -getItem:Compile`: nenhuma fonte de tests/ incluída. DLLs de todos os cinco targets presentes; nenhum nupkg em bin/Release. `git diff --check`: aprovado.
- Testes executados: sucesso HTTP e mapeamento básico, HTTP 503 retornado como erro do provedor e provedor inexistente sem HTTP; 12 execuções aprovadas nos quatro targets. Resultados TRX em TestResults/, ignorados pelo Git. Nenhuma consulta real executada.
- Problemas e limitações: comando global de testes requer runtime 9 global ou configuração de host por target nesta máquina. VSTest do SDK 11 emite aviso sobre VSTEST_ARTIFACTS_DIRECTORY ausente com Microsoft.NET.Test.Sdk 17.12.0; não impediu descoberta ou aprovação. Avisos de nulabilidade existentes continuam pendentes, sem supressão. Runtime .NET Framework 4.8.1 substitui 4.8; não houve teste em máquina com apenas 4.8. Workflow GitHub Actions não executado remotamente; validação remota depende de envio autorizado. Cobertura inicial não resolve os demais itens da auditoria.
- Handoff: alterações locais revisadas, sem commit, push, merge em main ou publicação. LICENSE, créditos, namespaces, API pública e cinco targets preservados. T02 não iniciada.
- Próximo passo exato: mantenedor revisar o diff da T01 e autorizar seu envio quando desejar; conferir a primeira execução remota de build/test. T02 continua pendente para outra tarefa.

## 2026-11-24 — Confirmação remota da T01
- Mantenedor confirmou sucesso de todas as etapas do GitHub Actions na execução [45415710723](https://github.com/ThalisonSantos/GetCNPJ/actions/runs/45415710723), commit 28ad05a1fe781658ce4faff5e2d4ccd3b1dc8475.
- T01 concluída; esta confirmação substitui a pendência remota registrada no handoff histórico acima.

## 2026-11-24 — Codex — T02
- Branch / commit de referência: develop / 28ad05a1fe781658ce4faff5e2d4ccd3b1dc8475; árvore limpa no início.
- Estado: concluído; revisão e validação remota posteriores informadas pelo mantenedor (45418466167, ver confirmação abaixo).
- Escopo e aceite: centralizar validação/normalização/formatação em classe independente; preservar assinaturas existentes e CNPJ numérico; validar DV alfanumérico pela especificação oficial; testes unitários e HTTP simulado nos quatro provedores; preservar cinco targets e distinguir suporte local de suporte externo não confirmado.
- Proposta comportamental antes da implementação: entradas com símbolos/letras Unicode, máscara incorreta ou letras nos DVs serão rejeitadas, em vez de remover caracteres arbitrários com Regex. CNPJs numéricos válidos, sem máscara ou com máscara padrão, continuam aceitos. Letras ASCII minúsculas serão convertidas para maiúsculas; espaços apenas nas extremidades serão tolerados como conveniência local. Não haverá alteração de assinatura pública ou validação de existência cadastral.
- Fora de escopo: T03 e demais tarefas, consultas reais, commit, push e publicação.
- Fonte consultada em 2026-11-24: [Manual de cálculo do DV da Receita Federal/Serpro, páginas 3–4](https://www.gov.br/receitafederal/pt-br/centrais-de-conteudo/publicacoes/documentos-tecnicos/cnpj/manual-dv-cnpj.pdf) e [portal oficial](https://www.gov.br/receitafederal/pt-br/acesso-a-informacao/acoes-e-programas/programas-e-atividades/cnpj-alfanumerico). Exemplo oficial 12.ABC.345/01DE-35 conferido; demais vetores sintéticos calculados separadamente por pesos explícitos. Os vetores não indicam cadastro existente.
- Arquivos alterados: Validation/CnpjValidator.cs; Providers/Base/CnpjProviderBase.cs; comentários em Interfaces/ICnpjProvider.cs e Models/CnpjData.cs; tests/GetCNPJ.Tests/CnpjValidatorTests.cs e ProviderCnpjTests.cs; README.md e tests/README.md; AUDIT.md, PROGRESS.md, ROADMAP.md e CHANGELOG.md.
- Decisões: classe pública estática independente de HTTP com ValidateAndNormalize, IsValid e Format. Cálculo ASCII menos 48, pesos cíclicos de 2 a 9 da direita para a esquerda e módulo 11; ambos os DVs somente numéricos. Preservadas as três assinaturas protegidas da base e todas as assinaturas públicas existentes. Format continua sendo apenas apresentação, mantendo valores fora de 14 posições inalterados; ReceitaWS continua preservando o formato da resposta.
- Inspeção dos provedores: quatro URLs recebem a string normalizada diretamente; campos cnpj/taxId são string. BrasilAPI/CNPJA/CNPJWS formatam via base; ReceitaWS preserva response.cnpj. Nenhum adaptador concreto precisou ser alterado. Testes simulados confirmam somente o comportamento local, sem confirmar suporte das APIs externas. A09 e a revisão ampla de provedores permanecem pendentes.
- Ambiente: Windows 10.0.26200 x64, SDK 11.0.100/MSBuild 18.12.2, .NET 8.0.34 e Framework 4.8.1 (Release 533509); runtime 9.0.23 existente em artifacts/dotnet9. Não foram adicionados targets ou dependências.
- Comandos e resultados:
  - `dotnet restore GetCNPJ.sln`: aprovado, sem necessidade de novas dependências.
  - `dotnet build GetCNPJ.sln -c Release`: aprovado em netstandard2.0, net48, net481, net8.0 e net9.0, 0 erros e 1290 avisos de nulabilidade no código existente; log artifacts/t02-build.log.
  - `dotnet test GetCNPJ.sln -c Release --no-build`: saída 1 pelo runtime 9 ausente na instalação global; 117 aprovados por target nos outros três. Log artifacts/t02-test-solution.log. Não foi contabilizado como aprovação global.
  - `dotnet test GetCNPJ.sln -c Release --no-build -f <target> --settings tests/test.runsettings --logger trx --results-directory TestResults/T02`: aprovado para net48, net481 e net8.0.
  - Para net9.0, mesmo comando acrescido de `-- RunConfiguration.DotNetHostPath="D:/Meus Projetos/GetCNPJ/artifacts/dotnet9/dotnet.exe"`: aprovado com runtime correto, sem roll-forward para outra versão.
- Testes: 117 aprovados, 0 falhas e 0 ignorados em cada um dos quatro targets executáveis (468 execuções). Incluem os três testes existentes, 62 de validação/formatação e 52 dos provedores (32 combinações de entrada/resposta e 20 rejeições antes do HTTP). Letras A–Z, máscaras, minúsculas, cultura turca, zeros iniciais, restos 0/1 em ambos os DVs, erros de DV, repetição numérica e caracteres ASCII/Unicode proibidos cobertos. Evidências TRX em TestResults/T02/; nenhum acesso a API real.
- Limitações: netstandard2.0 validado por compilação; os dois targets Framework rodam na instalação 4.8.1, sem uma máquina isolada com somente 4.8. Comando global depende do runtime 9 global, mas todos os testes foram aprovados por target. Persiste aviso VSTEST_ARTIFACTS_DIRECTORY do SDK 11/Test SDK 17.12.0, sem impedir testes. Suporte externo, existência cadastral e execução remota da T02 não confirmados.
- Handoff: diff revisado e `git diff --check` aprovado; LICENSE, autoria, metadados, dependências e frameworks preservados. T01 registrada como concluída local e remotamente; T02 concluída localmente. Nenhum commit, push ou publicação. T03 não iniciada.
- Próximo passo exato: mantenedor revisar o diff da T02 e, quando desejar, autorizar envio e conferir o CI. A tarefa T03 permanece pendente para solicitação separada.

## 2026-11-24 — Confirmação remota da T02
- Mantenedor informou revisão pelo ChatGPT e sucesso do GitHub Actions na execução [45418466167](https://github.com/ThalisonSantos/GetCNPJ/actions/runs/45418466167), commit e4863f10bbd4da2bcddfba59a54364607373b0ea.
- T02 concluída local e remotamente; substitui a pendência remota do handoff histórico. Suporte alfanumérico externo continua não confirmado.

## 2026-11-24 — Codex — T03
- Branch / commit de referência: develop / e4863f10bbd4da2bcddfba59a54364607373b0ea; árvore limpa no início.
- Estado: concluído localmente; sem validação remota desta alteração.
- Escopo e aceite: aquisição e registro atômicos, filas protegidas, limites independentes por provedor, cancelamento, Reset/GetAvailableRequests concorrentes, validação do construtor, tempo monotônico e testes determinísticos. Preservar cinco frameworks, assinaturas e implementações externas de IRateLimiter.
- Decisão de compatibilidade antes da implementação: acrescentar interface opcional IAtomicRateLimiter com AcquireAsync, sem novos membros em IRateLimiter. A base usará aquisição atômica quando disponível, sem RecordRequest adicional; implementações antigas continuarão no caminho WaitIfNeededAsync + RecordRequest. O par legado permanece não atômico por contrato; consumidores diretos do limiter embutido devem preferir AcquireAsync. Não é possível tornar duas chamadas públicas independentes atômicas sem mudar sua semântica.
- Mudança de comportamento planejada: maxRequests <= 0 e timeWindow <= 0 passam a lançar ArgumentOutOfRangeException no construtor, em vez de travar/funcionar sem limites. Reset preservará o estado sincronizado do provedor e acordará esperas. Nenhuma dependência nova, commit, push ou publicação; T04 fora de escopo.
- Arquivos alterados: RateLimiter/SlidingWindowRateLimiter.cs, RateLimiter/IRateLimiterTime.cs, Interfaces/IAtomicRateLimiter.cs, comentários em Interfaces/IRateLimiter.cs, Providers/Base/CnpjProviderBase.cs, CnpjClient.cs, Properties/AssemblyInfo.cs; tests/GetCNPJ.Tests/SlidingWindowRateLimiterTests.cs, RateLimiterIntegrationTests.cs, ManualRateLimiterTime.cs e AsyncTest.cs; README.md, tests/README.md e quatro documentos de acompanhamento.
- Implementação: ConcurrentDictionary com um estado estável por nome de provedor. Cada fila tem um único lock para limpar, checar e registrar; todas as operações de fila usam esse lock, sem await dentro. AcquireAsync reserva a vaga e já registra a tentativa. Reset limpa a fila existente e sinaliza esperas por TaskCompletionSource com continuations assíncronas; concorrentes acordados disputam a capacidade sob o mesmo lock. Timers que perdem para Reset são cancelados. GetAvailableRequests retorna capacidade protegida e expira registros inclusive na fronteira exata.
- Tempo e parâmetros: Stopwatch.Elapsed disponível nos cinco targets substitui DateTime.UtcNow, sem dependência de TimeProvider. IsHighResolution=True nesta máquina. Esperas positivas arredondadas para cima em ms, no mínimo 1 ms e no máximo Int32.MaxValue - 1 ms por trecho, suportando janelas até TimeSpan.MaxValue. Limites/intervalos não positivos rejeitados com ArgumentOutOfRangeException. CnpjClient constrói/valida o limiter antes de criar o HttpClient próprio; uma instância é compartilhada entre seus provedores, com contagem independente por nome.
- Compatibilidade: nenhum membro novo obrigatório em IRateLimiter, nenhum método removido ou assinatura existente alterada; IAtomicRateLimiter é opcional. A base chama AcquireAsync quando disponível e não chama RecordRequest depois; caminho antigo mantém espera e registro uma vez cada. RecordRequest manual não limita requisições e o protocolo separado permanece não atômico. Consumidores concorrentes do limiter embutido devem migrar para AcquireAsync; implementações externas antigas mantêm sua responsabilidade de sincronização. Cancelamento antes da reserva não conta; cancelamento após aquisição concluída não desfaz uma tentativa. Reset reinicia explicitamente a capacidade.
- Reprodução da falha: arquivos originais do commit e4863f1 copiados pelo git show para artifacts/T03Baseline; execução permitiu 100 esperas simultâneas concluídas antes do registro e 100 registros para limite 3. `dotnet run --project artifacts/T03Baseline/T03Baseline.csproj -c Release`: saída 0; evidência em artifacts/t03-baseline-reproduction.log. Reprodução e fontes temporárias ignoradas pelo Git.
- Ambiente: Windows 10.0.26200 x64; SDK 11.0.100 / MSBuild 18.12.2, runtime 8.0.34 e Framework 4.8.1 (Release 533509), runtime 9.0.23 local em artifacts/dotnet9. Frameworks e dependências inalterados.
- Comandos e resultados:
  - `dotnet restore GetCNPJ.sln`: aprovado, projetos atualizados e sem dependências novas.
  - `dotnet build GetCNPJ.sln -c Release`: aprovado em netstandard2.0, net48, net481, net8.0 e net9.0, 1290 avisos de nulabilidade existentes e 0 erros. Log artifacts/t03-build.log.
  - Teste dirigido em net8.0, filtro SlidingWindowRateLimiterTests/RateLimiterIntegrationTests: 28 aprovados.
  - `dotnet test GetCNPJ.sln -c Release --no-build`: saída 1 por falta do runtime 9 global; 145 aprovados em cada um dos outros três targets, sem contar como aprovação global. Log artifacts/t03-test-solution.log.
  - `dotnet test GetCNPJ.sln -c Release --no-build -f <target> --settings tests/test.runsettings --logger trx --results-directory TestResults/T03/Full`: aprovado em net48, net481 e net8.0.
  - Mesmo comando para net9.0 com `-- RunConfiguration.DotNetHostPath="D:/Meus Projetos/GetCNPJ/artifacts/dotnet9/dotnet.exe"`: aprovado com runtime 9 correto, sem roll-forward. Suíte completa: 145 aprovados por target (580 execuções), 0 falhas e 0 ignorados; TRX em TestResults/T03/Full/.
- Testes obrigatórios cobertos: 100 aquisições concorrentes/limite 7; ondas de concorrentes na expiração; janela deslizante com registros em instantes distintos e fronteira exata; expiração de registros; provedores independentes; cancelamento antes/durante a espera; Reset acordando oito concorrentes com apenas uma vaga e sem fila desconectada; 2400 operações concorrentes de Reset/Record/GetAvailable/Acquire; limites/intervalos inválidos, defaults e positivos extremos; intervalo submilissegundo sem busy loop; registro único nos quatro provedores reais com HTTP simulado; implementação externa que só possui os quatro métodos originais e implementação opcional atômica, incluindo token e ordem de chamadas.
- Limitações: protocolo legado separado sem atomicidade por contrato; implementação externa antiga responsável por sua concorrência. Runtime Framework 4.8.1 atende ambos os targets, sem máquina isolada com só 4.8; netstandard2.0 validado por compilação. Persistem aviso VSTEST_ARTIFACTS_DIRECTORY do SDK 11/Test SDK 17.12.0 e necessidade de host explícito para net9.0 nesta máquina. T03 sem CI remoto nesta etapa. Nenhuma API real consultada.
- Handoff: código e diff revisados, git diff --check aprovado; LICENSE, autoria, contratos existentes, cinco targets e dependências preservados. T02 registrada como revisada e validada remotamente; T03 concluída localmente. Nenhum commit, push ou publicação. T04 não iniciada.
- Próximo passo exato: mantenedor revisar T03 e autorizar envio quando desejar, conferir o CI e solicitar T04 separadamente.

## Modelo para registros futuros
Copiar a seção abaixo para cada tarefa/transferência entre agentes. Atualizar a checklist somente conforme evidência real.

### AAAA-MM-DD — Agente — ID da tarefa
- Branch / commit de referência:
- Estado: pendente / em andamento / bloqueado / concluído
- Objetivo e critérios de aceite:
- Arquivos alterados:
- Decisões tomadas e motivos:
- Comandos executados e resultados:
- Testes executados / não executados:
- Problemas e limitações:
- Próximo passo exato:
