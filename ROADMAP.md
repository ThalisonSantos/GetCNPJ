# Roadmap — GetCNPJ

Versões para planejamento interno, sujeitas à revisão de compatibilidade. Mantenedor aprovou GetCNPJ.Community 1.1.0 como identidade inicial do fork; nenhuma publicação autorizada nesta etapa.

## 1.1.0 — Estabilização

| ID | Tarefa | Critérios de aceite |
|---|---|---|
| T01 | Infraestrutura de testes e CI sem publicação | Projeto de testes integrado à solução; fontes de teste excluídas do projeto principal; testes descobertos e executados; CI de build/test sem envio ao NuGet. Preservar targets atuais. |
| T02 | CNPJ alfanumérico | Normalização e cálculo dos DVs conforme documentação oficial; aceitar formato numérico anterior; testes com vetores oficiais e entradas inválidas. Separar validade do CNPJ de suporte pelo provedor. |
| T03 | Rate limiter | Reserva atômica; proteção coerente de operações concorrentes; limites respeitados; validação de parâmetros e cancelamento testados; compatibilidade pública revisada. |
| T04 | Timeout e fallback | Timeout de um provedor não interrompe fallback quando há outro disponível; cancelamento explícito encerra a consulta; erros registrados; testes com provedores simulados. |
| T05 | Recursos HTTP | Revisar descarte de respostas, propriedade do HttpClient, nulabilidade e métodos síncronos; testar ciclo de vida sem acessar redes externas. |
| T06 | Dependências e provedores | Auditar dependências diretas/transitivas; verificar documentação e mapeamentos dos quatro provedores; documentar limites e suporte a CNPJ alfanumérico; testes de contratos com respostas simuladas. |
| T07 | Identidade e release | Escolher PackageId com mantenedor; preservar autoria/licença; revisar metadados e workflow; empacotar e validar sem publicar automaticamente. |
| H01 | Nulabilidade e qualidade final 1.1.0 | Revisar diagnósticos/modelos/DTOs/mapeamentos e concluir revisão técnica local da T06; ausência preservada, contratos CLR mantidos, testes de regressão e build sem supressões. |
| H02 | Homologação real dos provedores | Console manual independente, plano com identidades públicas/endpoints/orçamento previamente autorizado, evidências mínimas sem dados sensíveis; corrigir apenas incompatibilidades comprovadas com testes sintéticos. |
| H02.1 | Complemento autorizado da homologação | Quatro alfanuméricas dirigidas sync e duas automáticas nos modos originais; teto12 GETs, intervalo21s, classificação por evidência e nenhuma consulta fora do plano. |

Ordem proposta: T01, T02, T03, T04, T05, T06, T07. A separação de CI e publicação deve ser feita antes de qualquer merge em main.

