# Changelog

Alterações relevantes do fork serão registradas aqui, seguindo categorias de adição, alteração, correção, remoção e segurança. Adotar SemVer após definição da identidade do pacote.

## [Não publicado]
### Adicionado
- Diretrizes de manutenção e colaboração entre agentes de IA.
- Auditoria técnica inicial, roadmap e controle de progresso.
- Projeto xUnit integrado à solução, com HTTP simulado e três testes iniciais executados em net48, net481, net8.0 e net9.0.
- Configuração para falhar quando não houver testes descobertos e instruções de validação em tests/README.md.
- NuGet.Config com fonte pública explícita para restauração independente das fontes privadas da máquina.

### Alterado
- CI em Windows para restore/build/test em push e pull request, com actions atualizadas e permissão de leitura.
- Fontes e recursos de testes e artefatos locais excluídos dos itens do projeto principal; cinco targets originais preservados.

### Removido
- Empacotamento e publicação NuGet do workflow, incluindo referência ao segredo de publicação.
- Geração automática de pacote no build; projeto de testes não empacotável.

Nenhuma alteração funcional na biblioteca. Publicação não realizada; T02 não iniciada.

## [1.0.0] — Versão original herdada
- Código herdado do projeto GetCNPJ de azumamagus.
- Consultas por múltiplos provedores, fallback e rate limiting.
- Histórico original e licença MIT a preservar.
