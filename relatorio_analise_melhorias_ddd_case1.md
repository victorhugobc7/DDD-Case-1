# Relatório de Análise e Melhorias com DDD

## 1. Identificação do Relatório

**Disciplina:** Design de Melhoria de Software

**Projeto analisado:** Sistema de Gestão de Operadora de Saúde - `DDD-Case-1`

**Grupo analisado:** Mateus Souza Araujo, Victor Hugo Brito Coelho e José Emanuel Andrade Dourado

**Grupo avaliador:** A preencher

**Data:** 26/05/2026

**Base analisada:** checkout local em `/home/mateus/Documentos/Github/ddd/DDD-Case-1`, solução `HealthInsurance.slnx`.

## 2. Objetivo da Análise

Este relatório analisa como o projeto `DDD-Case-1` aplica conceitos de Domain-Driven Design, com foco em DDD Tático, nas duas primeiras partes do livro *Domain-Driven Design: Atacando as Complexidades no Coração do Software*, de Eric Evans.

A análise observa se o domínio de operadora de saúde está bem representado no código, se as regras de negócio ficam protegidas no modelo de domínio e se a arquitetura favorece manutenção e evolução.

Também são propostas melhorias inspiradas em *Supple Design*, buscando tornar o modelo mais expressivo, flexível, comunicativo e alinhado à linguagem do negócio.

## 3. Descrição Geral do Projeto Analisado

### 3.1. Resumo do case

O projeto modela um sistema de operadora de saúde com foco em autorização de procedimentos, elegibilidade de beneficiários, faturamento hospitalar básico, glosas e recursos administrativos.

O recorte implementado é um DDD tático v1. A solução atual não tenta implementar toda a regulação ANS/TISS, mas deixa essa evolução documentada. O código real concentra a parte executável nos fluxos de autorização de procedimento e criação de conta hospitalar a partir de uma autorização aprovada.

### 3.2. Principais funcionalidades identificadas

- Solicitar autorização de procedimento.
- Registrar materiais ou medicamentos solicitados.
- Aprovar autorização integralmente.
- Aprovar autorização parcialmente.
- Negar autorização com motivo de glosa e justificativa.
- Registrar pendência documental.
- Aprovar automaticamente urgência ou emergência e exigir auditoria posterior.
- Validar elegibilidade por beneficiário, plano, carência e idade permitida.
- Criar conta hospitalar a partir de autorização aprovada.
- Faturar apenas itens aprovados.
- Registrar glosas em itens de conta.
- Registrar recurso administrativo contra glosa.
- Persistir autorizações e contas hospitalares em SQLite.

### 3.3. Principais conceitos de domínio encontrados

| Conceito de domínio | Como aparece no sistema | Observação |
|---|---|---|
| Solicitação de autorização | `AuthorizationRequest` | Principal Aggregate Root do projeto. |
| Item solicitado | `RequestedItem` | Entidade interna da autorização. |
| Beneficiário | `Beneficiary` | Entidade com identidade, status, plano e cálculo de idade. |
| Plano | `Plan` | Entidade com tipo, coparticipação e regras de carência. |
| Procedimento | `ProcedureCatalogItem` | Item de catálogo identificado por `ProcedureCode`. |
| Número do plano | `PlanNumber` | Value Object. |
| Código de procedimento | `ProcedureCode` | Value Object. |
| CID | `CidCode` | Value Object com validação de formato. |
| Registro profissional | `ProfessionalRegistry` | Value Object. |
| Conta hospitalar | `HospitalBill` | Aggregate Root de faturamento. |
| Item da conta | `BillItem` | Entidade interna da conta hospitalar. |
| Glosa | `Glosa` | Entidade de auditoria ligada ao item faturado. |
| Recurso administrativo | `AdministrativeAppeal` | Entidade ligada a uma glosa. |
| Elegibilidade | `EligibilityService` | Domain Service para regra que cruza beneficiário, plano e procedimento. |

## 4. Análise da Arquitetura e Isolamento do Domínio

### 4.1. Domínio puro

**Pergunta principal:** a camada de domínio está independente de frameworks, banco de dados, APIs externas ou bibliotecas de infraestrutura?

**Análise**

A camada `Domain` está bem isolada. O projeto `Domain/Domain.csproj` não referencia `Application`, `Infra`, `UI` nem pacote de persistência. As entidades e Value Objects não possuem atributos de banco de dados, anotações de framework ou dependência de SQLite.

As regras principais aparecem em classes de domínio, como `AuthorizationRequest`, `RequestedItem`, `Plan`, `EligibilityService`, `BillItem` e `Glosa`.

**Evidências encontradas no código**

- Projeto analisado: `Domain/Domain.csproj`.
- Entidade analisada: `Domain/Aggregates/Autorizacoes/AuthorizationRequest.cs`.
- Value Objects analisados: `PlanNumber`, `ProcedureCode`, `CidCode`, `ProfessionalRegistry` e `Evidence`.
- Infraestrutura separada em: `Infra/Data/HealthInsuranceDatabase.cs`, `Infra/Repositories/AuthorizationRepository.cs` e `Infra/Repositories/HospitalBillRepository.cs`.
- Problema não encontrado: não há atributos de Entity Framework, SQL ou dependência de `Microsoft.Data.Sqlite` dentro do domínio.

**Avaliação:** atende completamente.

**Sugestão de melhoria**