Estado em 2026-11-24:
- T01 concluída local e remotamente. Mantenedor confirmou sucesso de todas as etapas do GitHub Actions na execução [45415710723](https://github.com/ThalisonSantos/GetCNPJ/actions/runs/45415710723), commit 28ad05a1fe781658ce4faff5e2d4ccd3b1dc8475.
- T02 concluída local e remotamente: revisão pelo ChatGPT e GitHub Actions [45418466167](https://github.com/ThalisonSantos/GetCNPJ/actions/runs/45418466167), commit e4863f10bbd4da2bcddfba59a54364607373b0ea, informados pelo mantenedor. Suporte externo foi investigado documentalmente na T06; não houve testes reais.
- T03 concluída local e remotamente: revisão pelo ChatGPT e GitHub Actions [45428236555](https://github.com/ThalisonSantos/GetCNPJ/actions/runs/45428236555), commit 45d4a6476a7a661da96877295679849efdd32c8a, informados pelo mantenedor. Protocolo legado separado continua não atômico por compatibilidade.
- T04 concluída local e remotamente: revisão pelo ChatGPT e GitHub Actions [45431741152](https://github.com/ThalisonSantos/GetCNPJ/actions/runs/45431741152), commit 7dd0a9db79bdc6c9a1b8276adac5d7e6d05a8aa1, informados pelo mantenedor.
- T05 concluída local e remotamente: revisão pelo ChatGPT e GitHub Actions [45435902138](https://github.com/ThalisonSantos/GetCNPJ/actions/runs/45435902138), commit 038723f43c67f5527339a10cc5856625f555635b, informados pelo mantenedor. Evidências anteriores preservadas em AUDIT.md/PROGRESS.md.
- T06 validada remotamente conforme mantenedor: GitHub Actions [45443368700](https://github.com/ThalisonSantos/GetCNPJ/actions/runs/45443368700), commit 8652242894604cf28c96121ea2bee67b51fa0a7a. Revisão técnica ChatGPT parcial, sem bloqueios nos trechos revisados; não equivale a revisão integral ou teste real dos provedores. Limitações em docs/PROVIDERS.md e AUDIT.md preservadas.
- T07 concluiu a preparação local: GetCNPJ.Community 1.1.0, autoria/créditos/MIT preservados, logo excluído da distribuição sem prova de autorização específica. Nome sem pacote público nas consultas oficiais (404), sem garantia de reserva. Histórico: cinco targets, 360 testes por target, pack/inspeção e consumidores net48/net8 aprovados, com 1275 avisos então mantidos. Validação/revisão remota registradas abaixo; candidato histórico não contém H01 e publicação continua não autorizada.
- T07 validada remotamente conforme mantenedor: GitHub Actions [45450063413](https://github.com/ThalisonSantos/GetCNPJ/actions/runs/45450063413), commit b06d48c28a05f5310fc6b61bb211cd478f5efc62. Revisão ChatGPT delimitada a metadados, licença, empacotamento documentado e CI; não equivale a homologação das APIs ou da H01.
- H01 concluída localmente: 255 diagnósticos por target resolvidos sem supressões, rebuild dos cinco targets com zero avisos/erros, 450 testes por target (1800 execuções), preservados os 360 anteriores e 307 entradas de API CLR. Anotações nullable públicas documentadas em docs/RELEASE.md; revisão técnica local da T06 concluída no escopo da biblioteca/contratos documentados, sem teste operacional externo. Mantenedor informou revisão ChatGPT dos principais trechos e validação remota [45461414433](https://github.com/ThalisonSantos/GetCNPJ/actions/runs/45461414433), commit 9fcb8b893fc30b18371aff9faf44d153f1de0557; não equivale a revisão integral ou homologação das APIs.
- H02 original revisada pelo ChatGPT nos principais trechos e validada remotamente conforme mantenedor: [45467304721](https://github.com/ThalisonSantos/GetCNPJ/actions/runs/45467304721), commit5e07ff467fa8d917586c3c32f990b54686547836. Revisão delimitada, não integral, referente ao escopo numérico original.
- H02.1 concluída localmente no escopo complementar autorizado: seis consultas/seis GETs de até12, todos HTTP200/identidade correspondente. Quatro alfanuméricas dirigidas sync confirmam operação pontual nos quatro provedores; automáticas numérica async/alfanumérica sync encerram em CNPJWS sem consultas adicionais. Transição real após falha não observada, coberta por simulação. Sem incompatibilidade de adaptador comprovada; zero avisos/erros e450 testes por target preservados/aprovados. Evidências/limitações em docs/HOMOLOGATION.md.
- Próximo passo: revisar H02.1 e validar CI do complemento quando houver envio autorizado. Nenhuma consulta adicional autorizada pelo saldo não utilizado. Regenerar candidato1.1.0 somente após homologação, a partir de commit aprovado, repetindo validações do pacote. Pacote T07 histórico não contém H01. Nenhum pack/commit/push/merge/tag/publicação nesta tarefa. H03 e tarefas1.2.0 não iniciadas.

Critério de saída: build e testes aprovados nos ambientes pertinentes, compatibilidade revisada, documentação atualizada e autorização específica para release.

## 1.2.0 — Resiliência
- T08: cache opcional com TTL configurável, estratégia de invalidação e indicação de dados antigos.
- T09: limites e prioridades configuráveis por provedor; respeitar HTTP 429 e Retry-After.
- T10: timeout global, classificação de erros e logs opcionais sem dados pessoais desnecessários.
- T11: avaliar integração com injeção de dependências sem dependências obrigatórias desnecessárias.

## 1.3.0 — Expansão
- T12: avaliar novos provedores e autenticação opcional.
- T13: melhorar cobertura de IE e complementar dados com origem identificada e tratamento de conflitos.
- T14: automatizar release autorizada, documentação e exemplos de uso.

## Decisões pendentes
- Publicação futura de GetCNPJ.Community 1.1.0 depende de autorização específica, nova conferência de disponibilidade do nome e artefato aprovado; identidade já aprovada pelo mantenedor.
- Suporte explícito a .NET 10/11 sem abandono do .NET Framework 4.8.
- Políticas de cache e seleção de provedores.
