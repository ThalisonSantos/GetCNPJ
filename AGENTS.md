# GetCNPJ — Diretrizes para agentes de desenvolvimento

## Objetivo
Evoluir o fork ThalisonSantos/GetCNPJ como biblioteca C# independente para consulta de CNPJ, confiável em produção e compatível com aplicações legadas e modernas. Preservar a licença MIT e os créditos originais.

## Regras obrigatórias
- Ler AGENTS.md, AUDIT.md, ROADMAP.md, PROGRESS.md e CHANGELOG.md antes de iniciar uma tarefa.
- Verificar `git status` e a branch atual. Trabalhar em `develop` ou em branches `feature/*` e `fix/*` derivadas de `develop`. Não alterar `main`, publicar NuGet ou fazer push sem autorização do mantenedor.
- Preservar inicialmente netstandard2.0, net48, net481, net8.0 e net9.0. Avaliar .NET 10/11 separadamente.
- Preservar a API pública, namespaces, contratos e comportamento compatível. Documentar propostas de quebra antes de implementá-las.
- Seguir as convenções de código existentes; evitar refatorações fora do escopo e dependências desnecessárias.
- Priorizar async/await, propagar CancellationToken e usar ConfigureAwait(false) internamente quando apropriado. Revisar métodos síncronos quanto a deadlocks.
- Não descartar HttpClient fornecido pelo consumidor. Descartar HttpResponseMessage corretamente.
- Respeitar limites e termos dos provedores. Não armazenar segredos, tokens ou dados reais de clientes no Git, testes ou logs.
- Não interpretar ausência de inscrição estadual como prova de não contribuinte. Não confundir CNPJ válido com cadastro existente.
- Preservar LICENSE e créditos do autor original. Não reutilizar a identidade do pacote NuGet original sem autorização.

## Processo de trabalho (Codex, Antigravity e ChatGPT)
1. Ler o último registro de PROGRESS.md e conferir o estado real dos arquivos e do Git.
2. Selecionar a próxima tarefa de ROADMAP.md; marcar como em andamento em PROGRESS.md, com escopo e critérios de aceite.
3. Inspecionar código, contratos e testes antes de editar. Confirmar especificações externas em fontes oficiais.
4. Implementar mudanças pequenas e testáveis. Priorizar testes determinísticos com HTTP simulado; consultas reais devem ser opcionais.
5. Executar os comandos de validação abaixo e registrar resultados reais, inclusive falhas e testes não executados.
6. Revisar o diff e atualizar AUDIT.md, PROGRESS.md e CHANGELOG.md conforme aplicável. Manter ROADMAP.md sincronizado com decisões aprovadas.
7. Encerrar a tarefa com um registro de handoff. Não repetir tarefas concluídas nem sobrescrever alterações locais de terceiros.

## Validação
```powershell
dotnet restore GetCNPJ.sln
dotnet build GetCNPJ.sln -c Release
dotnet test GetCNPJ.sln -c Release --no-build
```
Executar testes quando o projeto de testes existir. Validar targets .NET Framework em Windows com os reference assemblies necessários. Registrar ambiente, SDK, comandos, resultados e limitações. Uma execução bem-sucedida sem testes descobertos não equivale a testes aprovados.

## Critério de conclusão
Código revisado, testes pertinentes implementados e executados, compatibilidade verificada ou limitação explicitamente registrada, documentação atualizada e próximo passo definido.
