# Explicacoes sobre DDD tatico no projeto

Este arquivo complementa o README explicando melhor os pontos 4 e 7 da atividade:

- quais aggregates foram definidos;
- o que e um aggregate;
- o que significam os termos tecnicos usados;
- onde entram factories, services, repositories, application services e use cases.

O objetivo aqui nao e trocar a implementacao, mas deixar claro como ler o modelo atual.

---

## 1. Ideia central do DDD tatico

DDD significa Domain-Driven Design. A ideia principal e modelar o software a partir das regras e palavras do negocio.

Neste projeto, o negocio e uma operadora de saude. Por isso, o codigo usa conceitos como:

- beneficiario;
- plano;
- procedimento;
- autorizacao;
- item solicitado;
- conta hospitalar;
- glosa;
- recurso administrativo.

Em vez de criar apenas classes genericas como `Cadastro`, `Processador` ou `Registro`, o modelo tenta representar objetos que fazem sentido para quem entende o dominio da saude suplementar.

---

## 2. Glossario dos termos tecnicos

### Domain

`Domain` e o projeto onde ficam as regras de negocio.

Neste repositorio, ele esta em:

```text
Domain/
```

Exemplos:

- `AuthorizationRequest` sabe aprovar, negar e marcar pendencia de uma solicitacao.
- `RequestedItem` sabe controlar quantidade solicitada e aprovada.
- `EligibilityService` sabe validar elegibilidade cruzando beneficiario, plano e procedimento.

O dominio nao deveria depender de tela, banco de dados, API HTTP ou console. Ele representa a regra principal do sistema.

### Module

Modulo e uma divisao do dominio por assunto de negocio.

Neste projeto, os modulos aparecem como subpastas dentro das pastas por tipo:

```text
Domain/Aggregates/
Domain/ValueObjects/
Domain/Enums/
Domain/Factories/
Domain/Services/
Domain/Repositories/
```

Exemplos:

- `Autorizacoes`
- `Beneficiarios`
- `Planos`
- `Procedimentos`
- `Faturamento`
- `Auditoria`

Essa divisao evita misturar regras de autorizacao com regras de faturamento, auditoria ou plano.

### Entity

Entity e um objeto com identidade propria.

Mesmo que seus dados mudem, ele continua sendo o mesmo objeto porque tem um `Id` ou alguma identidade equivalente.

Exemplo:

```csharp
public Guid Id { get; private set; }
```

No projeto:

- `AuthorizationRequest` e uma entity porque possui `Id` e ciclo de vida proprio.
- `Beneficiary` e uma entity porque representa um beneficiario especifico.
- `HospitalBill` e uma entity porque representa uma conta hospitalar especifica.
- `Glosa` e uma entity porque representa uma glosa especifica aplicada a um item faturado.

### Value Object

Value Object e um objeto que nao e definido por identidade, mas pelo valor que carrega.

Dois value objects com o mesmo valor representam a mesma coisa conceitualmente.

Exemplos no projeto:

- `PlanNumber`
- `ProcedureCode`
- `CidCode`
- `ProfessionalRegistry`
- `Evidence`

Um `PlanNumber` serve para proteger o conceito "numero do plano". Em vez de espalhar `string` por todo o codigo, o sistema usa um tipo proprio que pode validar esse valor.

### Invariant

Invariant e uma regra que nunca pode ser quebrada dentro do modelo.

Exemplos:

- uma autorizacao precisa ter pelo menos um item;
- um item solicitado precisa ter quantidade maior que zero;
- uma solicitacao negada precisa ter justificativa;
- apenas solicitacoes pendentes podem receber decisao;
- uma glosa so pode ter um recurso administrativo ativo.

Aggregates existem principalmente para proteger invariants.

### Aggregate

Aggregate é um conjunto de objetos do domínio que deve ser tratado como uma única unidade de consistência.