Manter essa separação. Se o projeto evoluir para migrations, ORM, API REST ou mensageria, as configurações técnicas devem continuar fora do `Domain`, preferencialmente em `Infra`.

### 4.2. Inversão de dependência

**Pergunta principal:** as interfaces dos repositórios estão no domínio ou aplicação, enquanto as implementações estão na infraestrutura?

**Análise**

O projeto aplica corretamente inversão de dependência para persistência. As interfaces estão no domínio:

- `Domain/Repositories/Autorizacoes/IAuthorizationRepository.cs`.
- `Domain/Repositories/Faturamento/IHospitalBillRepository.cs`.

As implementações concretas ficam na infraestrutura:

- `Infra/Repositories/AuthorizationRepository.cs`.
- `Infra/Repositories/HospitalBillRepository.cs`.

Além disso, `Application` depende de `Domain`, e não de `Infra`. A UI é quem monta os repositórios concretos e injeta nos serviços de aplicação.

**Evidências**

| Item | Local atual | Avaliação |
|---|---|---|
| `IAuthorizationRepository` | `Domain/Repositories/Autorizacoes` | Bem posicionado. |
| `IHospitalBillRepository` | `Domain/Repositories/Faturamento` | Bem posicionado. |
| `AuthorizationRepository` | `Infra/Repositories` | Implementação concreta no lugar correto. |
| `HospitalBillRepository` | `Infra/Repositories` | Implementação concreta no lugar correto. |
| `Application/Application.csproj` | referencia apenas `Domain` | Mantém Application independente de Infra. |

**Avaliação:** atende completamente.

**Sugestão de melhoria**

Para uma versão maior, usar injeção de dependência formal na camada de entrada, em vez de instanciar os repositórios diretamente no `UI/Program.cs`. Isso não muda a arquitetura central, mas facilita testes e troca de infraestrutura.

### 4.3. Application Services magros

**Pergunta principal:** os serviços de aplicação apenas coordenam o fluxo ou concentram regras de negócio?

**Análise**

Os serviços de aplicação são relativamente magros. `AuthorizationService` e `BillingService` delegam para casos de uso específicos em `Application/UseCases`. Os casos de uso carregam aggregates, chamam métodos do domínio e salvam o resultado.

Exemplos positivos:

- `ApproveAuthorizationUseCase` carrega `AuthorizationRequest`, chama `ApproveFully()` e atualiza o repositório.
- `DenyAuthorizationUseCase` chama `authorization.Deny(...)`.
- `RegisterDocumentPendingUseCase` chama `authorization.RegisterDocumentPending(...)`.

O principal ponto de atenção é que `EligibilityService` existe no domínio e está testado, mas não aparece no fluxo principal de `RequestAuthorizationUseCase` ou `ApproveAuthorizationUseCase`. Assim, a regra de elegibilidade está modelada, mas não está garantida no caminho normal da aplicação.

Também existe uma regra de workflow em `CreateHospitalBillFromAuthorizationUseCase`: somente autorizações aprovadas podem gerar conta hospitalar. Essa regra é aceitável na aplicação porque cruza autorização e faturamento, mas poderia ficar mais expressiva com uma Factory ou método de criação de conta hospitalar a partir de autorização aprovada.

**Evidências**

| Classe | O que faz | Observação |
|---|---|---|
| `ApproveAuthorizationUseCase` | chama `authorization.ApproveFully()` | Coordenação adequada. |
| `DenyAuthorizationUseCase` | chama `authorization.Deny(...)` | Coordenação adequada. |
| `RequestAuthorizationUseCase` | cria Value Objects, itens e chama `AuthorizationRequestFactory` | Adequado, mas ainda não consulta elegibilidade. |
| `CreateHospitalBillFromAuthorizationUseCase` | verifica status aprovado e cria `HospitalBill` | Workflow entre aggregates, aceitável na Application. |
| `EligibilityService` | valida beneficiário, plano e procedimento | Não integrado ao fluxo principal. |

**Avaliação:** atende parcialmente.

**Sugestão de melhoria**

Integrar `EligibilityService` ao caso de uso correto. Para isso, o projeto precisaria de formas de carregar `Beneficiary`, `Plan` e `ProcedureCatalogItem`, provavelmente por repositórios ou consultas específicas. A aplicação continuaria coordenando, mas a decisão de elegibilidade permaneceria no domínio.

## 5. Análise da Modelagem de Objetos

### 5.1. Entidades expressivas

**Pergunta principal:** as entidades possuem comportamento de negócio ou são apenas classes com propriedades públicas?

**Análise**

O projeto evita um modelo totalmente anêmico. A classe mais forte é `AuthorizationRequest`, que protege status, itens, aprovação integral, aprovação parcial, negativa, pendência documental e urgência/emergência.

Outras classes também possuem comportamento:

- `RequestedItem` valida e controla quantidade aprovada.
- `Beneficiary` calcula idade, muda plano e altera status.
- `Plan` define e verifica carência.
- `ProcedureCatalogItem` verifica idade permitida.
- `BillItem` aplica glosa.
- `Glosa` registra recurso administrativo.
- `AdministrativeAppeal` controla decisão do recurso.

O ponto mais fraco é `HospitalBill`. Ela já protege a lista de itens com coleção somente leitura, mas ainda possui pouco comportamento de negócio além de `AddItem`. Como Aggregate Root de faturamento, poderia expressar mais regras: total da conta, status de faturamento, aplicação de glosa por item, impedimento de alterações após fechamento ou cancelamento.

