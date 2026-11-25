# GetCNPJ Community 1.1.0 — primeira release do fork independente

**GetCNPJ.Community 1.1.0 está disponível no [NuGet.org](https://www.nuget.org/packages/GetCNPJ.Community/1.1.0).**

Fork independente do GetCNPJ de **Bruno da Cunha Felipe**, mantido por **Thalison Santos**, com licença **MIT** e créditos/copyright originais preservados. Não possui afiliação oficial com a Receita Federal ou os serviços consultados.

## O que esta versão entrega

- Validação local de CNPJ numérico e alfanumérico conforme RFB/Serpro, preservando letras e zeros, com normalização, formatação e cálculo dos dígitos verificadores.
- Aquisição atômica no rate limiter embutido, segurança concorrente das filas, Reset e disponibilidade, limites independentes por provedor e cancelamento durante a espera.
- Fallback automático após timeout ou falha de um provedor. Cancelamento explícito interrompe novas tentativas; consultas dirigidas continuam sem fallback ou retry.
- Descarte de respostas HTTP em sucesso e falhas, com compartilhamento de HttpClient e propriedade dos recursos respeitados.
- Revisão dos contratos e mapeamentos de CNPJ.WS, ReceitaWS, BrasilAPI e CNPJá, incluindo correspondência do CNPJ retornado, erros em JSON com HTTP 200, respostas parciais e campos fiscais desconhecidos.
- Correção de nulabilidade nos modelos, DTOs e serviços sem supressões ou dados inventados. Código IBGE e flags nullable de Simples Nacional, MEI e situação da inscrição estadual disponíveis.

## Instalação e compatibilidade

```powershell
dotnet add package GetCNPJ.Community --version 1.1.0 --source https://api.nuget.org/v3/index.json
```

No Console do Gerenciador de Pacotes do Visual Studio, com NuGet.org selecionado:

```powershell
Install-Package GetCNPJ.Community -Version 1.1.0
```

Targets preservados: **netstandard2.0, net48, net481, net8.0 e net9.0**. Namespace e assembly continuam **GetCNPJ**; AssemblyVersion permanece **1.0.0.0**.

**Não instale GetCNPJ e GetCNPJ.Community simultaneamente**, inclusive por dependências transitivas. Substitua a referência anterior e revise o grafo de dependências.

As assinaturas públicas CLR existentes foram preservadas. As anotações nullable corrigidas podem exigir checagens ou ajustes em consumidores e overrides ao recompilar. Propriedades aditivas podem aparecer na serialização do consumidor.

Inscrição estadual ausente não comprova não contribuinte. Use `AtivoInformado`, `OptanteInformado` e `OptanteSimeiInformado` para distinguir informação desconhecida de negativa. Entradas com caracteres proibidos ou máscara incorreta são rejeitadas, em vez de transformadas por remoção indiscriminada. CNPJ localmente válido não comprova cadastro existente.

System.Text.Json 8.0.6 é usado somente nos targets legados; net8/net9 usam componentes do framework. System.Net.Http vem da plataforma. Revise conflitos de assemblies e binding redirects no aplicativo e mantenha os runtimes atualizados.

Consulte o [guia de integração](https://github.com/ThalisonSantos/GetCNPJ/blob/abaf152d756aa8283a43bf178cdc31dc7c724fd4/docs/INTEGRATION.md) e a [matriz dos provedores](https://github.com/ThalisonSantos/GetCNPJ/blob/abaf152d756aa8283a43bf178cdc31dc7c724fd4/docs/PROVIDERS.md).

## Evidências de validação

- Commit da versão: `abaf152d756aa8283a43bf178cdc31dc7c724fd4`. Tag **v1.1.0** confirmada nesse commit; merge e [GitHub Actions 45481606695](https://github.com/ThalisonSantos/GetCNPJ/actions/runs/45481606695) aprovados conforme confirmação do mantenedor.
- Antes da publicação: rebuild Release dos cinco targets com **zero avisos e zero erros**, **450 testes por framework executável** (1.800 execuções aprovadas), **20 cenários offline** e consumidores independentes net48/WinForms e net8 com 24 cenários cada. Pacote definitivo gerado uma única vez de checkout limpo do commit aprovado.
- Após a publicação: restauração exclusivamente do NuGet.org em cache novo e isolado, sem fonte local. Rebuild dos consumidores com **zero avisos e zero erros** e **24 cenários aprovados em cada consumidor**, com HTTP simulado. WinForms exercitou STA, ciclo de mensagens, await e atualização de controle sem janela visível.
- Assinatura de repositório do NuGet.org verificada com sucesso. As **14 entradas originais** do pacote publicado são idênticas, byte a byte, às do arquivo enviado; somente `.signature.p7s` foi acrescentada. RepositoryCommit e InformationalVersion foram confirmados nas cinco DLLs.
- O mantenedor confirmou instalação, compilação e teste no SysEmpresa, funcionando normalmente, com percepção de melhoria em relação à biblioteca original. É relato de homologação manual, sem testes individuais, ambiente ou métricas de desempenho presumidos. A validação pós-publicação não reexecutou o SysEmpresa.
- H02/H02.1 confirmaram operação alfanumérica pontual nos quatro provedores com cadastro público autorizado. Consultas automáticas reais encerraram no primeiro sucesso; a transição após falha foi validada com HTTP simulado. Nenhuma consulta cadastral real foi feita na preparação final ou na validação pós-publicação.
- A auditoria NuGet direta e transitiva anterior à publicação não reportou vulnerabilidades conhecidas; os restores pós-publicação também passaram com auditoria habilitada. Isso não garante ausência de vulnerabilidades futuras ou desconhecidas, nem audita os runtimes ou Windows.

## Identificação dos artefatos

Arquivo: **GetCNPJ.Community.1.1.0.nupkg**.

| Artefato | Tamanho | SHA-256 |
| --- | --- | --- |
| Definitivo original enviado | 227.903 bytes | `210FDD0AADEA40680D03625F68E6D83E719D930DC3D55AAAAA914BFA2D1A22E0` |
| Distribuído pelo NuGet.org, com assinatura de repositório | 240.996 bytes | `E81F066C26CA85877CE4B93EA1D439F0F9F83E0AD3F2870D5B55E8914337A883` |

RepositoryCommit: `abaf152d756aa8283a43bf178cdc31dc7c724fd4`.

InformationalVersion nos cinco assemblies: `1.1.0+abaf152d756aa8283a43bf178cdc31dc7c724fd4`.

A diferença entre os hashes dos arquivos completos é esperada pela assinatura adicionada pelo NuGet.org; não houve divergência no conteúdo original, incluindo DLLs, nuspec, documentos e licença.

## Limitações

As APIs públicas podem mudar, limitar requisições ou ficar indisponíveis. Os dados podem estar incompletos ou defasados; cobertura de IE, IBGE e campos fiscais varia por endpoint. Homologação pontual não garante todos os cadastros, cobertura integral ou consistência fiscal. Respeite cotas, limites de IP e termos comerciais: acesso gratuito não concede uso irrestrito ou redistribuição.

Nos testes locais, net48/net481 executaram sobre .NET Framework 4.8.1; não houve máquina isolada com apenas 4.8. netstandard2.0 foi compilado, sem runtime próprio. O relato do SysEmpresa não informa detalhes adicionais de ambiente ou cenários.

O workflow permanece dedicado a restore, build e testes, sem publicação automática.
