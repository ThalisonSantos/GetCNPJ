# Changelog

Alterações relevantes do fork serão registradas aqui, seguindo categorias de adição, alteração, correção, remoção e segurança. Adotar SemVer após definição da identidade do pacote.

## [Não publicado]
### Adicionado
- Matriz documental dos quatro provedores em docs/PROVIDERS.md e inventário/auditoria de todas as dependências em docs/DEPENDENCIES.md, verificados em 2026-11-24, sem consultas cadastrais reais.
- Endereco.CodigoIbge e flags nullable SimplesNacional.OptanteInformado/OptanteSimeiInformado e InscricaoEstadual.AtivoInformado; distinguem informação desconhecida de negativa, preservando os bools e ToString existentes. Adições podem aparecer na serialização dos modelos pelo consumidor.
- 79 casos sintéticos de contratos/mapeamento/identidade/erros JSON e fallback; suíte atual com 360 casos por target executável (1440 execuções aprovadas), preservados 281 anteriores com quatro expectativas corrigidas de listas [null]. NuGetAudit/all habilitado em restore/CI.
- Diretrizes de manutenção e colaboração entre agentes de IA.
- Auditoria técnica inicial, roadmap e controle de progresso.
- Projeto xUnit integrado à solução, com HTTP simulado e três testes iniciais executados em net48, net481, net8.0 e net9.0.
- Configuração para falhar quando não houver testes descobertos e instruções de validação em tests/README.md.
- NuGet.Config com fonte pública explícita para restauração independente das fontes privadas da máquina.
- Classe independente GetCNPJ.Validation.CnpjValidator para validar e normalizar CNPJs numéricos e alfanuméricos e aplicar a máscara, com cálculo de DV conforme manual oficial RFB/Serpro.
- Testes do exemplo oficial 12.ABC.345/01DE-35, vetores sintéticos, zeros iniciais, DVs, formato, cultura e caracteres proibidos; testes HTTP simulados dos quatro adaptadores para preservar letras nas URLs e respostas. Suíte ampliada para 117 casos por target executável.
- Interface opcional IAtomicRateLimiter e SlidingWindowRateLimiter.AcquireAsync para aquisição e registro atômicos, preservando os quatro membros de IRateLimiter.
- 28 casos de testes de concorrência, expiração, limites por provedor, cancelamento, Reset, parâmetros, registro único e compatibilidade; suíte ampliada para 145 casos por target executável na T03.
- 44 casos de timeout/fallback e cancelamento em provedores falsos/HTTP simulado, incluindo timer nativo de HttpClient sem rede; suíte ampliada para 189 casos por target executável na T04.
- 92 casos de recursos HTTP, com respostas/conteúdos/streams/handlers/clientes rastreáveis, falhas e reutilização; suíte atual com 281 casos por target executável.

### Alterado
- T05 registrada como concluída local e remotamente conforme revisão ChatGPT/validação GitHub Actions informadas pelo mantenedor: execução 45435902138, commit 038723f43c67f5527339a10cc5856625f555635b.
- System.Text.Json atualizado de 8.0.5 para patch estável 8.0.6 somente em netstandard2.0/net48/net481; net8/net9 usam componente do framework. Dependências de testes mantidas após auditoria direta/transitiva sem vulnerabilidades conhecidas reportadas. STJ 8.0.5 já corrigia CVE-2024-43485; atualização não atribui essa vulnerabilidade à versão anterior.
- Evidência documental de suporte alfanumérico confirmada para BrasilAPI e ReceitaWS; CNPJ.WS/CNPJá permanecem não confirmados. Nenhum teste real de suporte ou disponibilidade executado.
- CI em Windows para restore/build/test em push e pull request, com actions atualizadas e permissão de leitura.
- Fontes e recursos de testes e artefatos locais excluídos dos itens do projeto principal; cinco targets originais preservados.
- Métodos protegidos de validação, normalização e formatação da base delegam à classe central, sem alterar suas assinaturas. Letras ASCII minúsculas são normalizadas para maiúsculas e espaços externos são tolerados; DVs permanecem numéricos.
- Documentação registra a conclusão remota da T01 informada pelo mantenedor (execução 45415710723, commit 28ad05a1fe781658ce4faff5e2d4ccd3b1dc8475) e a validação local da T02.
- Conclusão remota da T02 registrada conforme revisão/validação informadas pelo mantenedor (execução 45418466167, commit e4863f10bbd4da2bcddfba59a54364607373b0ea).
- Conclusão remota da T03 registrada conforme revisão/validação informadas pelo mantenedor (execução 45428236555, commit 45d4a6476a7a661da96877295679849efdd32c8a).
- Conclusão remota da T04 registrada conforme revisão/validação informadas pelo mantenedor (execução 45431741152, commit 7dd0a9db79bdc6c9a1b8276adac5d7e6d05a8aa1).
- Anotações nullable locais nos argumentos opcionais de CnpjClient, sem mudança de assinatura CLR ou comportamento de null aceito.
- Expiração do limiter usa tempo decorrido de Stopwatch, com esperas fora dos locks e compatibilidade com intervalos positivos longos. Limites e intervalos não positivos passam a ser rejeitados no construtor; CnpjClient valida antes de criar seu HttpClient.

