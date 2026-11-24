# Auditoria técnica inicial

Data: 2026-11-24. Base: develop, commit ea643091f02534494b6b63887d923a474d904ad8.

Escopo: inspeção estática inicial do projeto original. O mantenedor informou que a compilação original foi bem-sucedida. Não foram reproduzidos build, testes automatizados ou consultas reais nesta auditoria.

| ID | Prioridade | Estado | Evidência / ação recomendada |
|---|---|---|---|
| A01 | Alta | Confirmado por inspeção | Providers/Base/CnpjProviderBase.cs remove caracteres não numéricos e usa int.Parse no cálculo dos DVs. Implementar validação numérica e alfanumérica conforme especificação oficial da Receita Federal; testar exemplos oficiais e rejeição de entradas inválidas. |
| A02 | Alta | Confirmado por inspeção | RateLimiter/SlidingWindowRateLimiter.cs protege WaitIfNeededAsync com semáforo, mas RecordRequest, Reset e GetAvailableRequests não usam a mesma proteção. A reserva separada permite ultrapassar limites. Projetar aquisição atômica preservando contratos públicos; testar concorrência, cancelamento e parâmetros inválidos. |
| A03 | Alta | Confirmado por inspeção | Services/CnpjService.cs propaga toda OperationCanceledException, inclusive em consultas específicas. Timeouts HTTP podem impedir fallback. Distinguir cancelamento explícito de timeout e testar ambos. |
| A04 | Média | Parcialmente confirmado | CnpjProviderBase.IsAvailableAsync não descarta HttpResponseMessage explicitamente. Revisar todos os FetchDataAsync, respostas HTTP e ciclo de vida do HttpClient. |
| A05 | Alta | Dependências identificadas; risco a avaliar | GetCNPJ.csproj referencia System.Text.Json 8.0.5 e System.Net.Http 4.3.4. Auditar vulnerabilidades e compatibilidade antes de atualizar; não presumir vulnerabilidade sem verificação. |
| A06 | Alta | Confirmado por inspeção | Não foi localizado projeto de testes na árvore original. Criar testes e evitar inclusão acidental das fontes de teste pelo projeto SDK situado na raiz. |
| A07 | Alta | Confirmado por inspeção | .github/workflows/main.yml publica NuGet em push para main e usa checkout@main e setup-dotnet@v1. Separar CI de publicação; atualizar actions e exigir release autorizada. |
| A08 | Alta | Confirmado por inspeção | GetCNPJ.csproj mantém PackageId GetCNPJ, versão 1.0.0 e URL upstream. Definir identidade disponível e créditos antes da publicação. |
| A09 | Alta | Pendente | Revisar CNPJ.WS, BrasilAPI, CNPJá e ReceitaWS: endpoints, mapeamentos, dados ausentes, limites, termos e suporte alfanumérico. |
| A10 | Média | Pendente | Revisar métodos síncronos, API pública, nulabilidade, modelos, classificação de erros e gestão do HttpClient. |

## Procedimento de auditoria
Para cada item registrar evidência, reprodução, impacto, alteração proposta, testes de regressão e resultado. Não marcar problema como resolvido apenas porque o código foi modificado.

## Limitações
As falhas de concorrência e timeout ainda não foram reproduzidas em testes. Suporte alfanumérico dos provedores, disponibilidade das APIs e vulnerabilidades atuais não foram verificados. Ausência de IE no retorno não comprova condição tributária. A auditoria deve evoluir à medida que o código for revisado.