**Evidências**

| Classe | Comportamentos observados | Avaliação |
|---|---|---|
| `AuthorizationRequest` | `ApproveFully`, `ApprovePartially`, `Deny`, `RegisterDocumentPending`, `SetAsEmergencyException` | Forte. |
| `RequestedItem` | `ApproveFully`, `ApprovePartially`, `Deny` | Boa entidade interna, mas seus métodos são públicos. |
| `Plan` | `SetGracePeriod`, `IsGracePeriodFulfilled` | Bom comportamento de domínio. |
| `BillItem` | `ApplyGlosa`, `ApplyClawbackAuditGlosa` | Bom comportamento local. |
| `HospitalBill` | `AddItem` | Ainda simples para uma root de faturamento. |

**Avaliação:** atende parcialmente.

**Sugestão de melhoria**

Enriquecer `HospitalBill` com comportamentos de faturamento e auditoria, por exemplo:

```csharp
public decimal TotalValue => _items.Sum(item => item.TotalValue);

public void ApplyGlosaToItem(Guid itemId, GlosaReason reason, string details)
{
    var item = _items.SingleOrDefault(x => x.Id == itemId)
        ?? throw new InvalidOperationException("Item de conta não encontrado.");

    item.ApplyGlosa(reason, details);
}
```

Assim, a conta hospitalar passa a controlar melhor sua própria fronteira.

### 5.2. Identidade clara

**Pergunta principal:** o grupo justificou corretamente o que deve ser Entity e o que deve ser Value Object?

**Análise**

A separação entre Entities e Value Objects está bem encaminhada. Objetos com identidade e ciclo de vida aparecem como classes com `Id`, enquanto conceitos definidos por valor aparecem como `record`.

O projeto também evita carregar aggregates inteiros dentro de `AuthorizationRequest`, usando `BeneficiaryId`, `PlanNumber`, `ProcedureCode`, `CidCode` e `ProfessionalRegistry`. Isso reduz acoplamento entre agregados.

O ponto que merece ajuste é a clareza sobre algumas fronteiras. `BillItem`, `Glosa` e `AdministrativeAppeal` têm identidade e comportamento, mas são manipulados a partir de `HospitalBill` ou `BillItem`. A documentação deve deixar claro se eles são entidades internas ou se algum deles poderá se tornar Aggregate Root em uma evolução futura.

**Evidências**

| Classe | Tipo observado | Observação |
|---|---|---|
| `AuthorizationRequest` | Entity / Aggregate Root | Identidade e ciclo de vida claros. |
| `RequestedItem` | Entity interna | Tem `Id`, mas deveria ser alterada pela root. |
| `PlanNumber` | Value Object | Não possui identidade própria. |
| `CidCode` | Value Object | Valida formato do CID. |
| `Glosa` | Entity interna ou Aggregate futuro | Precisa de fronteira melhor documentada se auditoria crescer. |

**Avaliação:** atende parcialmente.

**Sugestão de melhoria**

Adicionar uma seção curta na documentação explicando explicitamente:

- quais classes são Aggregate Roots;
- quais classes são entidades internas;
- quais classes não devem ter repository próprio;
- em quais situações `Glosa` poderia virar Aggregate Root independente.

### 5.3. Value Objects imutáveis

**Pergunta principal:** os Value Objects são imutáveis e protegem suas próprias invariantes?

**Análise**

Os Value Objects validam seus valores no construtor e foram modelados como `record`, o que é positivo. `CidCode`, por exemplo, valida o padrão de CID.

No entanto, as propriedades usam `init` público:

- `public string Value { get; init; }`
- `public string DocumentUrl { get; init; }`
- `public string Description { get; init; }`

Esse detalhe enfraquece a proteção das invariantes, porque o valor pode ser substituído por object initializer no momento da criação, potencialmente contornando a validação feita no construtor.

**Evidências**

| Classe | Problema observado |
|---|---|
| `PlanNumber` | `Value` tem `init` público. |
| `ProcedureCode` | `Value` tem `init` público. |
| `CidCode` | `Value` tem `init` público, apesar da validação por regex no construtor. |
| `ProfessionalRegistry` | `Value` tem `init` público. |
| `Evidence` | `DocumentUrl` e `Description` têm `init` público. |

**Avaliação:** atende parcialmente.

**Sugestão de melhoria**

Usar propriedades somente leitura ou `private init`, mantendo a validação concentrada no construtor.

```csharp
public sealed record CidCode
{
    public string Value { get; }

    public CidCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("O código CID não pode ser vazio.", nameof(value));

        if (!Regex.IsMatch(value, @"^[A-Z][0-9]{2}(\.[0-9])?$"))
            throw new ArgumentException("O código CID fornecido é inválido.", nameof(value));

        Value = value;
    }
}
```

### 5.4. Linguagem ubíqua

**Pergunta principal:** os nomes das classes, métodos e atributos refletem o vocabulário real do negócio?

**Análise**

O projeto usa muitos termos do domínio de saúde suplementar: autorização, beneficiário, plano, procedimento, CID, registro profissional, faturamento, glosa, auditoria e recurso administrativo.

Os métodos principais também revelam intenção: `ApproveFully`, `ApprovePartially`, `Deny`, `RegisterDocumentPending`, `SetAsEmergencyException`, `ApplyGlosa` e `FileAppeal`.

