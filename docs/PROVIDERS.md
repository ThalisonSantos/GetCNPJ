# Provedores — auditoria T06

Atualização H02 em 2026-11-24: console e evidências em [HOMOLOGATION.md](HOMOLOGATION.md). Mantenedor autorizou somente quatro consultas numéricas dirigidas/async ao Banco do Brasil matriz: **os quatro provedores retornaram HTTP 200, sucesso e CNPJ correspondente**. Uma tentativa TLS inicial sem HTTP registrada separadamente; quatro GETs com resposta, cinco tentativas no handler. Sem incompatibilidade de adaptador comprovada. Alfanumérico real, sync e fallback real não autorizados/não executados; **não há mudança nas classificações alfanuméricas abaixo**. Homologação parcial não garante disponibilidade futura ou consistência fiscal para outros cadastros. H01 validada remotamente pelo mantenedor (Actions 45461414433, commit 9fcb8b893fc30b18371aff9faf44d153f1de0557), com revisão dos principais trechos. As limitações históricas da coleta documental T06 permanecem registradas.

Verificação documental T06: **2026-11-24**. Escopo: os quatro endpoints públicos usados pelo GetCNPJ. Acesso a documentação e código oficial não equivale a consulta cadastral. **Na T06 nenhuma consulta real foi executada ou autorizada; na H02 houve as quatro consultas numéricas descritas acima.** A suíte automatizada continua exclusivamente com JSON sintético e handlers locais; esses testes não comprovam disponibilidade operacional, atualização ou existência de cadastro.

## Matriz de acesso