Em termos práticos, um aggregate garante que regras que envolvem vários objetos sejam aplicadas de forma atômica: ninguém pode alterar uma parte do conjunto deixando o todo em estado inválido.

Características importantes:
- Tem uma raiz (Aggregate Root) que é o único ponto de acesso externo ao aggregate.
- Contém entidades e value objects internos que só podem ser alterados pela raiz.
- Define métodos que aplicam e protegem as invariantes do conjunto.

Exemplo simples:

```text
AuthorizationRequest (Aggregate Root)
  ├─ RequestedItem (entidade interna)
  └─ RequestedItem (entidade interna)
```

Se uma regra precisa verificar ou alterar vários itens (por exemplo, aprovar parcialmente), essa lógica deve ficar dentro de AuthorizationRequest, garantindo consistência.

Em outras palavras: quando uma regra envolve varios objetos ao mesmo tempo, eles devem ficar dentro do mesmo aggregate para que ninguem altere uma parte e deixe o conjunto em estado invalido.

Um aggregate geralmente possui:

- uma raiz, chamada Aggregate Root;
- entidades internas;
- value objects;
- metodos que protegem as regras do conjunto.

Exemplo simples:

```text
AuthorizationRequest
  RequestedItem
  RequestedItem
```

A autorizacao e a unidade principal. Os itens existem dentro dela. A aprovacao parcial precisa olhar para os itens, validar se eles pertencem a solicitacao e conferir as quantidades.

Por isso, a regra fica em `AuthorizationRequest.ApprovePartially(...)`, e nao espalhada fora do dominio.

### Aggregate Root

Aggregate Root e a entidade principal do aggregate.

Ela funciona como a porta de entrada para alterar o conjunto.

Regra pratica: codigo de fora do aggregate deve chamar metodos da raiz, nao sair alterando entidades internas diretamente.

No projeto, `AuthorizationRequest` e a principal Aggregate Root porque controla:

- status da autorizacao;
- lista de `RequestedItem`;
- aprovacao integral;
- aprovacao parcial;
- negativa;
- pendencia documental;
- excecao de urgencia/emergencia;
- auditoria posterior.

Isso impede que outra camada aprove um `RequestedItem` diretamente e esqueca de atualizar o status da autorizacao.

### Consistency Boundary

Consistency Boundary significa "limite de consistencia".

E a fronteira dentro da qual as regras precisam estar corretas ao mesmo tempo.

Exemplo:

Ao aprovar parcialmente uma autorizacao, o sistema precisa garantir ao mesmo tempo que:

- a autorizacao esta pendente;
- os itens informados pertencem aquela autorizacao;
- nenhuma quantidade aprovada passa da quantidade solicitada;
- pelo menos alguma quantidade foi aprovada;
- itens nao aprovados ficam com quantidade aprovada igual a zero;
- o status final vira `AprovadaParcialmente`.

Tudo isso acontece dentro do boundary de `AuthorizationRequest`.

### Use Case

Use Case representa uma acao que o sistema oferece para o usuario ou para outro sistema.

Exemplos no projeto:

- solicitar autorizacao;
- aprovar autorizacao;
- aprovar autorizacao parcialmente;
- negar autorizacao;
- registrar pendencia documental;
- consultar status da autorizacao.

Depois da separacao feita no projeto, esses casos ficam em:

```text
Application/UseCases/Autorizacoes/
```

O use case coordena o fluxo, mas nao deve concentrar regra de negocio. Ele chama o dominio para executar as regras.

### DTO

DTO significa Data Transfer Object.

Ele e um objeto simples para transportar dados entre camadas.

Exemplo:

```text
AuthorizationRequestDto
```

Esse DTO recebe dados simples como `PlanNumber`, `ProcedureCode`, `ClinicalJustification` e `MaterialsAndMedicines`. Depois, o use case converte esses dados para objetos do dominio, como `PlanNumber`, `ProcedureCode`, `CidCode`, `ProfessionalRegistry` e `RequestedItem`.

