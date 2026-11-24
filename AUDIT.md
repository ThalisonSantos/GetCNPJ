# Auditoria técnica inicial

Data: 2026-11-24. Base: develop, commit ea643091f02534494b6b63887d923a474d904ad8.

Escopo: inspeção estática inicial do projeto original. O mantenedor informou que a compilação original foi bem-sucedida. Não foram reproduzidos build, testes automatizados ou consultas reais nesta auditoria.

| ID | Prioridade | Estado | Evidência / ação recomendada |
|---|---|---|---|
| A01 | Alta | Resolvido localmente na T02 | Validation/CnpjValidator.cs centraliza normalização, máscara e validação numérica/alfanumérica pelo manual RFB/Serpro. Base delega mantendo métodos protegidos. Testes do exemplo oficial, vetores sintéticos, DVs, Unicode e entradas inválidas aprovados nos quatro targets executáveis. Letras não são removidas; caracteres proibidos são rejeitados. Não comprova cadastro ou suporte externo. |
| A02 | Alta | Confirmado por inspeção | RateLimiter/SlidingWindowRateLimiter.cs protege WaitIfNeededAsync com semáforo, mas RecordRequest, Reset e GetAvailableRequests não usam a mesma proteção. A reserva separada permite ultrapassar limites. Projetar aquisição atômica preservando contratos públicos; testar concorrência, cancelamento e parâmetros inválidos. |
| A03 | Alta | Confirmado por inspeção | Services/CnpjService.cs propaga toda OperationCanceledException, inclusive em consultas específicas. Timeouts HTTP podem impedir fallback. Distinguir cancelamento explícito de timeout e testar ambos. |
| A04 | Média | Parcialmente confirmado | CnpjProviderBase.IsAvailableAsync não descarta HttpResponseMessage explicitamente. Revisar todos os FetchDataAsync, respostas HTTP e ciclo de vida do HttpClient. |
| A05 | Alta | Dependências identificadas; risco a avaliar | GetCNPJ.csproj referencia System.Text.Json 8.0.5 e System.Net.Http 4.3.4. Auditar vulnerabilidades e compatibilidade antes de atualizar; não presumir vulnerabilidade sem verificação. |
| A06 | Alta | Resolvido na T01; cobertura inicial | Projeto tests/GetCNPJ.Tests integrado à solução; fontes/recursos de testes excluídos da biblioteca. Três testes HTTP determinísticos aprovados em cada um dos quatro targets executáveis (12 execuções). Itens Compile avaliados sem fontes de tests/. Cobertura funcional ampliada fica para tarefas posteriores. |
| A07 | Alta | Resolvido na T01; validação remota informada pelo mantenedor | CI somente restore/build/test, sem pack, push ou segredo NuGet. Sucesso de todas as etapas informado para a execução 45415710723, commit 28ad05a1fe781658ce4faff5e2d4ccd3b1dc8475. |
| A08 | Alta | Confirmado por inspeção | GetCNPJ.csproj mantém PackageId GetCNPJ, versão 1.0.0 e URL upstream. Definir identidade disponível e créditos antes da publicação. |
| A09 | Alta | Pendente | Revisar CNPJ.WS, BrasilAPI, CNPJá e ReceitaWS: endpoints, mapeamentos, dados ausentes, limites, termos e suporte alfanumérico. |
| A10 | Média | Pendente | Revisar métodos síncronos, API pública, nulabilidade, modelos, classificação de erros e gestão do HttpClient. |

## Procedimento de auditoria
Para cada item registrar evidência, reprodução, impacto, alteração proposta, testes de regressão e resultado. Não marcar problema como resolvido apenas porque o código foi modificado.

