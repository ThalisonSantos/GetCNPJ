# Roadmap — GetCNPJ

Versões propostas para planejamento interno; sujeitas à revisão de compatibilidade e à escolha da identidade do pacote. Nenhuma publicação autorizada nesta etapa.

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

Ordem proposta: T01, T02, T03, T04, T05, T06, T07. A separação de CI e publicação deve ser feita antes de qualquer merge em main.

Estado em 2026-11-24:
- T01 concluída local e remotamente. Mantenedor confirmou sucesso de todas as etapas do GitHub Actions na execução [45415710723](https://github.com/ThalisonSantos/GetCNPJ/actions/runs/45415710723), commit 28ad05a1fe781658ce4faff5e2d4ccd3b1dc8475.
- T02 concluída local e remotamente: revisão pelo ChatGPT e GitHub Actions [45418466167](https://github.com/ThalisonSantos/GetCNPJ/actions/runs/45418466167), commit e4863f10bbd4da2bcddfba59a54364607373b0ea, informados pelo mantenedor. Suporte externo foi investigado documentalmente na T06; não houve testes reais.
- T03 concluída local e remotamente: revisão pelo ChatGPT e GitHub Actions [45428236555](https://github.com/ThalisonSantos/GetCNPJ/actions/runs/45428236555), commit 45d4a6476a7a661da96877295679849efdd32c8a, informados pelo mantenedor. Protocolo legado separado continua não atômico por compatibilidade.
- T04 concluída local e remotamente: revisão pelo ChatGPT e GitHub Actions [45431741152](https://github.com/ThalisonSantos/GetCNPJ/actions/runs/45431741152), commit 7dd0a9db79bdc6c9a1b8276adac5d7e6d05a8aa1, informados pelo mantenedor.
- T05 concluída local e remotamente: revisão pelo ChatGPT e GitHub Actions [45435902138](https://github.com/ThalisonSantos/GetCNPJ/actions/runs/45435902138), commit 038723f43c67f5527339a10cc5856625f555635b, informados pelo mantenedor. Evidências anteriores preservadas em AUDIT.md/PROGRESS.md.
- T06 concluída localmente: dependências diretas/transitivas auditadas sem vulnerabilidades conhecidas reportadas; referências condicionais por framework; contratos/mapeamentos dos quatro provedores corrigidos e matriz em docs/PROVIDERS.md. BrasilAPI/ReceitaWS com suporte alfanumérico confirmado por documentação oficial; CNPJ.WS/CNPJá não confirmados. Nenhuma consulta real. Cinco targets compilados, 360 casos aprovados por target executável (1440 execuções). Validação remota da T06 pendente.
- T07 não iniciada. Próximo passo é revisão da T06 e CI remoto pelo mantenedor. Nenhum commit, push ou publicação autorizado/executado nesta tarefa.

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
- Nome do pacote NuGet e estratégia de versionamento do fork.
- Suporte explícito a .NET 10/11 sem abandono do .NET Framework 4.8.
- Políticas de cache e seleção de provedores.