O principal problema é a mistura de idiomas. As pastas usam português (`Autorizacoes`, `Faturamento`, `Beneficiarios`, `Planos`), enquanto as classes e métodos usam inglês (`AuthorizationRequest`, `HospitalBill`, `RequestedItem`). Isso não impede o funcionamento, mas reduz a força da linguagem ubíqua.

Também há um nome que pode ficar mais preciso: `ClinicalJustification` no DTO é transformado em `CidCode`. Se o campo representa um CID, um nome como `ClinicalJustificationCid` ou `CidCode` seria mais direto.

**Evidências**

| Nome encontrado | Avaliação |
|---|---|
| `AuthorizationRequest` | Expressivo. |
| `Glosa` | Expressivo e alinhado ao domínio brasileiro. |
| `RegisterDocumentPending` | Expressivo. |
| `MaterialsAndMedicines` | Restritivo; o domínio usa também "item solicitado". |
| `ClinicalJustification` | Pode confundir texto clínico com código CID. |

**Avaliação:** atende parcialmente.

**Sugestão de melhoria**

Padronizar o idioma da linguagem ubíqua. Como o domínio é brasileiro e termos como glosa, carência, beneficiário e plano são naturais em português, uma opção seria migrar gradualmente nomes de domínio para português. Outra opção é manter classes em inglês, mas então traduzir também os conceitos de pasta e documentação técnica.

## 6. Análise de Aggregates e Fronteiras de Consistência

### 6.1. Fronteira de consistência

**Pergunta principal:** o Aggregate protege regras que precisam ser sempre verdadeiras?

**Análise**

`AuthorizationRequest` protege bem sua fronteira. Ela impede solicitação sem itens, aprovação repetida, aprovação parcial com item desconhecido, quantidade aprovada inválida e negativa sem justificativa.

`HospitalBill` protege menos regras. Ela recebe `BillItem`, mas ainda não expressa fechamento, cancelamento, total próprio, status de cobrança ou regra para impedir alteração depois de faturada. Isso é aceitável para o recorte v1, mas é um ponto claro de evolução.

**Evidências**

| Aggregate | Regra protegida | Situação |
|---|---|---|
| `AuthorizationRequest` | Somente solicitação pendente pode receber decisão. | Bem protegida por `EnsurePending()`. |
| `AuthorizationRequest` | Aprovação parcial deve informar itens válidos. | Bem protegida. |
| `AuthorizationRequest` | Negativa exige justificativa. | Bem protegida. |
| `HospitalBill` | Item não pode ser nulo. | Proteção mínima em `AddItem`. |
| `HospitalBill` | Total, fechamento e aplicação de glosa pela root. | Ainda não modelado. |

**Avaliação:** atende parcialmente.

**Sugestão de melhoria**

Tratar `HospitalBill` como uma Aggregate Root mais completa, adicionando comportamentos e invariantes de faturamento. Isso reduziria a chance de regras de faturamento ficarem espalhadas em DTOs, use cases ou UI.

### 6.2. Acesso via Aggregate Root

**Pergunta principal:** as alterações em objetos internos são feitas por meio da Aggregate Root?

**Análise**

O projeto usa coleções privadas e expõe `IReadOnlyCollection`, o que é uma boa prática. No entanto, as entidades internas ainda possuem métodos públicos.

Por exemplo, `AuthorizationRequest.Items` devolve `RequestedItem`, e `RequestedItem` possui métodos públicos como `ApproveFully`, `ApprovePartially` e `Deny`. Em um uso disciplinado, a aplicação chama a root. Mas tecnicamente outro código poderia obter um item e alterar sua quantidade aprovada diretamente, sem passar por `AuthorizationRequest`.

O mesmo raciocínio vale para `BillItem`: é possível acessar um item da conta e chamar `ApplyGlosa` diretamente se houver referência ao item.

**Evidências**

| Classe interna | Método público | Risco |
|---|---|---|
| `RequestedItem` | `ApproveFully`, `ApprovePartially`, `Deny` | Permite alteração fora de `AuthorizationRequest`. |
| `BillItem` | `ApplyGlosa`, `ApplyClawbackAuditGlosa` | Permite aplicar glosa sem passar por `HospitalBill`. |

**Avaliação:** atende parcialmente.

**Sugestão de melhoria**

Restringir métodos de entidades internas quando a regra exigir passagem pela root. Em C#, uma possibilidade é usar métodos `internal` nas entidades internas e expor métodos de negócio na Aggregate Root.

Exemplo:

```csharp
public void ApproveItemPartially(Guid itemId, int quantity)
{
    EnsurePending();

    var item = _items.SingleOrDefault(x => x.Id == itemId)
        ?? throw new InvalidOperationException("Item não pertence à solicitação.");

    item.ApprovePartially(quantity);
}
```

### 6.3. Repositórios por Aggregate Root

**Pergunta principal:** existe apenas um repositório por agregado?

**Análise**

O projeto está bem alinhado neste ponto. Existem repositórios apenas para os aggregates persistidos como unidade:

- `IAuthorizationRepository` para `AuthorizationRequest`.
- `IHospitalBillRepository` para `HospitalBill`.

Não há `RequestedItemRepository`, `BillItemRepository` ou `AdministrativeAppealRepository`, o que preserva a ideia de persistir o agregado pela root.

**Evidências**

| Repositório | Aggregate Root | Avaliação |
|---|---|---|
| `IAuthorizationRepository` | `AuthorizationRequest` | Adequado. |
| `IHospitalBillRepository` | `HospitalBill` | Adequado. |
| Repositórios para entidades internas | Não existem | Adequado para o recorte atual. |

