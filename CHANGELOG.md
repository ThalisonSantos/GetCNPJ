# Changelog

Alterações relevantes do fork serão registradas aqui, seguindo categorias de adição, alteração, correção, remoção e segurança. Adotar SemVer após definição da identidade do pacote.

## [Não publicado]
### Adicionado
- Diretrizes de manutenção e colaboração entre agentes de IA.
- Auditoria técnica inicial, roadmap e controle de progresso.
- Projeto xUnit integrado à solução, com HTTP simulado e três testes iniciais executados em net48, net481, net8.0 e net9.0.
- Configuração para falhar quando não houver testes descobertos e instruções de validação em tests/README.md.
- NuGet.Config com fonte pública explícita para restauração independente das fontes privadas da máquina.
- Classe independente GetCNPJ.Validation.CnpjValidator para validar e normalizar CNPJs numéricos e alfanuméricos e aplicar a máscara, com cálculo de DV conforme manual oficial RFB/Serpro.
- Testes do exemplo oficial 12.ABC.345/01DE-35, vetores sintéticos, zeros iniciais, DVs, formato, cultura e caracteres proibidos; testes HTTP simulados dos quatro adaptadores para preservar letras nas URLs e respostas. Suíte ampliada para 117 casos por target executável.
- Interface opcional IAtomicRateLimiter e SlidingWindowRateLimiter.AcquireAsync para aquisição e registro atômicos, preservando os quatro membros de IRateLimiter.
- 28 casos de testes de concorrência, expiração, limites por provedor, cancelamento, Reset, parâmetros, registro único e compatibilidade; suíte atual com 145 casos por target executável.

### Alterado
- CI em Windows para restore/build/test em push e pull request, com actions atualizadas e permissão de leitura.
- Fontes e recursos de testes e artefatos locais excluídos dos itens do projeto principal; cinco targets originais preservados.
- Métodos protegidos de validação, normalização e formatação da base delegam à classe central, sem alterar suas assinaturas. Letras ASCII minúsculas são normalizadas para maiúsculas e espaços externos são tolerados; DVs permanecem numéricos.
- Documentação registra a conclusão remota da T01 informada pelo mantenedor (execução 45415710723, commit 28ad05a1fe781658ce4faff5e2d4ccd3b1dc8475) e a validação local da T02.
- Conclusão remota da T02 registrada conforme revisão/validação informadas pelo mantenedor (execução 45418466167, commit e4863f10bbd4da2bcddfba59a54364607373b0ea).
- Expiração do limiter usa tempo decorrido de Stopwatch, com esperas fora dos locks e compatibilidade com intervalos positivos longos. Limites e intervalos não positivos passam a ser rejeitados no construtor; CnpjClient valida antes de criar seu HttpClient.

### Corrigido
- Normalização deixa de remover letras e caracteres arbitrários. Entradas com máscara incorreta, símbolos, Unicode, espaços internos ou letras nos DVs são rejeitadas por InvalidCnpjException, em vez de poderem se transformar em outro identificador. Mensagens textuais de erro foram ajustadas; CNPJs numéricos válidos continuam aceitos.
- Corrida entre checagem e registro no limiter embutido corrigida pelo caminho atômico dos provedores, sem registro duplicado. Reset, GetAvailableRequests e RecordRequest compartilham a proteção da fila por provedor. Reset não desconecta chamadas em andamento e acorda esperas; registros expiram na fronteira exata da janela. Cancelamento durante espera não consome vaga.

### Removido
- Empacotamento e publicação NuGet do workflow, incluindo referência ao segredo de publicação.
- Geração automática de pacote no build; projeto de testes não empacotável.

T03 concluída localmente com build dos cinco targets e 580 execuções de testes aprovadas. Chamadas legadas separadas WaitIfNeededAsync/RecordRequest continuam disponíveis e não constituem aquisição atômica; implementações externas antigas mantêm sua semântica. Validade de CNPJ não comprova cadastro ou suporte externo. T04 não iniciada. Nenhum commit, push ou publicação realizado nesta tarefa.

## [1.0.0] — Versão original herdada
- Código herdado do projeto GetCNPJ de azumamagus.
- Consultas por múltiplos provedores, fallback e rate limiting.
- Histórico original e licença MIT a preservar.