### Factory

Factory e uma classe ou metodo responsavel por criar objetos quando a criacao tem alguma complexidade.

Ela evita espalhar `new` e montagem manual por varias partes do sistema.

No projeto:

```text
AuthorizationRequestFactory
```

Ela cria uma `AuthorizationRequest` completa e aplica a regra inicial de urgencia/emergencia quando necessario.

### Domain Service

Domain Service e um servico do dominio usado quando uma regra de negocio nao pertence naturalmente a uma unica entity ou aggregate.

No projeto:

```text
EligibilityService
```

Ele valida elegibilidade cruzando:

- `Beneficiary`
- `Plan`
- `ProcedureCatalogItem`

Essa regra nao fica bem dentro de apenas uma dessas classes, porque depende das tres ao mesmo tempo.

### Application Service

Application Service coordena a execucao de casos de uso na camada de aplicacao.

Ele pode chamar use cases, repositories e objetos de dominio, mas nao deveria ser o lugar onde as regras principais do negocio vivem.

No projeto:

```text
AuthorizationService
```

Ele ficou como uma fachada para manter uma API simples para UI e testes, delegando a execucao para os use cases em `Application/UseCases/Autorizacoes`.

### Repository

Repository e uma abstracao para buscar e salvar aggregates.

Ele permite que a regra de negocio fale "salve esta autorizacao" ou "busque esta autorizacao" sem saber se os dados estao em memoria, banco SQL, arquivo, API externa ou outro mecanismo.

No dominio ficam os contratos:

```text
IAuthorizationRepository
IHospitalBillRepository
```

Na infraestrutura fica a implementacao concreta:

```text
AuthorizationRepository
```

No projeto atual, `AuthorizationRepository` salva em memoria usando `ConcurrentDictionary`, porque e uma demonstracao.

### Infrastructure

Infrastructure e a camada tecnica.

Ela implementa detalhes como banco de dados, arquivos, mensageria, APIs externas e armazenamento.

Neste projeto:

```text
Infra/
```

O dominio define o que precisa (`IAuthorizationRepository`). A infraestrutura decide como isso sera feito (`AuthorizationRepository` em memoria).

---

## 3. Explicacao detalhada do ponto 4: Aggregates definidos

O README diz:

```text
No recorte implementado, os principais aggregates sao:

- AuthorizationRequest, contendo seus RequestedItem.
- HospitalBill, contendo seus BillItem.
- BillItem, concentrando as Glosa aplicadas ao item faturado.
- Glosa, controlando seu AdministrativeAppeal.
- Beneficiary, Plan e ProcedureCatalogItem aparecem como aggregates simples de referencia do dominio.
```

Abaixo esta a explicacao de cada item.

### 3.1 AuthorizationRequest contendo RequestedItem

`AuthorizationRequest` representa uma solicitacao de autorizacao de procedimento.

Ela contem seus `RequestedItem`, que representam os itens solicitados, como materiais, medicamentos ou itens de procedimento.

Estrutura conceitual:

```text
AuthorizationRequest
  RequestedItem
  RequestedItem
```

Por que isso e um aggregate?

Porque as regras da autorizacao dependem dos itens:

- uma autorizacao precisa ter pelo menos um item;
- a aprovacao integral aprova todos os itens;
- a aprovacao parcial valida os itens e suas quantidades;
- a negativa nega todos os itens;
- a urgencia/emergencia aprova os itens e marca auditoria posterior.

Por isso, `RequestedItem` nao deveria ser manipulado livremente por fora. O caminho correto e passar pela raiz `AuthorizationRequest`.

Exemplos de metodos da raiz:

```text
ApproveFully()
ApprovePartially(...)
Deny(...)
RegisterDocumentPending(...)
SetAsEmergencyException()
```

Esses metodos protegem o estado do conjunto.

### 3.2 HospitalBill contendo BillItem