**Avaliação:** atende completamente.

**Sugestão de melhoria**

Manter essa regra. Se `Glosa` ou `AdministrativeAppeal` ganharem ciclo de vida próprio no futuro, a equipe deve decidir explicitamente se eles continuam internos ao faturamento ou se viram Aggregate Roots.

## 7. Análise de Factories, Domain Services e Modules

### 7.1. Uso de Factories

**Pergunta principal:** a criação do objeto é complexa o suficiente para justificar uma Factory?

**Análise**

`AuthorizationRequestFactory` é adequada para o recorte atual. Ela gera o identificador da autorização, cria o aggregate e aplica a regra de urgência/emergência chamando `SetAsEmergencyException()`.

Essa Factory não é apenas um repasse simples para o construtor, porque existe uma regra de montagem: se a solicitação for urgente ou emergencial, ela já nasce aprovada integralmente e marcada para auditoria posterior.

**Avaliação:** uso adequado.

**Sugestão de melhoria**

Manter a Factory, mas evitar que ela cresça demais. Caso a criação passe a depender de elegibilidade, cobertura, carência e políticas externas, essas decisões devem ser separadas em serviços de domínio ou políticas específicas.

Também pode ser criada uma Factory de faturamento para deixar mais explícita a criação de `HospitalBill` a partir de uma autorização aprovada.

### 7.2. Domain Services

**Pergunta principal:** os Domain Services representam operações de domínio que não pertencem naturalmente a uma entidade específica?

**Análise**

`EligibilityService` é um bom exemplo de Domain Service. Ele valida uma regra que cruza `Beneficiary`, `Plan` e `ProcedureCatalogItem`, sem forçar essa responsabilidade para apenas uma entidade.

Ele verifica:

- beneficiário ativo;
- beneficiário pertencente ao plano informado;
- carência cumprida;
- idade permitida para o procedimento.

O ponto de melhoria é de integração. O serviço está testado, mas não é chamado pelo fluxo principal de autorização.

**Avaliação:** uso parcialmente adequado.

**Sugestão de melhoria**

Integrar `EligibilityService` no fluxo de solicitação ou aprovação. O ideal é que a aplicação carregue as entidades necessárias e delegue a regra ao serviço de domínio antes de permitir a autorização.

### 7.3. Modules

**Pergunta principal:** os módulos agrupam conceitos de domínio ou apenas camadas técnicas?

**Análise**

O projeto usa uma organização híbrida. A primeira divisão é técnica:

- `Aggregates`
- `ValueObjects`
- `Enums`
- `Factories`
- `Services`
- `Repositories`

Dentro dessas pastas, há subpastas de domínio, como `Autorizacoes`, `Faturamento`, `Planos`, `Procedimentos`, `Beneficiarios` e `Auditoria`.

Essa estrutura funciona para um projeto acadêmico pequeno, mas em um sistema maior a organização por tipo técnico pode espalhar um mesmo módulo de negócio por várias pastas.

**Avaliação:** atende parcialmente.

**Sugestão de melhoria**

Se o projeto crescer, considerar uma organização mais próxima dos módulos de negócio:

```text
Domain
  Autorizacoes
    AuthorizationRequest
    RequestedItem
    AuthorizationStatus
    AuthorizationRequestFactory
    EligibilityService
    IAuthorizationRepository
  Faturamento
    HospitalBill
    BillItem
    IHospitalBillRepository
  Auditoria
    Glosa
    AdministrativeAppeal
    Evidence
```

Essa mudança deixaria cada conceito de domínio mais coeso.

## 8. Documentação e Defesa Técnica

### 8.1. Justificativa de trade-offs

**Pergunta principal:** o grupo explicou por que escolheu uma solução em vez de outra?

**Análise**

A documentação existente em `documentacao_modelagem_ddd_saude.md` explica várias decisões de modelagem, como uso de Entities, Value Objects, Aggregates, Domain Services, Factories e Repositories. Também explicita trade-offs, como usar SQLite simples, manter ANS/TISS v2 como evolução futura e usar `AuthorizationRequest` como aggregate principal.

**Avaliação:** atende completamente.

**Sugestão de melhoria**

Adicionar uma pequena seção específica para decisões ainda ambíguas:

- por que `Glosa` ainda não possui repository próprio;
- por que `EligibilityService` não está integrado ao fluxo principal;
- por que os nomes misturam português e inglês;
- por que `MaterialsAndMedicines` representa todos os itens solicitados com quantidade fixa 1.

### 8.2. Rastreabilidade das regras de negócio

**Pergunta principal:** é fácil encontrar no código onde cada regra de negócio foi implementada?

**Análise**

As regras principais são relativamente rastreáveis porque estão em classes bem nomeadas e cobertas por testes no runner em `Tests/Program.cs`. Porém, ainda falta uma tabela formal ligando regra, classe, método e teste.

Além disso, a regra de elegibilidade existe em `EligibilityService`, mas não está conectada ao caminho principal de `RequestAuthorizationUseCase` ou `ApproveAuthorizationUseCase`. Isso deve aparecer na rastreabilidade para evitar falsa impressão de que a regra está sempre aplicada.

**Evidências**

