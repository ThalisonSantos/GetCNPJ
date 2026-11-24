# Changelog

## [1.1.0] — Preparada para publicação, ainda não publicada

Primeira versão do fork independente **GetCNPJ.Community**, mantido por **Thalison Santos**, derivado de GetCNPJ de **Bruno da Cunha Felipe**. Licença MIT e copyright original preservados. Preparação final registrada em2026-11-24; sem data de publicação, tag ou GitHub Release criada nesta etapa.

### Adicionado

- Validação independente de CNPJ numérico/alfanumérico em CnpjValidator, com normalização preservando letras, DV conforme RFB/Serpro e formatação. Validade local não comprova cadastro.
- IAtomicRateLimiter/AcquireAsync opcionais, preservando implementações existentes de IRateLimiter; testes de concorrência, cancelamento, Reset e expiração.
- Endereco.CodigoIbge e flags nullable OptanteInformado/OptanteSimeiInformado/AtivoInformado para distinguir ausência de negativa fiscal.
- Infraestrutura de testes e CI sem publicação:450 casos por framework executável,20 cenários offline do console e consumidores independentes net48/WinForms/net8 com24 cenários cada na H03.
- Console manual de homologação fora da suíte/CI/pacote; relatórios de provedores, dependências, integração, candidatos, notas e procedimento de release.

### Corrigido

- Corrida entre espera e registro no limiter embutido; filas internas protegidas, limites por provedor independentes, aquisição única, tempo monotônico e parâmetros inválidos rejeitados.
- Timeout/falha de um provedor permite fallback automático; cancelamento explícito interrompe novas tentativas e consultas dirigidas permanecem sem fallback ou retry. Falhas completas preservadas.
- Descarte de respostas/conteúdos/streams em sucesso, erros HTTP/JSON, disponibilidade e fallback. Reutilização de HttpClient e propriedade externo/interno preservadas, com limpeza em falha de construção e Dispose seguro repetido.
- DTOs/mapeamentos de CNPJ.WS, ReceitaWS, BrasilAPI e CNPJá: tipos/aliases, listas com null, IBGE/CNAEs/datas/capital/flags fiscais. Identidade ausente/inválida/divergente e envelopes de erro HTTP200 rejeitados; respostas parciais com identidade válida continuam aceitas.
- Nulabilidade real nos modelos/DTOs/serviços:1275 avisos históricos resolvidos sem supressões ou dados fabricados; guardas contra NRE, FailedProviders com itens null, CreateSuccess(null), configuração de provedores e coleção capturada uma vez.

### Alterado e compatibilidade

- Identidade aprovada: PackageId GetCNPJ.Community, Title GetCNPJ Community, versão1.1.0; namespace/assembly GetCNPJ e AssemblyVersion1.0.0.0 preservados, FileVersion1.1.0.0.
- Cinco targets preservados: netstandard2.0, net48, net481, net8.0 e net9.0. Mesmas assinaturas públicas CLR; anotações nullable podem exigir checagens/ajustes em consumidores/overrides ao recompilar.
- Entradas com caracteres proibidos/máscara incorreta agora são rejeitadas em vez de transformadas por remoção indiscriminada. Minúsculas ASCII e espaços externos são conveniências de entrada; DVs continuam numéricos.
- Lista de IE vazia não comprova não contribuinte. Bools legados mantêm suas projeções; usar propriedades nullable para distinguir desconhecido. Campos aditivos podem aparecer na serialização do consumidor. ReceitaWS preserva a apresentação recebida e máscaras públicas históricas de CNAE continuam compatíveis.
- System.Text.Json8.0.5→8.0.6 apenas nos legados; net8/net9 usam framework. System.Net.Http4.3.4 removido em favor da plataforma. Auditoria direta/transitiva não reportou vulnerabilidades conhecidas; não se atribui CVE-2024-43485 a8.0.5, que já era correção.
- README/exemplos/modelos e documentação sincronizados com ausência fiscal, propriedade HTTP, evidência operacional e migração. GetCNPJ e GetCNPJ.Community não podem coexistir no mesmo grafo.

### Removido

- Publicação/pack automáticos e segredo NuGet do workflow; geração de pacote em build.
- Referência comercial FeTx dos metadados e logotipo da distribuição/README sem comprovação específica de autorização. Créditos e LICENSE originais preservados; logo histórico permanece somente no repositório.

### Homologação e limites

- T01–T07, H01, H02, H02.1 e H03 concluídas nos escopos documentados. Revisões/CI remotos informados pelo mantenedor, com limites preservados em AUDIT.md/PROGRESS.md.
- H02/H02.1: quatro consultas numéricas dirigidas e seis complementares autorizadas, com HTTP200/identidade correspondente. Operação alfanumérica pontual confirmada nos quatro provedores. Automáticas encerraram em CNPJWS; transição real após falha não observada, coberta por HTTP simulado.
- H03: revisão ChatGPT e CI45475015057 para2e676ae4055cff9ab0497f2d644b8c2c305c7f0b informados. Mantenedor confirmou instalação, compilação e teste no SysEmpresa, funcionando normalmente, com percepção de melhoria ante a original. Relato manual sem detalhes de casos/métricas/ambiente; não é benchmark ou certificação de todos os fluxos.
- Candidatos T07/H03, caminhos, tamanhos e hashes preservados em docs/RELEASE.md. Nenhum pacote definitivo gerado nesta preparação: dependerá de commit final aprovado, árvore limpa, CI, inspeção, consumidores e autorizações externas separadas.
- Provedores sem SLA/cobertura fiscal garantidos; termos/cotas e informações ausentes permanecem aplicáveis. Framework4.8 testado localmente sobre4.8.1; netstandard compilado. Não houve consulta real nova nem implementação1.2.0 nesta preparação.

## [1.0.0] — Versão original herdada

Código original de Bruno da Cunha Felipe ([azumamagus/GetCNPJ](https://github.com/azumamagus/GetCNPJ)), com múltiplos provedores, fallback e rate limiting. O histórico de evolução e validações T01–H03 permanece em AUDIT.md, PROGRESS.md e nos relatórios em docs/; a consolidação acima descreve o resultado1.1.0 sem substituir essas evidências.
