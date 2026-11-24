# GetCNPJ Community 1.1.0 — primeira release do fork independente

> Rascunho local em português, preparado em2026-11-24. Nenhuma GitHub Release foi criada. Antes de publicar, concluir o checklist em PUBLICATION.md, remover este aviso e preencher somente os dados reais do artefato definitivo no bloco final. Não usar os hashes dos candidatos T07/H03 como hash da release.

GetCNPJ Community é o fork independente do GetCNPJ de **Bruno da Cunha Felipe**, mantido por **Thalison Santos**, com licença **MIT** e créditos/copyright originais preservados. Não possui afiliação oficial com a Receita Federal ou os serviços consultados.

## O que esta versão entrega

- Validação local de CNPJ numérico e alfanumérico conforme RFB/Serpro, preservando letras/zeros, compatibilidade numérica e cálculo dos DVs.
- Aquisição atômica no limiter embutido, segurança concorrente das filas/Reset/disponibilidade, limites independentes por provedor e cancelamento da espera.
- Timeout de provedor permite fallback automático; cancelamento do consumidor interrompe novas tentativas. Consultas dirigidas continuam sem fallback/retry.
- Respostas HTTP descartadas em sucesso e falhas; compartilhamento de HttpClient, propriedade externo/interno e Dispose revisados.
- Contratos/mapeamentos de CNPJ.WS, ReceitaWS, BrasilAPI e CNPJá revistos, incluindo identidade retornada, erros em JSON HTTP200, dados parciais e campos fiscais desconhecidos.
- Nulabilidade corrigida nos modelos/DTOs/serviços sem suprimir diagnósticos ou inventar dados ausentes. IBGE e flags nullable de Simples/MEI/situação IE disponíveis.

## Compatibilidade e migração

PackageId **GetCNPJ.Community**, versão **1.1.0**. Namespace e assembly continuam **GetCNPJ**; AssemblyVersion1.0.0.0 preservada. Targets: **netstandard2.0, net48, net481, net8.0 e net9.0**.

Não instalar GetCNPJ e GetCNPJ.Community simultaneamente, nem por dependências transitivas. Trocar a referência ao pacote e revisar o grafo. Assinaturas CLR existentes foram preservadas, mas anotações nullable corretas podem exigir checagens/ajustes em consumidores/overrides ao recompilar. Propriedades aditivas podem aparecer na serialização do consumidor.

IE vazia não prova não contribuinte; usar AtivoInformado/OptanteInformado/OptanteSimeiInformado para distinguir desconhecido de negativo. Entradas com caracteres proibidos ou máscara incorreta passam a ser rejeitadas, em vez de transformadas por remoção indiscriminada. Detalhes no CHANGELOG e guia de integração do repositório.

Após confirmação da publicação no NuGet.org:

```powershell
dotnet add package GetCNPJ.Community --version 1.1.0
```

## Evidências de validação

- Rebuild Release dos cinco targets com zero avisos e zero erros;450 testes por framework executável,1800 execuções aprovadas, e20 cenários offline do console manual.
- H03 validou consumidores independentes net48/WinForms e net8 por fonte local/cache isolado:24 cenários sintéticos cada, incluindo cancelamento/timeout/recursos/nullable e ciclo de mensagens WinForms.
- O mantenedor confirmou que instalou, compilou e testou GetCNPJ.Community1.1.0 no SysEmpresa, funcionando normalmente, com percepção de melhoria em relação à biblioteca original. **É relato de homologação manual**, sem testes individuais, ambiente, medições ou ganho de desempenho presumidos.
- H02/H02.1 confirmaram operação alfanumérica pontual nos quatro provedores com cadastro público autorizado. Consultas automáticas reais encerraram no primeiro sucesso; passagem após falha permaneceu validada com HTTP simulado. A preparação final não fez novas consultas reais.
- Auditoria NuGet direta/transitiva final não reportou vulnerabilidades conhecidas, sem garantir ausência de falhas futuras/não catalogadas ou auditar runtimes/Windows.

## Limitações

Dados podem estar incompletos/defasados e os serviços podem mudar ou ficar indisponíveis. Cobertura de IE/IBGE/campos fiscais varia por endpoint. Homologação pontual não é garantia de todos os cadastros ou de consistência fiscal. Respeitar cotas/IP e termos comerciais dos serviços; acesso gratuito não concede uso irrestrito ou redistribuição.

Na validação automatizada local, net48/net481 executaram sobre Framework4.8.1; não houve máquina isolada com apenas4.8. netstandard2.0 foi compilado. O relato do SysEmpresa não informa detalhes adicionais de ambiente/cenários.

System.Text.Json8.0.6 é usado somente nos legados; net8/net9 usam componentes do framework. System.Net.Http vem da plataforma. Rever conflitos/redirects do aplicativo e manter runtimes corrigidos. O workflow continua apenas restore/build/test, sem publicação automática.

## Identificação do artefato definitivo — preencher após validação

Antes de publicar estas notas, inserir: SHA completo do commit final aprovado, execução CI desse SHA, nome/tamanho/SHA-256 do nupkg definitivo, InformationalVersion, resultado da inspeção/consumidores do mesmo arquivo e URL NuGet confirmada após indexação. Esses dados ainda não existem nesta preparação; não inferir dos candidatos anteriores. Tag prevista: **v1.1.0**, apontando exatamente ao commit usado no pacote.