| Regra de negócio | Classe/Método | Existe teste? | Observação |
|---|---|---|---|
| Solicitação deve ter ao menos um item | `AuthorizationRequest` | Sim | Bem localizada. |
| Aprovação integral aprova todos os itens | `AuthorizationRequest.ApproveFully()` | Sim | Bem localizada. |
| Aprovação parcial valida quantidade e item | `AuthorizationRequest.ApprovePartially(...)` | Sim | Bem localizada. |
| Solicitação decidida não pode receber nova decisão | `AuthorizationRequest.EnsurePending()` | Sim | Testada indiretamente. |
| Negativa exige justificativa | `AuthorizationRequest.Deny(...)` | Sim | Bem localizada. |
| Urgência aprova e exige auditoria posterior | `AuthorizationRequestFactory` e `AuthorizationRequest.SetAsEmergencyException()` | Sim | Bem localizada. |
| Beneficiário inativo não pode aprovar | `EligibilityService.ValidateEligibility(...)` | Sim | Não integrado ao fluxo principal. |
| Carência impede autorização | `EligibilityService` e `Plan.IsGracePeriodFulfilled(...)` | Sim | Não integrado ao fluxo principal. |
| Faturamento só aceita autorização aprovada | `CreateHospitalBillFromAuthorizationUseCase` | Sim | Workflow de Application. |
| Faturamento parcial usa apenas itens aprovados | `CreateHospitalBillFromAuthorizationUseCase` | Sim | Bem testada. |

**Avaliação:** atende parcialmente.

**Sugestão de melhoria**

Criar uma tabela de rastreabilidade dentro da documentação, mantendo a ligação entre regra, método e teste. Essa tabela também deve marcar regras que existem no domínio, mas ainda não estão integradas ao fluxo principal.

### 8.3. Diagrama de domínio

**Pergunta principal:** o diagrama apresentado reflete o que foi realmente codificado?

**Análise**

O documento `documentacao_modelagem_ddd_saude.md` preserva diagramas conceituais e atualiza o texto para refletir o código atual. Isso é bom para contexto histórico, mas pode gerar dúvida quando o leitor compara diagrama e implementação.

O código atual tem um recorte mais específico: autorização, elegibilidade, faturamento básico, glosas e recurso administrativo. Já os diagramas e a documentação também citam evoluções como ANS/TISS v2, GuiaTISS e FaturamentoXML.

**Avaliação:** atende parcialmente.

**Sugestão de melhoria**

Criar um diagrama simples e atualizado apenas do código executável atual. Ele deve mostrar:

- `AuthorizationRequest` contendo `RequestedItem`;
- `EligibilityService` usando `Beneficiary`, `Plan` e `ProcedureCatalogItem`;
- `HospitalBill` contendo `BillItem`;
- `BillItem` contendo `Glosa`;
- `Glosa` contendo `AdministrativeAppeal`;
- repositories apenas para `AuthorizationRequest` e `HospitalBill`.

## 9. Propostas de Melhoria com Supple Design

### 9.1. Intention-Revealing Interfaces

**Ideia:** métodos e classes devem revelar claramente sua intenção.

**Problema encontrado**

O projeto possui bons nomes em vários pontos, mas alguns ainda podem comunicar melhor o negócio. `MaterialsAndMedicines`, por exemplo, é usado para criar `RequestedItem`, mas o domínio trata esses elementos como itens solicitados. O nome atual limita o conceito a materiais e medicamentos, embora o fluxo possa incluir outros itens relacionados ao procedimento.

Outro ponto é `ClinicalJustification`, que no código precisa ser um CID válido. O nome parece sugerir texto livre de justificativa clínica, mas a implementação espera um `CidCode`.

**Sugestão de melhoria**

Renomear campos e métodos para revelar melhor o conceito de domínio:

| Nome atual | Sugestão |
|---|---|
| `MaterialsAndMedicines` | `RequestedItems` |
| `ClinicalJustification` | `CidCode` ou `ClinicalJustificationCid` |
| `SetAsEmergencyException` | `ApproveAsEmergencyException` |

**Justificativa**

Nomes mais precisos reduzem ambiguidade e tornam o código mais próximo da conversa real sobre autorização de saúde.

### 9.2. Side-Effect-Free Functions

**Ideia:** sempre que possível, métodos de consulta ou cálculo não devem alterar o estado do objeto.

**Problema encontrado**

O projeto já tem bons exemplos de métodos sem efeito colateral, como:

- `Beneficiary.CalculateAge(...)`;
- `Plan.IsGracePeriodFulfilled(...)`;
- `ProcedureCatalogItem.IsAgePermitted(...)`;
- `BillItem.TotalValue`.

O risco está em futuras evoluções misturarem cálculo com alteração de estado, principalmente no faturamento. Como `HospitalBill` ainda não possui `TotalValue`, esse cálculo hoje aparece no DTO mapper `BillingUseCaseSupport.ToDto(...)`.

**Sugestão de melhoria**

Adicionar cálculo puro no domínio e deixar alteração de estado em métodos separados.

```csharp
public decimal TotalValue => _items.Sum(item => item.TotalValue);

public void Close()
{
    if (!_items.Any())
        throw new InvalidOperationException("Uma conta hospitalar precisa ter itens.");

    Status = HospitalBillStatus.Fechada;
}
```

**Justificativa**

Cálculos sem efeito colateral são mais previsíveis, mais fáceis de testar e podem ser usados por Application, UI e persistência sem risco de mudar estado acidentalmente.

### 9.3. Assertions

**Ideia:** o modelo deve deixar explícitas suas condições obrigatórias e invariantes.

**Problema encontrado**

