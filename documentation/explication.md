# Explicacao simples sobre DDD tatico no projeto

Este arquivo explica, de forma mais simples, os termos usados no README e como eles aparecem no codigo.

O foco e responder:

- o que e aggregate;
- o que e aggregate root;
- onde estao os aggregate roots;
- o que sao entity, value object, enum, factory, service e repository;
- como as camadas `Domain`, `Application` e `Infra` conversam.

---

## 1. Ideia principal

DDD significa Domain-Driven Design.

Em portugues simples: e uma forma de organizar o codigo usando as palavras e regras do negocio.

Neste projeto, o negocio e uma operadora de saude. Por isso o codigo usa nomes como:

- `Beneficiary`
- `Plan`
- `ProcedureCatalogItem`
- `AuthorizationRequest`
- `RequestedItem`
- `HospitalBill`
- `BillItem`
- `Glosa`
- `AdministrativeAppeal`

Esses nomes nao sao apenas "classes". Eles representam coisas reais do dominio.

---

## 2. Como o Domain esta organizado

O projeto `Domain` guarda as regras de negocio.

Hoje ele esta separado por tipo:

```text
Domain/
  Aggregates/
  ValueObjects/
  Enums/
  Factories/
  Services/
  Repositories/
```

Cada pasta ainda pode ter subpastas por assunto de negocio, como:

```text
Autorizacoes
Beneficiarios
Planos
Procedimentos
Faturamento
Auditoria
```

Exemplo:

```text
Domain/Aggregates/Autorizacoes/AuthorizationRequest.cs
Domain/ValueObjects/Planos/PlanNumber.cs
Domain/Factories/Autorizacoes/AuthorizationRequestFactory.cs
Domain/Repositories/Autorizacoes/IAuthorizationRepository.cs
```

---

## 3. Glossario simples

### Domain

`Domain` e onde ficam as regras importantes do negocio.

Exemplo:

- uma autorizacao precisa ter item;
- uma autorizacao pendente pode ser aprovada;
- uma glosa precisa ter justificativa;
- uma glosa so pode ter um recurso ativo.

Essas regras nao devem ficar na tela, no banco de dados ou no console. Elas ficam no dominio.

### Entity

Entity e uma classe que tem identidade.

Ou seja: ela representa uma coisa especifica no sistema.

Normalmente aparece com `Id`.

Exemplos:

- `AuthorizationRequest`
- `RequestedItem`
- `Beneficiary`
- `Plan`
- `HospitalBill`
- `BillItem`
- `Glosa`
- `AdministrativeAppeal`

Exemplo simples:

```csharp
public Guid Id { get; private set; }
```

Se os dados mudam, o objeto continua sendo o mesmo porque o `Id` continua sendo o mesmo.

### Value Object

Value Object e uma classe que representa um valor.

Ela nao precisa de `Id`.

O que importa e o valor que ela carrega.

Exemplos:

- `PlanNumber`
- `ProcedureCode`
- `CidCode`
- `ProfessionalRegistry`
- `Evidence`

Por que usar value object?

Porque ele evita espalhar `string` solta pelo sistema.

Em vez de passar qualquer texto como numero do plano, o codigo usa:

```text
PlanNumber
```

Isso deixa o dominio mais claro e permite validar esse valor em um lugar so.

### Enum

Enum representa uma lista fechada de opcoes.

Exemplos:

- `AuthorizationStatus`
- `BeneficiaryStatus`
- `PlanType`
- `ProcedureType`
- `GlosaReason`
- `AppealStatus`

Exemplo de uso:

```text
AuthorizationStatus.Pendente
AuthorizationStatus.Negada
AuthorizationStatus.AprovadaIntegralmente
```

Isso evita usar texto solto como `"pendente"` ou `"negada"` em varias partes do codigo.

---

## 4. O que e aggregate

Aggregate e um grupo de objetos que precisa ficar consistente junto.

Pense assim:

> Se uma regra mexe em mais de um objeto ao mesmo tempo, esses objetos provavelmente pertencem ao mesmo aggregate.

Exemplo:

```text
AuthorizationRequest
  RequestedItem
  RequestedItem
```

Uma autorizacao tem itens.

Quando aprova parcialmente uma autorizacao, o sistema precisa validar:

- se a autorizacao ainda esta pendente;
- se os itens informados pertencem a essa autorizacao;
- se a quantidade aprovada nao passa da quantidade solicitada;
- se pelo menos alguma quantidade foi aprovada;
- se o status final da autorizacao deve mudar.

Essa regra nao deve ficar espalhada.

Ela fica dentro de `AuthorizationRequest`.

Por isso `AuthorizationRequest` e um aggregate.

---

## 5. O que e aggregate root

Aggregate Root e a classe principal do aggregate.

Ela e a porta de entrada para alterar o aggregate.

Regra pratica:

> Quem esta fora do aggregate deve chamar a root, nao alterar os objetos internos diretamente.

Exemplo:

```text
AuthorizationRequest
  RequestedItem
```

O codigo de fora nao deveria aprovar um `RequestedItem` diretamente.

O correto e chamar:

```text
AuthorizationRequest.ApproveFully()
AuthorizationRequest.ApprovePartially(...)
AuthorizationRequest.Deny(...)
```

Assim, `AuthorizationRequest` garante que o status e os itens fiquem corretos ao mesmo tempo.

---

## 6. Onde estao os aggregate roots no projeto

Os aggregate roots ficam em:

```text
Domain/Aggregates/
```

### Principal aggregate root

```text
Domain/Aggregates/Autorizacoes/AuthorizationRequest.cs
```

`AuthorizationRequest` e a principal root do projeto.

Ela controla:

- status da autorizacao;
- itens solicitados;
- aprovacao integral;
- aprovacao parcial;
- negativa;
- pendencia documental;
- urgencia/emergencia;
- auditoria posterior.

Ela contem:

```text
RequestedItem
```

### Outros aggregate roots

```text
Domain/Aggregates/Faturamento/HospitalBill.cs
```

`HospitalBill` representa uma conta hospitalar e contem `BillItem`.

```text
Domain/Aggregates/Auditoria/Glosa.cs
```

`Glosa` controla o recurso administrativo (`AdministrativeAppeal`).

```text
Domain/Aggregates/Beneficiarios/Beneficiary.cs
Domain/Aggregates/Planos/Plan.cs
Domain/Aggregates/Procedimentos/ProcedureCatalogItem.cs
```

Esses sao aggregates simples de referencia.

Eles tem regras proprias, mas nao possuem uma estrutura interna tao grande quanto `AuthorizationRequest`.

### Observacao sobre BillItem

`BillItem` esta em:

```text
Domain/Aggregates/Faturamento/BillItem.cs
```

Ele concentra regras de glosa:

```text
ApplyGlosa(...)
ApplyClawbackAuditGlosa(...)
```

Mas, no modelo atual, ele tambem pode ser entendido como uma entity interna de `HospitalBill`, porque nao existe um repository proprio para `BillItem`.

Em DDD mais rigoroso:

- `HospitalBill` seria a root;
- `BillItem` seria uma entity interna;
- `Glosa` poderia ser root se tiver ciclo de vida proprio.

---

## 7. Aggregates do projeto explicados

### AuthorizationRequest + RequestedItem

Estrutura:

```text
AuthorizationRequest
  RequestedItem
  RequestedItem
```

`AuthorizationRequest` representa a solicitacao de autorizacao.

`RequestedItem` representa cada item solicitado.

Regras protegidas por `AuthorizationRequest`:

- autorizacao precisa ter pelo menos um item;
- so autorizacao pendente recebe decisao;
- aprovacao integral aprova todos os itens;
- aprovacao parcial valida item e quantidade;
- negativa precisa de justificativa;
- urgencia/emergencia aprova como excecao e marca auditoria posterior.

### HospitalBill + BillItem

