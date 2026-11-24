# H02 — Homologação manual dos provedores

## Estado em 2026-11-24

**Homologação parcial executada no escopo autorizado: quatro consultas numéricas dirigidas, assíncronas, com HTTP 200 e identidade correspondente nos quatro provedores.** O mantenedor autorizou expressamente somente essas quatro consultas. As consultas alfanuméricas, síncronas e com fallback do plano original não foram autorizadas nem executadas. Suporte operacional alfanumérico permanece inconclusivo. A leitura de páginas institucionais/documentação não constitui consulta aos endpoints cadastrais.

Base: `develop`, commit `9fcb8b893fc30b18371aff9faf44d153f1de0557`. Conforme informação do mantenedor, a H01 teve revisão técnica dos principais trechos pelo ChatGPT e [GitHub Actions 45461414433](https://github.com/ThalisonSantos/GetCNPJ/actions/runs/45461414433) aprovado nessa revisão. Isso não equivale a revisão integral ou homologação dos serviços externos.

Ambiente local: Windows 10.0.26200 x64, SDK 11.0.100/MSBuild 18.12.2, .NET 8.0.34, .NET Framework 4.8.1 e runtime 9.0.23 local. Targets da biblioteca preservados: netstandard2.0, net48, net481, net8.0 e net9.0. Não houve alteração de versão, contratos públicos, adaptadores, dependências ou workflow.

## Aplicação independente

Projeto: `tools/GetCNPJ.Homologation/GetCNPJ.Homologation.csproj`, console net8.0 com ProjectReference para a biblioteca atual. Fora da solução, suíte padrão, CI e pacote. Não utiliza o candidato NuGet histórico, que não contém H01.

```powershell
dotnet build tools/GetCNPJ.Homologation/GetCNPJ.Homologation.csproj -c Release
# Somente HTTP sintético, sem rede:
dotnet tools/GetCNPJ.Homologation/bin/Release/net8.0/GetCNPJ.Homologation.dll --self-test
# Prévia: valida localmente, mostra endpoints/orçamento, sem rede:
dotnet tools/GetCNPJ.Homologation/bin/Release/net8.0/GetCNPJ.Homologation.dll --cnpj 00000000000191 --provider CNPJWS --mode async --expected-name "BANCO DO BRASIL SA"
```

`--provider`: CNPJWS, ReceitaWS, BrasilAPI, CNPJA ou auto. `--mode`: sync ou async; usa efetivamente as respectivas APIs públicas da biblioteca. Sem argumentos exibe ajuda. Sem `--execute` nunca cria transporte de rede. Somente depois da autorização específica, acrescentar `--execute` e confirmar `CONSULTAR` no stdin. Essa confirmação local não substitui a autorização prévia do mantenedor.

Uma execução realiza uma consulta lógica: até um GET por provedor selecionado, sem retry, redirecionamento, healthcheck, cookies ou autenticação. Timeout HTTP de 30 segundos; HttpClient compartilhado pelos provedores e descartado pelo console ao final. Ctrl+C cancela o transporte; a API síncrona original não recebe token, mas a guarda do handler impede novos GETs após cancelamento. HTTP 401/403/429 e qualquer 3xx interrompem o transporte e exigem parar todo o lote, inclusive as próximas invocações manuais. Nenhum contorno ou repetição é autorizado.

Saída: UTC, ambiente, modo, duração total e por tentativa, contagem, HTTP quando recebido, sucesso/provedor, categoria/tipos de erro, presença dos campos na fonte e no modelo, correspondência do CNPJ e alertas de consistência. Mensagens externas e corpos JSON não são impressos, pois exceções da biblioteca podem incluir o corpo HTTP. O console não grava arquivos. Telefones, e-mails e sócios nunca têm seus valores exibidos. Não salvar respostas completas ou dados sensíveis nem em artefatos locais/logs.

As checagens locais conferem identidade, razão social de referência opcional, formato de CEP/IBGE/CNAEs, capital negativo, datas futuras e contradição MEI positivo/Simples negativo. A saída distingue null, ausente, vazio, lista vazia, negativo explícito e opção desconhecida. Mostra presença de endereço e atividades, datas, capital e UF/situação informada das IEs, omitindo seus números. Não certifica existência de código IBGE/CNAE, correção tributária, atualização cadastral, completude de IE nem equivalência de matriz e filial. Campo preenchido na fonte e ausente no modelo gera alerta para investigação, não conclusão automática de defeito.

## Identidades propostas e fontes públicas

Verificação documental em **2026-11-24**, sem consultas cadastrais:

| Caso | CNPJ | Organização e evidência |
|---|---|---|
| N — numérico | `00.000.000/0001-91` (`00000000000191`) | Banco do Brasil S.A., matriz. [Cadastro da companhia na B3](https://sistemaswebb3-listados.b3.com.br/listedCompaniesPage/main/1023/CODBOLSA/overview?language=pt-br). |
| A — alfanumérico | `00.000.000/E08G-12` (`00000000E08G12`) | Filial do Banco do Brasil S.A. identificada expressamente na [notícia oficial da Receita Federal, publicada em 31/07/2026](https://www.gov.br/receitafederal/pt-br/assuntos/noticias/2026/julho/receita-federal-gera-o-primeiro-cnpj-em-formato-alfanumerico). |

Ambos passam pela validação local. O alfanumérico foi obtido da publicação oficial, sem geração aleatória. A publicação da RFB comprova a referência documental do cadastro; não comprova sua presença ou atualização nos quatro serviços. Para a filial não se presume o endereço, CNAE ou situação da matriz.

## Plano original e autorização efetiva

Proposta original: **10 consultas lógicas, no máximo 16 GETs cadastrais**, sem credenciais/serviços pagos. **Autorização recebida nesta sessão: “Autorizar somente as 4 consultas numéricas dirigidas”. Somente as ordens 1–4 abaixo foram executadas. As ordens 5–10 continuam sem autorização.** Aguardar **pelo menos 21 segundos após o término de cada consulta lógica** antes da seguinte, inclusive entre invocações do console. Não executar em paralelo nem compartilhar o mesmo IP com outras homologações. Limites documentados: CNPJ.WS/ReceitaWS 3/min, CNPJá Open 5/min/IP; BrasilAPI sem cota numérica documentada, adotado o mesmo espaçamento conservador. Fontes e restrições comerciais: [PROVIDERS.md](PROVIDERS.md). Isso não autoriza revenda ou redistribuição de resultados.

| Ordem | Caso | Seleção | Modo | Máximo de GETs |
|---|---|---|---|---|
| 1–4 | N | CNPJWS, ReceitaWS, BrasilAPI, CNPJA, uma consulta dirigida para cada | async | 4 (1 cada) |
| 5–8 | A | CNPJWS, ReceitaWS, BrasilAPI, CNPJA, uma consulta dirigida para cada | sync | 4 (1 cada) |
| 9 | N | auto, prioridade original CNPJWS → ReceitaWS → BrasilAPI → CNPJA | async | 4 |
| 10 | A | auto, mesma prioridade | sync | 4 |

Máximo global por provedor: 4 GETs distribuídos no lote; por CNPJ/provedor: 2. Fallback só continua diante de falhas que não sejam bloqueio/cancelamento; termina no primeiro sucesso. O lote inteiro para em 401/403/429/3xx, ainda que restem consultas no orçamento. Não realizar novas tentativas para recuperar testes não executados sem nova autorização. Falhas sem HTTP recebido permanecem sem status; não inventar resultado do servidor. O orçamento é teto, não meta.

Endpoints exatos (GET, sem parâmetros adicionais):

| Provedor | Caso N | Caso A |
|---|---|---|
| CNPJWS | `https://publica.cnpj.ws/cnpj/00000000000191` | `https://publica.cnpj.ws/cnpj/00000000E08G12` |
| ReceitaWS | `https://receitaws.com.br/v1/cnpj/00000000000191` | `https://receitaws.com.br/v1/cnpj/00000000E08G12` |
| BrasilAPI | `https://brasilapi.com.br/api/cnpj/v1/00000000000191` | `https://brasilapi.com.br/api/cnpj/v1/00000000E08G12` |
| CNPJA | `https://open.cnpja.com/office/00000000000191` | `https://open.cnpja.com/office/00000000E08G12` |

## Evidências locais e registro operacional

Restore da solução com auditoria configurada aprovado (`artifacts/h02-restore.log`). Rebuild Release completo dos cinco targets aprovado, zero avisos/erros (`artifacts/h02-build.log`). Preservados e aprovados **450 testes por target executável, 1800 execuções**, zero falhas/ignorados. Comando global terminou com código 1 pela ausência de runtime 9 global; net48/net481/net8 passaram nessa execução e net9 passou com `RunConfiguration.DotNetHostPath` apontando para `artifacts/dotnet9/dotnet.exe`. TRX em `TestResults/H02/Solution` e `TestResults/H02/Net9`; logs `artifacts/h02-test-*.log`. Aviso VSTest sobre VSTEST_ARTIFACTS_DIRECTORY já existente permanece sem supressão; não é diagnóstico de compilação. net48 executa sobre runtime Framework4.8.1, sem ambiente isolado4.8; netstandard somente compilado.

Console: build Release sem avisos/erros e 20 cenários offline aprovados, cobrindo quatro provedores nos dois modos, fallback ordenado, parada em 401/403/429/3xx, redação de contatos/sócios sintéticos, checagens de consistência e prévia sem execução. São testes auxiliares manuais com handlers falsos; não substituem os 450 casos da biblioteca nem homologam servidores reais.

Após atualizar README/metadados para refletir a homologação numérica parcial, repetidos o rebuild completo da solução, build do console e seus20 cenários: zero avisos/erros. Validação final executada separadamente com `dotnet test tests/GetCNPJ.Tests/GetCNPJ.Tests.csproj -c Release -f <target> --no-build --settings tests/test.runsettings --results-directory TestResults/H02/Final --logger trx`, para net48/net481/net8.0/net9.0 (neste último acrescentado `-- "RunConfiguration.DotNetHostPath=D:/Meus Projetos/GetCNPJ/artifacts/dotnet9/dotnet.exe"`). Os quatro comandos terminaram com código0,450 aprovados cada, zero falhas/ignorados. Logs `artifacts/h02-test-final-<target>.log` e quatro TRX em `TestResults/H02/Final`. A falha inicial global por runtime ausente continua registrada, sem ser ocultada.

Execução em 2026-11-24, mesmo ambiente descrito acima. Quatro consultas lógicas N/dirigidas/async; **quatro GETs com resposta HTTP**, um por provedor. Houve ainda uma tentativa inicial no sandbox ao CNPJ.WS, que falhou na autenticação TLS sem resposta HTTP: UTC 18:53:49.479Z, tempo total205,9ms, transporte191,5ms, cadeia ProviderException → HttpRequestException → AuthenticationException → Win32Exception. Portanto **cinco tentativas de envio no handler**, das quais uma não chegou a obter HTTP. A saída inicial chamava esse contador de “requisições reais”; o rótulo foi corrigido para “tentativas de envio”, sem alterar a biblioteca.

O erro TLS do sandbox foi tratado com execução de rede explicitamente autorizada fora dele, mantendo validação TLS normal. Não houve retry de resposta de servidor, remoção de verificação de certificado, redirecionamento ou contorno de bloqueio. Os quatro acessos com resposta HTTP ocorreram separadamente, com mais de21s entre términos/inícios. Nenhum 401/403/429/3xx, timeout ou falha de mapeamento recebido. Não foram gravados corpos HTTP nem mensagens externas; evidência abaixo transcrita da saída mínima saneada.

| Ordem / provedor | UTC do término (2026-11-24) | GETs com HTTP | HTTP | Transporte + leitura (ms) | Total consulta (ms) | Resultado |
|---|---|---|---|---|---|---|
| 1 / CNPJ.WS | 18:55:57.715Z | 1 | 200 | 588,7 | 649,5 | Sucesso; CNPJ confere; sem alerta local |
| 2 / ReceitaWS | 18:56:59.159Z | 1 | 200 | 410,1 | 450,2 | Sucesso; CNPJ confere; sem alerta local |
| 3 / BrasilAPI | 18:59:14.129Z | 1 | 200 | 437,2 | 485,7 | Sucesso; CNPJ confere; sem alerta local |
| 4 / CNPJá Open | 19:03:43.101Z | 1 | 200 | 502,3 | 544,2 | Sucesso; CNPJ confere; sem alerta local |

### Campos e consistência observados

Todos retornaram a identidade `00000000000191`, razão social compatível com a referência pública Banco do Brasil S.A., nome fantasia Direção Geral (variação de caixa/grafia), situação ativa, CEP equivalente após retirar máscara, CNAE principal com sete dígitos e um CNAE secundário. Endereço/logradouro presentes na fonte e modelo; não se transcreveu o endereço completo nem se certificou sua entrega postal. IBGE presente e consistente entre CNPJ.WS/BrasilAPI/CNPJá; não disponível na ReceitaWS nesse contrato. Contagens e formatos de atividades foram conferidos; não houve validação externa dos códigos ou comparação detalhada das descrições secundárias.

Capital social numérico e coerente nos quatro; mesma abertura e data de situação cadastral, sem datas futuras. Simples e MEI vieram expressamente negativos, corretamente preservados como negativos informados, sem inferência por ausência. Não se realizou consulta fiscal complementar. A publicação não substitui certidão ou comprovante cadastral/fiscal.

| Informação | CNPJ.WS | ReceitaWS | BrasilAPI | CNPJá Open |
|---|---|---|---|---|
| CNPJ, razão social, fantasia, situação, endereço/CEP | Presentes | Presentes | Presentes | Presentes |
| IBGE municipal | Presente | Ausente no modelo; não documentado na fonte | Presente | Presente |
| CNAE principal / secundários | Presente / 1 item | Presente / 1 item | Presente / 1 item | Presente / 1 item |
| IE e situação | 1 IE, UF DF, ativa explicitamente, data2025-10-10 | Lista vazia; recurso não documentado nesse endpoint | Lista vazia; recurso não documentado | Lista vazia; recurso comercial, fora do Open |
| Simples / MEI | Negativo / negativo explícitos | Negativo / negativo explícitos | Negativo / negativo explícitos | Negativo / negativo explícitos |
| Capital / abertura / data da situação | Presentes | Presentes | Presentes | Presentes |
| Última atualização | 2026-11-14 | 2026-11-24 | Ausente, conforme contrato | 2026-11-24 |

IE vazia nos três serviços não contradiz a IE presente no CNPJ.WS: são coberturas distintas, sem prova de não contribuinte. Números de IE não foram persistidos nem confrontados com cadastro estadual. Datas de atualização diferentes não provam defeito; refletem fontes/cache distintos. Nenhum campo central ausente nas quatro consultas observadas; isso não garante respostas completas para outras empresas. Nenhum alerta fonte preenchida/modelo ausente nos campos documentados inspecionados. Não houve incompatibilidade comprovada que justificasse alterar adaptadores ou expectativas dos450 testes. Não se adicionou fixture com resposta real.

### Limitações e classificação alfanumérica

| Provedor | Evidência documental alfanumérica | Teste alfanumérico real | Classificação operacional |
|---|---|---|---|
| CNPJ.WS | Não confirmado | Não autorizado/não executado | Inconclusiva; não confirmado |
| ReceitaWS | Confirmado por documentação oficial | Não autorizado/não executado | Inconclusiva; sem confirmação por teste real |
| BrasilAPI | Confirmado por documentação oficial | Não autorizado/não executado | Inconclusiva; sem confirmação por teste real |
| CNPJá Open | Não confirmado | Não autorizado/não executado | Inconclusiva; não confirmado |

Consultas reais sync e fallback também não autorizadas/não executadas. Seus caminhos passaram apenas nos20 cenários sintéticos do console e na suíte da biblioteca, sem homologação operacional. Não houve consultas em massa, geração aleatória, uso de credenciais/planos pagos, coleta de clientes ou acesso a APIs estaduais adicionais.

HTTP404, erro genérico ou ausência de registro alfanumérico não provariam falta de suporte. Somente resposta alfanumérica real autorizada, mapeada corretamente e com identidade correspondente permitiria classificar aquele provedor como **Confirmado por teste real autorizado** para esse formato. O sucesso numérico aqui não altera essa classificação. Não suportado exige evidência explícita.

Handoff: escopo autorizado concluído, **H02 parcialmente homologada**, demais cenários dependem de nova autorização específica. Não repetir as quatro consultas para ampliar o relatório sem autorização. H03 não iniciada; nenhum pacote regenerado; candidato anterior histórico e futura geração exige commit aprovado e homologação.