## Validação T01 — 2026-11-24
- Base: develop / bbc8a79; Windows 10.0.26200 x64, SDK 11.0.100 / MSBuild 18.12.2. O uso desse SDK local não adiciona targets .NET 10/11 à biblioteca.
- Restore aprovado após isolar NuGet.org em NuGet.Config e restaurar dependências com acesso de rede autorizado. A primeira tentativa falhou com NU1301, fonte privada herdada e TLS no sandbox.
- Build Release aprovado para netstandard2.0, net48, net481, net8.0 e net9.0: 0 erros e 1290 avisos de nulabilidade no código existente. Não houve alteração funcional nem supressão dos avisos.
- Teste global inicial: net48/net481/net8.0 aprovados; net9.0 abortado por ausência do runtime. Instalado runtime 9.0.23 em artifacts/dotnet9 e executado net9.0 com RunConfiguration.DotNetHostPath: 3/3 aprovados. Reexecuções por target com runsettings: 12 execuções aprovadas, nenhuma falha ou ignorado. Evidências TRX em TestResults/ (não versionadas).
- A instalação local expôs resolução indevida de referências do .NET Framework para DLLs em artifacts/; uma recompilação falhou. DefaultItemExcludes passou a excluir artifacts/** e a recompilação completa foi aprovada. Fontes de tests/ também permanecem excluídas.
- netstandard2.0 verificado por compilação; não tem runtime próprio. Os testes net48/net481 usam o runtime .NET Framework instalado nesta máquina, sem comprovar uma instalação isolada de 4.8.
- Nenhum provedor real consultado. Nenhum pacote gerado pelo build, publicado ou enviado. LICENSE, autoria, API e dependências de produção preservadas. T02 não iniciada.
- Proteção contra descoberta vazia reproduzida com filtro sem correspondência: execução retornou código 1 conforme esperado. O SDK 11 emitiu aviso de VSTEST_ARTIFACTS_DIRECTORY ausente com Test SDK 17.12.0, sem impedir a descoberta e aprovação dos testes reais.

## Confirmação remota da T01 — 2026-11-24
- O mantenedor informou sucesso de todas as etapas da execução [45415710723](https://github.com/ThalisonSantos/GetCNPJ/actions/runs/45415710723), commit 28ad05a1fe781658ce4faff5e2d4ccd3b1dc8475. T01 concluída; a pendência remota do registro histórico foi encerrada. A execução não foi repetida nesta tarefa.

## Validação T02 — 2026-11-24
- Base: develop / 28ad05a; árvore limpa. Ambiente local igual ao registrado na T01 (SDK 11.0.100, Windows x64 e runtime 9.0.23 local).
- Especificação consultada em 2026-11-24: [portal oficial](https://www.gov.br/receitafederal/pt-br/acesso-a-informacao/acoes-e-programas/programas-e-atividades/cnpj-alfanumerico) e [manual RFB/Serpro, pp. 3–4](https://www.gov.br/receitafederal/pt-br/centrais-de-conteudo/publicacoes/documentos-tecnicos/cnpj/manual-dv-cnpj.pdf). Letras ASCII maiúsculas usam valor do código menos 48; pesos de 2 a 9 da direita para a esquerda; módulo 11, resto 0/1 gera DV zero. O segundo cálculo inclui o primeiro DV. Exemplo oficial: 12.ABC.345/01DE-35, somatórios 459 e 424.
- `CnpjValidator` é independente de HTTP. `ValidateAndNormalize` aceita sem máscara ou máscara padrão, retorna maiúsculas ASCII e lança InvalidCnpjException para entradas inválidas. `IsValid` retorna bool. `Format` mantém a operação de apresentação anterior, sem validar DVs. Os três métodos protegidos da base continuam disponíveis, delegando à classe central.
- Compatibilidade: CNPJs numéricos válidos e zeros iniciais preservados; sem mudanças em assinaturas existentes, tipos dos modelos ou dependências. Política de minúsculas e espaços externos é conveniência local. Endurecimento documentado antes da implementação: máscara incorreta, símbolos, Unicode e caracteres extras agora são rejeitados em vez de descartados; a mensagem textual da exceção mudou, preservando seu tipo.
- Provedores revisados: os quatro FetchDataAsync concatenam o identificador string à URL, sem conversão numérica; modelos de resposta usam string. BrasilAPI, CNPJA e CNPJWS chamam FormatCnpj; ReceitaWS preserva diretamente response.cnpj. Os quatro caminhos foram testados com HTTP simulado, incluindo entrada numérica e alfanumérica, respostas com/sem máscara e rejeição antes do HTTP. Não foram necessárias alterações nos adaptadores concretos.
- `dotnet restore GetCNPJ.sln`: aprovado. `dotnet build GetCNPJ.sln -c Release`: cinco targets compilados, 1290 avisos de nulabilidade existentes e 0 erros; log em artifacts/t02-build.log.
- `dotnet test GetCNPJ.sln -c Release --no-build`: três targets aprovados com 117 casos cada, saída global 1 por ausência do runtime 9 na instalação global, como na T01. Execuções por target com runsettings/TRX e DotNetHostPath local para net9.0: 117 aprovados por target, 468 execuções, 0 falhas e 0 ignorados. Evidências em TestResults/T02/ (não versionadas). netstandard2.0 validado por compilação; .NET Framework 4.8/4.8.1 usam runtime 4.8.1 instalado.
- T02 ainda sem execução remota; nenhuma API real consultada. Suporte alfanumérico das APIs permanece não confirmado. T03 não iniciada; sem commit, push ou publicação.

## Limitações atuais
As falhas de concorrência e timeout ainda não foram reproduzidas em testes. Suporte alfanumérico dos provedores, disponibilidade das APIs e vulnerabilidades atuais não foram verificados. Ausência de IE no retorno não comprova condição tributária. A auditoria deve evoluir à medida que o código for revisado.