### Corrigido
- Contratos BrasilAPI: códigos numéricos, porte textual, nomes atuais de matriz/filial e logradouro, e-mail/IBGE. CNPJá: company.id textual tolerado no DTO interno, head ausente não vira FILIAL, IBGE e e-mail preenchido mapeados. CNPJ.WS tolera campos auxiliares string/objeto/null inconsistentes no schema/exemplos; listas ignoram itens null.
- CNPJ retornado ausente/inválido/divergente rejeitado na base (incluindo subclasses que a utilizam). Erros explícitos dentro de HTTP 200 deixam de ser sucesso; ReceitaWS ERROR gera ProviderException na consulta direta em vez de null, mantendo falha/fallback no cliente. Dados parciais com identidade válida continuam aceitos. JsonException preservada para JSON inválido; documentos/respostas descartados nas falhas.
- SIMEI isolado preservado; flags fiscais ausentes permanecem desconhecidas nas propriedades nullable. Capital textual não interpreta vírgula malformada como milhar; datas usam formatos documentados invariantes. Sem alterar máscara histórica de CNAE, apresentação de CNPJ ReceitaWS ou inferir situação tributária de lista IE vazia.
- Normalização deixa de remover letras e caracteres arbitrários. Entradas com máscara incorreta, símbolos, Unicode, espaços internos ou letras nos DVs são rejeitadas por InvalidCnpjException, em vez de poderem se transformar em outro identificador. Mensagens textuais de erro foram ajustadas; CNPJs numéricos válidos continuam aceitos.
- Corrida entre checagem e registro no limiter embutido corrigida pelo caminho atômico dos provedores, sem registro duplicado. Reset, GetAvailableRequests e RecordRequest compartilham a proteção da fila por provedor. Reset não desconecta chamadas em andamento e acorda esperas; registros expiram na fronteira exata da janela. Cancelamento durante espera não consome vaga.
- Timeout/OperationCanceledException sem cancelamento do token do consumidor deixa de interromper fallback automático. Consulta direcionada retorna falha do provedor sem fallback. Cancelamento explícito encerra novas tentativas antes/depois das chamadas e após espera do limiter. Preservadas prioridade, falhas completas quando todos falham, aquisição atômica, contratos públicos e ausência de retry.
- Respostas HTTP agora descartadas nas consultas dos quatro provedores e na verificação de disponibilidade herdada da base, incluindo conteúdo/streams em sucesso, falhas HTTP/JSON/mapeamento/leitura e fallback.
- HttpClient próprio limpo quando a configuração/construção de CnpjClient falha; Dispose repetido/concorrente libera a instância uma única vez. HttpClient externo nunca descartado pelo wrapper; compartilhamento/reutilização e buffering/timeout preservados.

### Removido
- Pacote System.Net.Http 4.3.4 e transitivas exclusivas, substituídos por referência ao assembly da plataforma nos targets Framework (biblioteca e testes), sem criar/descartar HttpClient adicional.
- Empacotamento e publicação NuGet do workflow, incluindo referência ao segredo de publicação.
- Geração automática de pacote no build; projeto de testes não empacotável.

T05 concluída remotamente conforme mantenedor. T06 concluída localmente com build dos cinco targets e 1440 execuções de testes aprovadas (net9 via host local); revisão/CI remoto pendentes. Chamadas legadas separadas WaitIfNeededAsync/RecordRequest continuam disponíveis e não constituem aquisição atômica; implementações externas antigas mantêm sua semântica. Validade de CNPJ não comprova cadastro ou operação externa. T07 não iniciada. Nenhum commit, push ou publicação realizado nesta tarefa.

## [1.0.0] — Versão original herdada
- Código herdado do projeto GetCNPJ de azumamagus.
- Consultas por múltiplos provedores, fallback e rate limiting.
- Histórico original e licença MIT a preservar.