Estrutura:

```text
HospitalBill
  BillItem
  BillItem
```

`HospitalBill` representa a conta hospitalar.

`BillItem` representa cada item cobrado.

Regras atuais:

- conta precisa ter id valido;
- beneficiario precisa ter id valido;
- estabelecimento nao pode ser vazio;
- item adicionado nao pode ser nulo.

### BillItem + Glosa

Estrutura:

```text
BillItem
  Glosa
  Glosa
```

Glosa e uma negativa, desconto ou contestacao sobre um item faturado.

Exemplo:

> O hospital cobrou um item, mas a operadora entende que ele nao deve ser pago por falta de documento, regra contratual ou auditoria.

`BillItem` cria glosas por estes metodos:

```text
ApplyGlosa(...)
ApplyClawbackAuditGlosa(...)
```

### Glosa + AdministrativeAppeal

Estrutura:

```text
Glosa
  AdministrativeAppeal
```

`AdministrativeAppeal` e o recurso contra uma glosa.

Regra principal:

- uma glosa so pode ter um recurso administrativo ativo.

Por isso o recurso e criado por:

```text
Glosa.FileAppeal(...)
```

E nao diretamente por fora.

### Beneficiary, Plan e ProcedureCatalogItem

Esses sao aggregates simples.

`Beneficiary` cuida de:

- nome;
- data de nascimento;
- plano vinculado;
- status ativo/inativo;
- calculo de idade.

`Plan` cuida de:

- numero do plano;
- tipo do plano;
- coparticipacao;
- carencia por tipo de procedimento.

`ProcedureCatalogItem` cuida de:

- codigo do procedimento;
- descricao;
- tipo;
- idade minima e maxima.

---

## 8. O que e factory

Factory e uma classe que cria um objeto quando a criacao tem regra ou muitos detalhes.

No projeto:

```text
Domain/Factories/Autorizacoes/AuthorizationRequestFactory.cs
```

Ela cria uma `AuthorizationRequest`.

Ela recebe:

- beneficiario;
- numero do plano;
- codigo do procedimento;
- CID;
- profissional solicitante;
- estabelecimento;
- data;
- itens;
- indicador de urgencia/emergencia.

Ela tambem aplica a regra:

```text
se for urgencia/emergencia, aprova como excecao e marca auditoria posterior
```

Sem factory, cada parte do sistema poderia criar uma autorizacao de um jeito diferente.

---

## 9. O que e domain service

Domain Service e uma classe de regra de negocio que nao pertence bem a uma unica entity.

No projeto:

```text
Domain/Services/Autorizacoes/EligibilityService.cs
```

`EligibilityService` valida elegibilidade.

Ele precisa olhar ao mesmo tempo para:

- `Beneficiary`
- `Plan`
- `ProcedureCatalogItem`
- data da solicitacao

Ele valida:

- beneficiario ativo;
- beneficiario pertence ao plano;
- carencia cumprida;
- idade permitida para o procedimento.

Essa regra cruza varios objetos. Por isso fica em um domain service.

---

## 10. O que e repository

Repository e uma porta para buscar e salvar aggregates.

No dominio ficam os contratos:

```text
Domain/Repositories/Autorizacoes/IAuthorizationRepository.cs
Domain/Repositories/Faturamento/IHospitalBillRepository.cs
```

Eles dizem o que o dominio precisa:

- buscar por id;
- adicionar;
- atualizar quando o fluxo precisa mudar um aggregate ja salvo.

Eles nao dizem como salvar.

A implementacao real fica em `Infra`:

```text
Infra/Repositories/AuthorizationRepository.cs
Infra/Repositories/HospitalBillRepository.cs
```

Hoje elas salvam em SQLite. A UI usa, por padrao, o arquivo `health-insurance.db`.

No futuro poderia trocar SQLite por outro banco, e o dominio nao precisaria mudar.

---

## 11. O que e use case

Use Case e uma acao que o sistema executa.

No projeto, os use cases ficam em:

```text
Application/UseCases/Autorizacoes/
```

Exemplos:

```text
RequestAuthorizationUseCase
ApproveAuthorizationUseCase
ApproveAuthorizationPartiallyUseCase
DenyAuthorizationUseCase
RegisterDocumentPendingUseCase
GetAuthorizationStatusUseCase
```

O use case coordena o fluxo:

1. recebe dados;
2. busca aggregate no repository;
3. chama metodo do dominio;
4. salva alteracao;
5. devolve resposta.

O use case nao deve concentrar regra de negocio.

Quem decide se pode aprovar, negar ou mudar status e o dominio.

---

## 12. O que e application service

Application Service e uma fachada da camada de aplicacao.

No projeto:

```text
Application/Services/AuthorizationService.cs
```

Ele existe para oferecer uma API simples para a UI e os testes.

Ele delega o trabalho para os use cases.

Exemplo:

```text
AuthorizationService.ApproveAuthorizationAsync(...)
  chama ApproveAuthorizationUseCase
    busca AuthorizationRequest
    chama AuthorizationRequest.ApproveFully()
    salva no repository
```

---

## 13. Como as camadas conversam

Fluxo de criar autorizacao:

```text
UI
  chama AuthorizationService

AuthorizationService
  chama RequestAuthorizationUseCase

RequestAuthorizationUseCase
  recebe AuthorizationRequestDto
  cria value objects
  cria RequestedItem
  chama AuthorizationRequestFactory
  salva pelo IAuthorizationRepository

AuthorizationRequest
  protege as regras de negocio

Infra
  salva no SQLite com AuthorizationRepository
```

Fluxo de aprovar autorizacao:

```text
UI
  chama AuthorizationService.ApproveAuthorizationAsync

ApproveAuthorizationUseCase
  busca AuthorizationRequest
  chama AuthorizationRequest.ApproveFully()
  salva no repository

AuthorizationRequest
  valida se esta pendente
  aprova todos os itens
  muda status para AprovadaIntegralmente
```

Fluxo de criar conta hospitalar:

```text
UI
  chama BillingService.CreateHospitalBillFromAuthorizationAsync

CreateHospitalBillFromAuthorizationUseCase
  busca AuthorizationRequest pelo IAuthorizationRepository
  valida se a autorizacao esta aprovada
  cria HospitalBill com os itens aprovados
  salva pelo IHospitalBillRepository

Infra
  salva no SQLite com HospitalBillRepository
```

---

## 14. Tabela rapida

| Termo | Explicacao simples | Exemplo |
|-------|--------------------|---------|
| Domain | Onde ficam as regras de negocio | `Domain/` |
| Entity | Objeto com identidade | `AuthorizationRequest` |
| Value Object | Objeto definido pelo valor | `PlanNumber` |
| Enum | Lista fechada de opcoes | `AuthorizationStatus` |
| Aggregate | Grupo que precisa ficar consistente junto | `AuthorizationRequest` + `RequestedItem` |
| Aggregate Root | Porta de entrada do aggregate | `AuthorizationRequest` |
| Factory | Cria objeto complexo corretamente | `AuthorizationRequestFactory` |
| Domain Service | Regra que cruza varios objetos | `EligibilityService` |
| Use Case | Acao do sistema | `RequestAuthorizationUseCase` |
| Application Service | Fachada da aplicacao | `AuthorizationService`, `BillingService` |
| Repository | Busca e salva aggregates | `IAuthorizationRepository`, `IHospitalBillRepository` |
| Infra | Implementa detalhe tecnico | `AuthorizationRepository`, `HospitalBillRepository` |

---

## 15. Resumo final

O ponto mais importante do projeto e este:

```text
AuthorizationRequest e a principal aggregate root.
```

Ela centraliza o fluxo de autorizacao.

Isso evita regra espalhada pelo sistema.

Quando alguem quer aprovar, negar ou marcar pendencia, deve passar por `AuthorizationRequest`.

Assim o dominio continua consistente.