| Serviço / site | GET usado pela biblioteca | Serviço público / autenticação | Limite documentado | CNPJ alfanumérico |
|---|---|---|---|---|
| [CNPJ.WS](https://www.cnpj.ws/) | `https://publica.cnpj.ws/cnpj/{cnpj}` | Gratuito, sem cadastro/token | 3 consultas/minuto | **Não confirmado**: referência ainda declara parâmetro integer; não há confirmação explícita consultada |
| [ReceitaWS](https://receitaws.com.br/) | `https://receitaws.com.br/v1/cnpj/{cnpj}` | Gratuito, sem token; somente dados em banco/cache | 3 consultas/minuto; 429 ao exceder, 504 quando exige resolução online | **Confirmado por documentação oficial**: OpenAPI descreve 12 caracteres alfanuméricos + 2 DVs e regex `[A-Z0-9]{12}[0-9]{2}` |
| [BrasilAPI](https://brasilapi.com.br/) | `https://brasilapi.com.br/api/cnpj/v1/{cnpj}` | Público gratuito, sem autenticação no contrato | Cota numérica não publicada nas fontes consultadas; respeitar termos e respostas 429 | **Confirmado por documentação oficial**: regex alfanumérica explícita no OpenAPI |
| [CNPJá](https://cnpja.com/) | `https://open.cnpja.com/office/{cnpj}` | Gratuito, sem cadastro/chave | 5 consultas/minuto por IP | **Não confirmado**: identificador string no SDK não comprova suporte alfanumérico |

Classificações possíveis: **Confirmado por documentação oficial**, **Confirmado por teste real autorizado**, **Não suportado** (exige evidência explícita) e **Não confirmado**. Nenhum dos quatro recebeu classificação por teste real; ausência de confirmação não significa comprovação de incompatibilidade.

O limite padrão local continua 3 aquisições/minuto por provedor, por instância de cliente. Ele é conservador para CNPJá, não representa uma cota oficial da BrasilAPI e não coordena clientes/processos que compartilhem o mesmo IP. Prioridades continuam CNPJWS → ReceitaWS → BrasilAPI → CNPJA. Não foram implementados limites/prioridades configuráveis por serviço.

## Matriz de dados documentados dos endpoints públicos

“Sim” indica campo/recurso documental, sujeito a ausência, null, defasagem e cobertura do cadastro. Não indica garantia de preenchimento.

| Informação | CNPJ.WS | ReceitaWS | BrasilAPI | CNPJá Open |
|---|---|---|---|---|
| Razão social, fantasia, abertura, situação, natureza, porte, capital | Sim | Sim | Sim | Sim |
| Endereço, CEP, telefones, e-mail | Sim | Sim | Sim | Sim |
| CNAE principal/secundários e quadro societário | Sim | Sim | Sim | Sim |
| Código IBGE municipal | `estabelecimento.cidade.ibge_id` | Não documentado nesse retorno | `codigo_municipio_ibge`; `codigo_municipio` não é usado como IBGE | `address.municipality` |
| Simples Nacional / MEI | `simples.simples` / `simples.mei` | `simples` / `simei` | `opcao_pelo_simples` / `opcao_pelo_mei` | `company.simples` / `company.simei` |
| Inscrições estaduais | Lista em `estabelecimento.inscricoes_estaduais`, UF/situação/data | Não no endpoint usado; operação comercial separada para IE | Não documentadas no contrato consultado | Guia reserva IE à API comercial; não ofertadas pelo adaptador Open |
| Última atualização | `atualizado_em` | `ultima_atualizacao` | Sem campo equivalente no contrato usado | `updated` |
| Outros dados documentados não expostos integralmente | País, representante, motivo e situação especial | Metadados de uso/billing (free/database), motivo e situação especial | País/exterior, fax, motivos e situação especial | SUFRAMA; metadados/históricos/enriquecimentos variam conforme modalidade |

## Fontes e condições por serviço

### CNPJ.WS

Fontes: [consulta pública e schema](https://docs.cnpj.ws/referencia-de-api/api-publica/consultando-cnpj), [termos](https://www.cnpj.ws/termos-de-uso). JSON divide empresa e `estabelecimento`; IE é lista, não propriedade única. Termos distinguem acesso público e comercial e restringem comercialização/redistribuição de resultados, permitindo benefício próprio; não garantem exatidão ou atualização. Avaliar os termos aplicáveis à integração comercial pretendida. O contrato apresenta campos como string em alguns pontos onde exemplos usam null/objetos; essa inconsistência não autoriza presumir outro endpoint. Sem evidência de operação real nesta auditoria.

### ReceitaWS

Fontes: [documentação](https://developers.receitaws.com.br/), [OpenAPI oficial indicado pelo próprio site](https://developers.receitaws.com.br/receitaws.yaml), [termos](https://receitaws.com.br/terms.pdf). Schema `rfResultSuccess` é plano, com atividades/QSA e objetos fiscais. HTTP 200 também admite `resultInvalidCNPJ` e `rfResultRejected`, com `status: ERROR`. A operação comercial de IE e consultas com defasagem exigem Bearer token; não são chamadas pela biblioteca. Termos restringem revenda/comercialização dos resultados e não garantem atualidade. Confirmação alfanumérica é documental, sem comprovação de resposta real.

### BrasilAPI

Fontes: [OpenAPI CNPJ oficial](https://github.com/BrasilAPI/BrasilAPI/blob/main/pages/docs/doc/cnpj.json), [termos de uso no repositório oficial](https://github.com/BrasilAPI/BrasilAPI#termos-de-uso). Resposta plana, códigos cadastrais numéricos, descrições textuais e listas CNAE/QSA. Contrato prevê 400, 404 e 503; depende de fonte a montante. Serviço experimental, sem crawling/automação em massa ou sobrecarga; licença MIT do código não concede uso ilimitado do serviço. Não foi encontrada autorização comercial irrestrita específica nas fontes consultadas; avaliar termos para o uso pretendido.

### CNPJá

Fontes: [guia Open](https://cnpja.com/api/open), [referência Open](https://cnpja.com/api/open/reference), [SDK oficial — método público](https://github.com/cnpja/sdk-nodejs/blob/master/source/open/office/open.office.service.ts), [tipos do SDK](https://github.com/cnpja/sdk-nodejs/blob/master/source/cnpja/cnpja.dto.ts), [termos](https://cnpja.com/subscription/tos). O SDK tipa a consulta pública como `OfficeDto`; recursos comerciais opcionais desse tipo não se tornam públicos por isso. JSON contém `taxId`, `company`, `address`, `status`, atividades e contatos. Termos proíbem recriar serviço concorrente e automações que sobrecarreguem a infraestrutura; uso/tratamento dos dados é responsabilidade do consumidor. O plano gratuito com cadastro/créditos é distinto do Open sem autenticação.

Limitação de coleta: a referência Open depende de JavaScript; acesso direto à documentação retornou 429 durante a auditoria. Não foram repetidas consultas para contornar o limite. Guia, termos e SDK oficial complementaram a evidência de schema; isso não verifica o servidor em operação. O SDK é consultado como fonte, não adicionado como dependência.

## Decisões de mapeamento e compatibilidade

| Provedor | Incompatibilidade encontrada / correção |
|---|---|
| BrasilAPI | `situacao_cadastral`, `qualificacao_do_responsavel` e `codigo_qualificacao_representante_legal` são códigos numéricos, não strings. `porte` documentado é textual; mantém tolerância ao número legado via campo interno, usando `descricao_porte` como alternativa. Nomes atuais `descricao_identificador_matriz_filial` e `descricao_tipo_de_logradouro` passam a prevalecer, preservando aliases anteriores. E-mail e IBGE agora mapeados. Situação numérica conhecida pode suprir descrição ausente; código desconhecido continua desconhecido. |
| CNPJ.WS | Campos não expostos `qualificacao_representante` e `motivo_situacao_cadastral` toleram string/objeto/null diante da inconsistência entre schema/exemplos. IBGE municipal mapeado sem usar SIAFI. IE sem situação não é automaticamente inativa: nullable novo registra desconhecido. Strings Sim/Não distinguem opções conhecidas. |
| ReceitaWS | Bool interno nullable para não perder informação ausente; objeto SIMEI isolado deixa de ser descartado. Datas usam dd/MM/yyyy (e ISO já aceito), sem interpretação pela cultura da máquina. Erro explícito em HTTP 200 gera ProviderException em consulta direta; anteriormente `status: ERROR` retornava null. No cliente continua sendo falha com fallback quando cabível, agora com exceção de diagnóstico. |
| CNPJá | `company.id` atual é string, não int. Campo interno não exposto tolera ambas as representações sem converter o identificador solicitado. `head` ausente/null não gera FILIAL. IBGE municipal e primeiro e-mail preenchido mapeados; listas ignoram membros null. Opções fiscais desconhecidas preservadas nos campos nullable. |

Regras comuns:

- A base exige CNPJ retornado válido e igual ao solicitado após normalização, inclusive em subclasses externas que usam `CnpjProviderBase`. A string de apresentação existente é preservada. Divergência/identidade inválida gera ProviderException; não se trata de entrada inválida do consumidor. Dados parciais com identidade válida ainda são aceitos; sucesso não certifica completude cadastral.
- JSON literal null ou CNPJWS sem estabelecimento continuam sem dados; `{}` nunca é consulta bem-sucedida. Erros explícitos em `status`, `error` ou `errors` são rejeitados mesmo com identificador válido. Não se interpreta `status` objeto do CNPJá como erro. JSON/tipos inválidos permanecem falhas de desserialização; campos desconhecidos continuam tolerados.
- Listas null/ausentes viram listas vazias existentes; membros null são ignorados. IE sem número é ignorada. **Lista de IE vazia significa informação indisponível ou sem itens, nunca prova de não contribuinte.**
- `Endereco.CodigoIbge` é adição nullable, com dígitos em string. Não deriva código de nome do município. `SimplesNacional.OptanteInformado` / `OptanteSimeiInformado` e `InscricaoEstadual.AtivoInformado` são adições bool nullable: null desconhecido, false negativo informado, true positivo informado. Os bools antigos preservam false para desconhecido; `ToString()` legado da IE continua usando `Ativo`. Os campos são propriedades mutáveis independentes, não um mecanismo de sincronização de alterações feitas pelo consumidor.
- Adições de propriedades podem aparecer na serialização dos modelos pelo consumidor; nenhum tipo ou membro público anterior foi removido/substituído. Não houve padronização geral de maiúsculas, descrições, máscara da ReceitaWS ou formatos públicos existentes.
- CNAEs numéricos/subclasse mantêm sete dígitos, inclusive zeros iniciais, e a apresentação histórica `01.11-3-01`. Essa máscara legada não é promessa de formato oficial CNAE. ReceitaWS preserva o texto recebido. CEP/telefones permanecem strings; zeros não são convertidos em números. Não se inventa telefone/IBGE ausente.
- Capital social usa decimal; nas fontes textuais usa ponto decimal invariante, sem aceitar vírgula como separador de milhar e inflar valores. Texto monetário malformado fica desconhecido (null). Datas inválidas/ausentes ficam null; campos de data usam formatos documentados sem depender da cultura local. Datas tipadas inválidas no JSON do CNPJá continuam erro de desserialização.
- Fontes oferecem campos além do modelo comum (representantes, motivos, históricos, país, SUFRAMA, fax etc.). Eles não foram todos acrescentados à API pública nesta tarefa; a matriz diferencia disponibilidade documental de efetivo mapeamento. Sem inferências fiscais novas, cache, retries ou consulta a operações comerciais.

## Evidência de testes

`ProviderContractTests` usa somente conteúdo sintético: respostas representativas completas, parciais/nulas, códigos/tipos documentados, campos ausentes, listas com null, datas e capital inválidos, erros no JSON, identidade divergente/ inválida e fallback. `HttpResourceDisposalTests` mantém cobertura HTTP 400/404/429/500/503 e descarte. Os quatro antigos testes que esperavam NullReferenceException para `[null]` foram ajustados para verificar a correção (lista vazia e descarte), preservando os 281 casos anteriores. Fontes, datas e limitações acima são evidência documental; a execução dos testes comprova somente os adaptadores locais.