`HospitalBill` representa uma conta hospitalar.

Ela contem itens faturados, os `BillItem`.

Estrutura conceitual:

```text
HospitalBill
  BillItem
  BillItem
```

Por que isso e um aggregate?

Porque uma conta hospitalar nao e apenas um numero solto. Ela agrupa itens que foram cobrados para um beneficiario e um estabelecimento executante.

No codigo atual, `HospitalBill` protege regras como:

- id da conta nao pode ser vazio;
- id do beneficiario nao pode ser vazio;
- estabelecimento executante nao pode ser vazio;
- item adicionado nao pode ser nulo.

No modelo atual, `HospitalBill` e uma raiz natural para salvar e carregar uma conta inteira.

### 3.3 BillItem concentrando Glosa

`BillItem` representa um item faturado dentro de uma conta hospitalar.

Ele tambem concentra as `Glosa` aplicadas ao item.

Estrutura conceitual:

```text
BillItem
  Glosa
  Glosa
```

O que e uma glosa?

Glosa e uma negativa, desconto ou contestacao aplicada sobre um item faturado. Em uma operadora de saude, uma glosa pode acontecer quando a operadora entende que aquele item nao deveria ser pago, foi cobrado incorretamente, nao possui documentacao suficiente ou nao esta conforme a regra contratual.

Por que `BillItem` concentra glosas?

Porque a glosa precisa estar ligada ao item cobrado. A regra nao e "glosar a conta inteira" necessariamente; muitas vezes a auditoria glosa um item especifico.

No codigo:

```text
ApplyGlosa(...)
ApplyClawbackAuditGlosa(...)
```

Esses metodos criam uma `Glosa` ligada ao `BillItem`.

Observacao de modelagem:

Em DDD mais estrito, se `BillItem` so e salvo e carregado junto com `HospitalBill`, ele pode ser visto como uma entity interna do aggregate `HospitalBill`. Neste README, ele aparece como aggregate porque tambem concentra uma pequena fronteira de regra sobre suas glosas. Se futuramente `BillItem` ganhar repository proprio ou ciclo de vida independente, ele ficaria mais claramente como Aggregate Root.

### 3.4 Glosa controlando AdministrativeAppeal

`Glosa` representa a glosa aplicada.

`AdministrativeAppeal` representa o recurso administrativo contra essa glosa.

Estrutura conceitual:

```text
Glosa
  AdministrativeAppeal
```

Por que isso e um aggregate?

Porque existe uma regra importante:

```text
uma glosa so pode ter um recurso administrativo ativo
```

Essa regra esta em `Glosa.FileAppeal(...)`.

Ou seja, a `Glosa` controla se um recurso pode ou nao ser criado. O recurso nao deveria ser aberto por fora sem perguntar para a glosa, porque isso poderia quebrar a regra de "apenas um recurso por glosa".

O `AdministrativeAppeal` tambem possui regras:

- precisa ter pelo menos uma evidencia;
- so recurso em analise pode ser processado;
- pode terminar como glosa mantida;
- pode terminar como glosa revertida.

### 3.5 Beneficiary, Plan e ProcedureCatalogItem como aggregates simples de referencia

Esses objetos aparecem como aggregates simples porque possuem identidade e regras proprias, mas no recorte atual nao possuem colecoes internas complexas.

#### Beneficiary

Representa o beneficiario.

Regras atuais:

- id nao pode ser vazio;
- nome nao pode ser vazio;
- data de nascimento nao pode ser futura;
- plano vinculado nao pode ser vazio;
- pode calcular idade;
- pode trocar plano;
- pode ser ativado ou suspenso.

#### Plan

Representa o plano contratado.

Regras atuais:

- id nao pode ser vazio;
- numero do plano nao pode ser vazio;
- coparticipacao precisa estar entre 0 e 100;
- permite definir carencia por tipo de procedimento;
- permite verificar se a carencia foi cumprida.

