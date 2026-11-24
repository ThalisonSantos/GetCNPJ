# Testes e CI

O projeto `GetCNPJ.Tests` usa xUnit v2 e VSTest, com referência ao projeto da biblioteca. Executa os mesmos 360 casos em `net48`, `net481`, `net8.0` e `net9.0` (1440 execuções aprovadas na T06). Preservados os 281 casos da T05, com quatro expectativas de listas nulas ajustadas pela correção descrita abaixo. `netstandard2.0` é validado por compilação: é um contrato de APIs, sem runtime próprio.

Os testes exercitam o cliente público com apenas BrasilAPI habilitada: resposta HTTP de sucesso e mapeamento básico, HTTP 503 registrado como falha do provedor e seleção de provedor inexistente sem requisição HTTP. `StubHttpMessageHandler` intercepta todo HTTP; não há consulta real, credenciais ou dados de clientes. O CNPJ e a razão social são sintéticos e não representam evidência de cadastro existente.

A T02 adicionou 62 casos da classe independente `CnpjValidator`: CNPJs numéricos com zeros iniciais, exemplo alfanumérico oficial `12.ABC.345/01DE-35`, vetores sintéticos que cobrem A–Z e restos 0/1 nos dois DVs, máscara, letras minúsculas, cultura turca, entradas inválidas e caracteres proibidos. O valor esperado do exemplo oficial foi retirado das páginas 3–4 do [manual da RFB/Serpro](https://www.gov.br/receitafederal/pt-br/centrais-de-conteudo/publicacoes/documentos-tecnicos/cnpj/manual-dv-cnpj.pdf); os demais vetores são sintéticos, com DVs calculados separadamente por pesos explícitos.

Mais 52 casos verificam os quatro adaptadores de provedores: 32 combinações de entradas numéricas/alfanuméricas e respostas com/sem máscara preservam o identificador na URL e no mapeamento, e 20 entradas inválidas são rejeitadas antes do HTTP. ReceitaWS mantém o formato recebido, conforme comportamento anterior; os outros três aplicam a máscara. Esses testes não confirmam suporte das APIs externas.

A T03 adicionou 28 casos: 18 do limiter e 10 de integração/compatibilidade. Incluem 100 chamadas simultâneas para sete vagas, ondas concorrentes na expiração da janela, expiração individual na fronteira exata, provedores independentes, cancelamento antes/durante a espera, Reset acordando concorrentes sem perder a fila e 2400 operações concorrentes de Reset/Record/GetAvailable/Acquire. Também verificam parâmetros inválidos, valores positivos extremos, intervalo menor que 1 ms e protocolo legado, além de registro único nos quatro provedores e implementações externas antigas/opcionais.

`ManualRateLimiterTime` avança tempo explicitamente e controla as esperas; os testes não precisam aguardar minutos reais. A substituição é interna, acessível somente à assembly de testes por InternalsVisibleTo. `AsyncTest` usa um prazo de 15 segundos apenas para detectar travamentos; asserções de janela usam tempo manual. Evidências completas da T03 ficam em TestResults/T03/Full/. Não há nova dependência de produção ou teste.

A T04 adicionou 44 casos: falhas/timeouts em cadeia, preservação de todos os erros quando não há sucesso, prioridades e empates, consultas direcionadas sem fallback, cancelamento antes da consulta, durante HTTP/espera do limiter e entre tentativas, inclusive provedor que ignora o token e retorna outro resultado. Os quatro adaptadores são exercitados com HTTP simulado, sem requisições adicionais após sucesso/cancelamento. Exceções simuladas incluem OperationCanceledException, TaskCanceledException, TimeoutException interna, HttpRequestException e token interno cancelado sem cancelamento do consumidor.

Antes da correção, os dois casos GetAsync_TimeoutOfFirstProvider_AllowsSecondToSucceed falharam em net8.0: o timeout simulado escapou e impediu fallback. Evidência em TestResults/T04/Baseline/ e artifacts/t04-baseline-reproduction.log. Depois da correção, todos os casos foram aprovados; suíte completa em TestResults/T04/Full/.

Dois casos complementares usam o timer real de HttpClient (100 ms) sobre AsyncStubHttpMessageHandler local, sem rede nem asserção de duração exata. Capturam nos TRX a forma nativa da exceção: TaskCanceledException sem interna em net48/net481 e com TimeoutException interna em net8/net9 nesta máquina. Isso caracteriza o pipeline com handler simulado, não todos os transportes HTTP possíveis. O restante usa sinais explícitos, exceções simuladas ou tempo manual.

A T05 adicionou 92 casos: 82 de respostas/conteúdo/streams e 10 de propriedade do cliente. TrackedHttpResponse, TrackedContent (StreamContent), TrackedStream, TrackedHandler e TrackedHttpClient permitem observar o descarte real antes da limpeza do fixture. Cobrem sucesso e métodos síncronos; HTTP 400/404/429/500/503; JSON inválido/tipo incorreto/null; erro de mapeamento; falhas de buffering/leitura; disponibilidade; descarte antes da próxima tentativa de fallback; reutilização, propriedade, falha de construção e Dispose repetido/concorrente. O ponto de criação de cliente é interno, não uma nova API pública.

Antes da correção, os oito casos iniciais de sucesso/disponibilidade falharam em net8.0 porque a resposta não era descartada (DisposeCalls = 0). Evidências em TestResults/T05/Baseline/ e artifacts/t05-baseline-reproduction.log. Noventa casos novos passaram dirigidos em net8.0 e net48, antes dos dois complementares síncronos. Todos os 281 casos finais passaram nos quatro targets; TRX em TestResults/T05/Full/. Testes de buffering confirmam também a limpeza efetuada pelo próprio HttpClient quando a resposta ainda não foi entregue ao provedor.

Em Windows, usar SDK com suporte a .NET 9, runtimes .NET 8 e 9 e .NET Framework 4.8.1. Os reference assemblies de .NET Framework devem estar disponíveis pelos targeting packs ou pelo mecanismo de restauração do SDK. Como .NET Framework 4.8.1 substitui 4.8, os testes de ambos os targets usam o runtime instalado; não comprovam execução em uma máquina com apenas 4.8.

```powershell
dotnet restore GetCNPJ.sln
dotnet build GetCNPJ.sln -c Release
dotnet test GetCNPJ.sln -c Release --no-build --settings tests/test.runsettings --logger trx --results-directory TestResults
```

O arquivo `test.runsettings` faz a execução falhar quando nenhum teste é encontrado. Os resultados TRX ficam em `TestResults/`, ignorado pelo Git. O `NuGet.Config` da raiz limita a restauração à fonte pública NuGet.org, evitando dependência de fontes privadas da máquina.

O workflow `.github/workflows/main.yml` executa restore, build de todos os targets e testes em Windows, nos pushes das branches configuradas, em pull requests para `main`/`develop` e por acionamento manual. Não contém empacotamento, publicação ou uso de segredo NuGet. A biblioteca não gera pacote automaticamente no build, e o projeto de testes não é empacotável.

## Auditoria T06 e contratos dos provedores

A T06 adicionou 79 casos em `ProviderContractTests`. Os fixtures são strings JSON sintéticas nos testes, construídas a partir dos schemas oficiais descritos em [docs/PROVIDERS.md](../docs/PROVIDERS.md), sem respostas reais nem dados de pessoas. Exercitam os quatro mapeamentos, respostas completas/parciais, null e objeto vazio, tipos/códigos atuais, listas nulas/com membros null, IBGE versus SIAFI, CNAEs com zero inicial, CEP/telefone, capital decimal, datas/cultura, desconhecido versus negativo, SIMEI independente, CNPJ inválido/divergente e erros no JSON mesmo com HTTP 200. Dois casos verificam fallback e descarte quando o primeiro provedor retorna erro/identidade divergente. Casos de HTTP 400/404/429/500/503 e demais garantias anteriores continuam na suíte.

Antes da correção, os cinco primeiros casos novos falharam em net8.0 (1 incompatibilidade de tipos BrasilAPI e 4 divergências de identificador aceitas); evidências em `artifacts/t06-before.log` e `TestResults/T06/Before/`. Quatro casos antigos `MappingException...` esperavam NullReferenceException em `[null]`; foram renomeados para `NullListMember...` e agora verificam lista vazia e descarte, pois a falha foi corrigida. Nenhum dos 281 casos foi removido ou ignorado. O tipo público JsonException continua preservado para JSON inválido, inclusive após inspeção do envelope com JsonDocument.

Suíte final: 360 aprovados/zero falhas/zero ignorados por target (1440 execuções), TRX em `TestResults/T06/Final/`. Cinco targets compilados em Release, rebuild completo com 1275 avisos de nulabilidade e zero erros. O comando global encontrou ausência do runtime 9 global; execução net9 com host local aprovada. Frameworks net48/net481 usam o mesmo runtime 4.8.1, sem ambiente isolado 4.8.

System.Net.Http é referência explícita do Framework também no projeto de testes; System.Text.Json nos testes modernos vem do framework. Pacotes de testes mantidos após auditoria sem vulnerabilidade conhecida reportada; detalhes completos em [docs/DEPENDENCIES.md](../docs/DEPENDENCIES.md). `Directory.Build.props` habilita NuGetAudit para diretas e transitivas em restore/CI. Para repetir o inventário e auditoria:

```powershell
dotnet restore GetCNPJ.sln --force-evaluate --no-http-cache -p:NuGetAuditMode=all
dotnet list GetCNPJ.sln package --include-transitive --format json
dotnet list GetCNPJ.sln package --vulnerable --include-transitive --format json
```

Esses comandos acessam metadados NuGet, não APIs de cadastro. Todos os testes continuam locais; consultas reais exigem autorização específica. Evidência documental de suporte alfanumérico (BrasilAPI/ReceitaWS) não equivale a teste de operação real.

## Runtime .NET 9 local nesta validação

Nesta máquina, o runtime .NET 9 foi instalado somente em `artifacts/dotnet9`, ignorado pelo Git. Para repetir o teste desse target sem instalação global:

```powershell
$runtimeHost = Join-Path $PWD 'artifacts/dotnet9/dotnet.exe'
dotnet test GetCNPJ.sln -c Release --no-build -f net9.0 --settings tests/test.runsettings --logger trx --results-directory TestResults -- "RunConfiguration.DotNetHostPath=$runtimeHost"
```

Para `net48`, `net481` e `net8.0`, usar o comando normal com `-f <target>`. O CI instala os SDKs/runtimes 8 e 9 com `setup-dotnet`, seleciona o SDK 9 por um global.json temporário e executa a solução inteira.

Referências oficiais: [xUnit e VSTest](https://xunit.net/docs/nuget-packages-v3), [setup-dotnet](https://github.com/actions/setup-dotnet), [checkout](https://github.com/actions/checkout).