O domínio já possui várias validações em construtores e métodos. Mesmo assim, há pontos a reforçar:

- Value Objects com `init` público podem ter valor inválido atribuído no object initializer.
- `AdministrativeAppeal` valida evidências, mas não valida explicitamente `id` e `glosaId` contra `Guid.Empty`.
- `HospitalBill.AddItem` aceita qualquer `BillItem`, sem validar se o item pertence ao mesmo contexto de beneficiário/autorização esperado.

**Sugestão de melhoria**

Fechar propriedades dos Value Objects e reforçar invariantes em entidades internas.

```csharp
internal AdministrativeAppeal(Guid id, Guid glosaId, List<Evidence> evidenceDocuments)
{
    if (id == Guid.Empty)
        throw new ArgumentException("O id do recurso é inválido.", nameof(id));

    if (glosaId == Guid.Empty)
        throw new ArgumentException("A glosa do recurso é inválida.", nameof(glosaId));

    if (evidenceDocuments == null || !evidenceDocuments.Any())
        throw new ArgumentException("Um recurso deve conter pelo menos uma evidência.", nameof(evidenceDocuments));
}
```

**Justificativa**

As regras essenciais devem estar protegidas no próprio modelo para que nenhuma camada externa precise "lembrar" de validar a mesma coisa.

### 9.4. Conceptual Contours

**Ideia:** o design deve separar conceitos que possuem significados diferentes no negócio.

**Problema encontrado**

O DTO `AuthorizationRequestDto` recebe `MaterialsAndMedicines` como `List<string>`. Isso mistura conceitos que podem ter regras diferentes no domínio:

- material;
- medicamento;
- procedimento associado;
- item solicitado;
- quantidade solicitada.

Além disso, todo item nasce com quantidade 1 em `RequestAuthorizationUseCase.CreateRequestedItems(...)`. Isso simplifica o v1, mas limita o modelo.

**Sugestão de melhoria**

Criar um DTO de item solicitado com descrição, quantidade e, se necessário, tipo.

```csharp
public class RequestedItemDto
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? ItemType { get; set; }
}
```

**Justificativa**

Separar melhor os contornos conceituais evita que regras específicas de quantidade, material, medicamento ou procedimento fiquem escondidas em uma lista de strings.

### 9.5. Standalone Classes

**Ideia:** classes importantes do domínio devem ser compreensíveis por si mesmas, sem depender de muitos detalhes externos.

**Problema encontrado**

`AuthorizationRequest` é compreensível por si mesma. Já `HospitalBill` depende mais da Application para ganhar significado: o caso de uso decide quais itens entram, de onde vêm os valores e quando a conta pode ser criada.

Isso é aceitável no v1, mas enfraquece a expressividade do agregado de faturamento.

**Sugestão de melhoria**

Adicionar comportamento de negócio direto em `HospitalBill`, como total, aplicação de glosa por item e fechamento.

```csharp
public void AddApprovedAuthorizationItem(
    Guid authorizationId,
    string description,
    int approvedQuantity,
    decimal unitValue)
{
    AddItem(new BillItem(Guid.NewGuid(), authorizationId, description, approvedQuantity, unitValue));
}
```

**Justificativa**

Uma classe de domínio forte mostra o que ela faz sem obrigar o leitor a procurar todo o comportamento em use cases e mapeadores.

### 9.6. Closure of Operations

**Ideia:** sempre que possível, uma operação deve retornar um objeto do mesmo tipo conceitual ou manter a consistência do modelo.

**Problema encontrado**

Valores financeiros são representados por `decimal` em `BillItem.UnitValue`, `BillItem.TotalValue`, `HospitalBillDto.TotalValue` e `CreateHospitalBillDto.UnitValuesByItemId`.

Para o recorte acadêmico, `decimal` é suficiente. Mas, em faturamento de saúde, dinheiro tende a ganhar regras próprias: moeda, arredondamento, soma, desconto, glosa, estorno e auditoria.

**Sugestão de melhoria**

Criar um Value Object `Money`.

```csharp
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = "BRL")
    {
        if (amount < 0)
            throw new ArgumentException("Valor não pode ser negativo.", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Moeda é obrigatória.", nameof(currency));

        Amount = amount;
        Currency = currency;
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Não é possível somar moedas diferentes.");

        return new Money(Amount + other.Amount, Currency);
    }
}
```

**Justificativa**

Um Value Object de dinheiro deixa regras financeiras mais explícitas e evita tratar valor monetário como número solto.

## 10. Plano de Refatoração Proposto

| Problema identificado | Impacto no domínio | Melhoria proposta | Prioridade |
|---|---|---|---|
| `EligibilityService` não está integrado ao fluxo principal | Permite solicitar/aprovar sem passar por elegibilidade | Carregar beneficiário, plano e procedimento no caso de uso e chamar o Domain Service | Alta |
| Value Objects usam `init` público | Invariantes podem ser burladas no object initializer | Trocar para `get` ou `private init` | Alta |
| Entidades internas têm métodos públicos de alteração | Pode quebrar a fronteira do agregado | Restringir métodos internos e expor operações pela Aggregate Root | Alta |
| `HospitalBill` é pouco expressiva | Regras de faturamento tendem a ficar na Application | Adicionar total, fechamento e aplicação de glosa pela root | Média |
| DTO de autorização usa `List<string>` para itens | Perde quantidade e tipo do item solicitado | Criar `RequestedItemDto` com quantidade e tipo | Média |
| Mistura de português e inglês | Enfraquece a linguagem ubíqua | Padronizar idioma dos nomes de domínio | Média |
| Diagrama mistura conceito futuro e código atual | Pode confundir avaliação do que foi implementado | Criar diagrama específico do código executável atual | Média |
| Dinheiro modelado como `decimal` | Regras financeiras ficam implícitas | Criar Value Object `Money` quando faturamento evoluir | Baixa |
| Rastreabilidade sem tabela formal | Dificulta defesa técnica | Criar tabela regra -> classe/método -> teste | Média |