#### ProcedureCatalogItem

Representa um item do catalogo de procedimentos.

Regras atuais:

- codigo nao pode ser nulo;
- descricao nao pode ser vazia;
- idade minima nao pode ser maior que idade maxima;
- permite validar se uma idade e permitida para o procedimento.

Eles sao "de referencia" porque a autorizacao usa dados deles para decidir elegibilidade, mas nao controla o ciclo de vida deles.

---

## 4. Explicacao detalhada do ponto 7: Factories, Services e Repositories

O README diz:

```text
- Factory: AuthorizationRequestFactory, no modulo de Autorizacoes, cria uma solicitacao completa com value objects e itens solicitados.
- Domain Service: EligibilityService, no modulo de Autorizacoes, valida elegibilidade cruzando beneficiario, plano e procedimento.
- Application Service: AuthorizationService, no projeto Application, coordena os casos de uso e chama o dominio sem concentrar regra de negocio.
- Repositories: IAuthorizationRepository e IHospitalBillRepository sao contratos do dominio. AuthorizationRepository, no projeto Infra, e a implementacao em memoria usada na demonstracao.
```

Abaixo esta a explicacao de cada papel.

### 4.1 Factory: AuthorizationRequestFactory

Uma factory existe quando criar um objeto exige mais do que apenas chamar `new`.

No projeto, `AuthorizationRequestFactory` cria uma autorizacao com:

- novo id;
- beneficiario;
- numero do plano;
- codigo do procedimento;
- CID ou justificativa clinica;
- profissional solicitante;
- estabelecimento executante;
- data prevista;
- itens solicitados;
- informacao de urgencia/emergencia.

A factory tambem aplica uma decisao inicial:

```text
se for urgencia/emergencia, a solicitacao e aprovada como excecao e marcada para auditoria posterior
```

Isso deixa a criacao de `AuthorizationRequest` padronizada.

Sem factory, cada lugar do sistema poderia criar uma autorizacao de um jeito diferente, esquecendo algum campo ou alguma regra inicial.

### 4.2 Domain Service: EligibilityService

`EligibilityService` e um Domain Service porque a regra de elegibilidade depende de mais de um objeto de dominio.

Ele precisa olhar para:

- beneficiario;
- plano;
- procedimento;
- data da solicitacao.

As validacoes feitas por ele incluem:

- beneficiario precisa estar ativo;
- beneficiario precisa pertencer ao plano informado;
- carencia do plano precisa estar cumprida;
- idade do beneficiario precisa ser permitida para o procedimento.

Essa regra nao pertence perfeitamente a `Beneficiary`, nem somente a `Plan`, nem somente a `ProcedureCatalogItem`.

Por isso ela fica em um servico de dominio.

### 4.3 Application Service: AuthorizationService

`AuthorizationService` fica no projeto `Application`.

Ele nao deveria decidir as regras de negocio. O papel dele e coordenar a chamada dos casos de uso.

No estado atual, ele funciona como uma fachada para operacoes de autorizacao:

- solicitar autorizacao;
- aprovar autorizacao;
- aprovar parcialmente;
- negar;
- registrar pendencia documental;
- consultar status.

Ele delega a execucao para classes de use case em:

```text
Application/UseCases/Autorizacoes/
```

Isso separa melhor:

- caso de uso: fluxo da aplicacao;
- dominio: regra de negocio;
- infraestrutura: armazenamento.

### 4.4 Use Cases em Application/UseCases

Os use cases sao as acoes especificas da aplicacao.

Exemplos:

```text
RequestAuthorizationUseCase
ApproveAuthorizationUseCase
ApproveAuthorizationPartiallyUseCase
DenyAuthorizationUseCase
RegisterDocumentPendingUseCase
GetAuthorizationStatusUseCase
```

Eles fazem tarefas como:

- receber DTO;
- converter dados simples para objetos do dominio;
- buscar aggregate no repository;
- chamar metodo do aggregate;
- salvar alteracao no repository;
- montar DTO de resposta.

O ponto importante: o use case orquestra, mas quem decide se pode aprovar, negar ou mudar status e o dominio.

### 4.5 Repositories: IAuthorizationRepository e IHospitalBillRepository

Repository e o mecanismo de persistencia visto pelo dominio.

O dominio declara contratos:

```text
IAuthorizationRepository
IHospitalBillRepository
```

Esses contratos dizem quais operacoes sao necessarias:

- buscar por id;
- adicionar;
- atualizar.

Mas eles nao dizem como isso sera salvo.

Essa separacao e importante porque o dominio nao deve depender de banco de dados especifico.

Hoje:

```text
AuthorizationRepository
```

salva em memoria.

Amanha, poderia existir:

```text
SqlAuthorizationRepository
MongoAuthorizationRepository
ApiAuthorizationRepository
```

E o dominio continuaria praticamente igual.

---

## 5. Como as camadas conversam

Fluxo simplificado de uma solicitacao de autorizacao:

```text
UI
  chama Application Service

Application Service
  chama RequestAuthorizationUseCase

RequestAuthorizationUseCase
  recebe AuthorizationRequestDto
  cria Value Objects
  cria RequestedItem
  chama AuthorizationRequestFactory
  salva usando IAuthorizationRepository

AuthorizationRequestFactory / AuthorizationRequest
  aplicam regras do dominio

Infra
  implementa AuthorizationRepository em memoria
```

Fluxo simplificado de aprovacao:

```text
UI
  chama AuthorizationService.ApproveAuthorizationAsync

AuthorizationService
  delega para ApproveAuthorizationUseCase

ApproveAuthorizationUseCase
  busca AuthorizationRequest no repository
  chama AuthorizationRequest.ApproveFully()
  salva alteracao no repository

AuthorizationRequest
  valida se esta pendente
  aprova todos os RequestedItem
  muda status para AprovadaIntegralmente
```

---

## 6. Regra pratica para diferenciar os conceitos

Use esta regra mental:

| Conceito | Pergunta que responde | Exemplo no projeto |
|----------|------------------------|--------------------|
| Entity | Quem e este objeto especifico? | `AuthorizationRequest`, `Beneficiary` |
| Value Object | Que valor valido isto representa? | `PlanNumber`, `ProcedureCode` |
| Aggregate | Que grupo precisa ficar consistente junto? | `AuthorizationRequest` + `RequestedItem` |
| Aggregate Root | Por onde altero esse grupo? | `AuthorizationRequest` |
| Factory | Como crio esse objeto corretamente? | `AuthorizationRequestFactory` |
| Domain Service | Que regra de negocio cruza mais de um objeto? | `EligibilityService` |
| Use Case | Que acao a aplicacao executa? | `RequestAuthorizationUseCase` |
| Application Service | Quem coordena a entrada da aplicacao? | `AuthorizationService` |
| Repository | Como busco e salvo aggregates? | `IAuthorizationRepository` |
| Infrastructure | Qual detalhe tecnico executa persistencia? | `AuthorizationRepository` |

---

## 7. Resumo curto

Aggregate e uma fronteira de consistencia.

Aggregate Root e a entidade principal que protege essa fronteira.

Entity tem identidade.

Value Object tem valor.

Factory cria objetos complexos de forma padronizada.

Domain Service guarda regra de negocio que cruza mais de um objeto.

Application Service coordena operacoes da aplicacao.

Use Case representa uma acao concreta do sistema.

Repository abstrai busca e persistencia.

Infrastructure implementa detalhes tecnicos.

No projeto, a principal decisao foi deixar `AuthorizationRequest` como raiz do fluxo de autorizacao, porque ela controla status, itens, aprovacao, negativa, pendencia e urgencia/emergencia sem espalhar essas regras fora do dominio.