## 11. Priorização das Melhorias

### Melhorias críticas

São as que afetam diretamente a integridade do domínio.

- Integrar `EligibilityService` ao fluxo de autorização.
- Fechar invariantes dos Value Objects removendo `init` público.
- Garantir que alterações em entidades internas passem pela Aggregate Root.
- Reforçar assertions em entidades internas, como `AdministrativeAppeal`.

### Melhorias importantes

São as que melhoram clareza, manutenção e evolução.

- Enriquecer `HospitalBill` com comportamento de faturamento.
- Criar `RequestedItemDto` em vez de usar `List<string>`.
- Padronizar a linguagem ubíqua em português ou inglês.
- Criar tabela de rastreabilidade das regras de negócio.
- Atualizar o diagrama para refletir somente o código atual.

### Melhorias desejáveis

São refinamentos que melhoram o design sem impedir a execução atual.

- Criar `Money` como Value Object quando o faturamento crescer.
- Adotar injeção de dependência formal na camada de entrada.
- Reorganizar módulos por conceito de negócio se o projeto aumentar.
- Documentar cenários de evolução para ANS/TISS v2 separadamente do recorte implementado.

## 12. Conclusão da Análise

O projeto `DDD-Case-1` demonstra boa compreensão de DDD Tático. A separação entre `Domain`, `Application`, `Infra`, `UI` e `Tests` está coerente, o domínio está isolado da infraestrutura e as interfaces de repositório ficam no lugar correto.

O ponto mais forte do projeto é `AuthorizationRequest` como Aggregate Root. Ela protege as principais decisões de autorização, como aprovação integral, aprovação parcial, negativa, pendência documental e exceção de urgência/emergência. Isso mostra que o domínio não foi tratado apenas como estrutura de dados.

Os principais pontos de melhoria estão na integração da elegibilidade ao fluxo principal, na proteção mais forte dos Value Objects, na passagem obrigatória por Aggregate Roots e no enriquecimento do agregado de faturamento.

De modo geral, o projeto possui uma base sólida para um recorte acadêmico de DDD tático v1. Com as melhorias propostas, o modelo ficaria mais expressivo, mais seguro contra inconsistências e mais alinhado à ideia de *Supple Design*.

## 13. Checklist Final de Avaliação

| Critério | Atende? | Observações |
|---|---|---|
| Domínio independente de infraestrutura | Sim | `Domain` não referencia SQLite, UI ou Application. |
| Interfaces de repositório bem posicionadas | Sim | Interfaces no `Domain`, implementações no `Infra`. |
| Application Services sem regra de negócio | Parcial | São magros, mas elegibilidade ainda não é coordenada no fluxo principal. |
| Entidades possuem comportamento | Parcial | `AuthorizationRequest` é forte; `HospitalBill` ainda é simples. |
| Value Objects são imutáveis | Parcial | Validam no construtor, mas usam `init` público. |
| Linguagem ubíqua aparece no código | Parcial | Termos do domínio aparecem, mas há mistura de idiomas. |
| Aggregates protegem invariantes | Parcial | Autorização protege bem; faturamento pode evoluir. |
| Alterações passam pela Aggregate Root | Parcial | Coleções são somente leitura, mas entidades internas têm métodos públicos. |
| Existe apenas um repositório por Aggregate Root | Sim | Não há repositórios para entidades internas. |
| Factories são justificadas | Sim | `AuthorizationRequestFactory` aplica regra de urgência/emergência. |
| Domain Services são bem usados | Parcial | `EligibilityService` é adequado, mas não integrado ao fluxo principal. |
| Modules agrupam conceitos de domínio | Parcial | Existem subpastas de domínio, mas a primeira divisão ainda é técnica. |
| Decisões técnicas foram justificadas | Sim | Documentação explica trade-offs principais. |
| Regras de negócio são rastreáveis | Parcial | Há testes e nomes claros, mas falta tabela formal de rastreabilidade. |
| Diagrama reflete o código | Parcial | O texto foi atualizado, mas os diagramas ainda misturam conceito e evolução futura. |
| Há propostas de melhoria com Supple Design | Sim | Este relatório propõe melhorias por expressividade, invariantes e fronteiras conceituais. |

## 14. Orientação Final

O projeto não deve ser avaliado apenas pela existência de pastas chamadas `Domain`, `Application` e `Infra`. O que realmente mostra DDD é a forma como as regras importantes ficam protegidas no domínio.

Nesse sentido, `AuthorizationRequest` é a principal evidência positiva: ela concentra a decisão de autorização e impede transições inválidas. Para evoluir, o projeto deve levar esse mesmo padrão para faturamento, elegibilidade integrada, Value Objects mais fechados e fronteiras de aggregate mais rígidas.

Uma boa próxima etapa seria transformar este relatório em um plano incremental de refatoração, começando pelas melhorias críticas e validando cada mudança com testes de domínio.
