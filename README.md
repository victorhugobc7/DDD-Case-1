# Sistema de Gestão de Operadora de Saúde (DDD Case)

**Versão da entrega implementada:** DDD tático v1

**Status:** Implementação demonstrável do fluxo de Autorização de Procedimento

**Última Atualização:** Maio de 2026

---

## Sumário Executivo

Este projeto implementa a modelagem arquitetural de um **sistema de gestão de operadora de saúde**, com foco especial em processos críticos de:
- **Autorização de Procedimentos** (pré-autorização)
- **Execução de Procedimentos** (rastreabilidade médica)
- **Faturamento em Lotes TISS** (padrão obrigatório ANS)
- **Auditoria e Glosas** (controle de fraude e pertinência)

O código atual implementa o recorte **DDD tático v1**, concentrado em Autorização de Procedimento. Os tópicos ANS/TISS v2, GuiaTISS, FaturamentoXML e Auditoria formal permanecem descritos como evolução futura do modelo.

---

## Estado da Implementação

Projetos:

- `Domain`: módulos de negócio em `Domain/Modules`, com entidades, value objects, enums, factories, contratos de repository e domain services agrupados por área.
- `Application`: DTOs e `AuthorizationService`, coordenando casos de uso sem concentrar regra de negócio.
- `Infra`: repository em memória para demonstração.
- `UI`: console demonstrando solicitação pendente, aprovação parcial e urgência/emergência com auditoria posterior.
- `Tests`: runner de testes sem dependências externas.

### Módulos de Domínio Implementados

Os módulos ficam em `Domain/Modules`. A lista abaixo é a divisão modular do projeto; as tabelas de entidades v1.0/v2.0 continuam sendo apenas o catálogo de entidades e conceitos do domínio.

| Módulo | Pasta | Principais tipos |
|--------|-------|------------------|
| **Beneficiários** | `Domain/Modules/Beneficiarios` | `Beneficiary`, `BeneficiaryStatus` |
| **Planos** | `Domain/Modules/Planos` | `Plan`, `PlanNumber`, `PlanType` |
| **Procedimentos** | `Domain/Modules/Procedimentos` | `ProcedureCatalogItem`, `ProcedureCode`, `ProcedureType`, `CidCode` |
| **Rede Credenciada** | `Domain/Modules/RedeCredenciada` | `ProfessionalRegistry` |
| **Autorizações** | `Domain/Modules/Autorizacoes` | `AuthorizationRequest`, `RequestedItem`, `AuthorizationRequestFactory`, `EligibilityService`, `IAuthorizationRepository` |
| **Faturamento** | `Domain/Modules/Faturamento` | `HospitalBill`, `BillItem`, `IHospitalBillRepository` |
| **Auditoria** | `Domain/Modules/Auditoria` | `Glosa`, `AdministrativeAppeal`, `Evidence`, `GlosaReason`, `AppealStatus` |

### Roteiro da Atividade

#### 1. Qual era o problema do case

O case trata da modelagem de uma operadora de saúde que precisa controlar solicitações de procedimentos, validação de elegibilidade, regras de plano, execução/faturamento e possíveis glosas. O problema central era transformar um fluxo de saúde suplementar, cheio de regras e exceções, em um modelo de domínio organizado, rastreável e preparado para evolução para padrões ANS/TISS.

#### 2. Quais módulos foram definidos

Foram definidos módulos por área de negócio, não por tipo técnico de classe:

- **Beneficiários**: dados e situação do segurado.
- **Planos**: produto contratado, número do plano, tipo e regras de carência.
- **Procedimentos**: catálogo de procedimentos, código do procedimento, tipo e CID.
- **Rede Credenciada**: identificação do profissional/prestador envolvido.
- **Autorizações**: solicitação, itens solicitados, status, fábrica e elegibilidade.
- **Faturamento**: conta hospitalar e itens faturados.
- **Auditoria**: glosas, motivos, evidências e recurso administrativo.

#### 3. Quais foram as principais Entities e Value Objects

Principais **Entities**:

- `Beneficiary`
- `Plan`
- `ProcedureCatalogItem`
- `AuthorizationRequest`
- `RequestedItem`
- `HospitalBill`
- `BillItem`
- `Glosa`
- `AdministrativeAppeal`

Principais **Value Objects**:

- `PlanNumber`
- `ProcedureCode`
- `CidCode`
- `ProfessionalRegistry`
- `Evidence`

#### 4. Quais Aggregates foram definidos

No recorte implementado, os principais aggregates são:

- `AuthorizationRequest`, contendo seus `RequestedItem`.
- `HospitalBill`, contendo seus `BillItem`.
- `BillItem`, concentrando as `Glosa` aplicadas ao item faturado.
- `Glosa`, controlando seu `AdministrativeAppeal`.
- `Beneficiary`, `Plan` e `ProcedureCatalogItem` aparecem como aggregates simples de referência do domínio.

#### 5. Qual classe foi escolhida como Aggregate Root e por quê

A principal **Aggregate Root** escolhida foi `AuthorizationRequest`, porque ela concentra o ciclo de vida da solicitação de autorização. É nela que ficam o status da solicitação, os itens solicitados, a aprovação integral/parcial, a negativa, a pendência documental e a exceção de urgência/emergência com auditoria posterior.

Essa escolha evita que outras camadas alterem diretamente os itens ou o status sem passar pelas regras do domínio.

#### 6. Quais regras de negócio foram centralizadas no domínio

As principais regras centralizadas no domínio foram:

- Solicitação de autorização deve possuir pelo menos um item.
- Item solicitado deve ter quantidade maior que zero.
- Apenas solicitações pendentes podem ser aprovadas, negadas ou marcadas com pendência.
- Aprovação integral aprova todos os itens.
- Aprovação parcial valida se os itens pertencem à solicitação e se as quantidades são válidas.
- Negativa exige justificativa.
- Urgência/emergência é aprovada como exceção e exige auditoria posterior.
- Beneficiário inativo não pode ter autorização aprovada.
- Beneficiário deve pertencer ao plano informado.
- Plano aplica carência por tipo de procedimento.
- Procedimento pode restringir idade mínima e máxima.
- Glosa exige justificativa.
- Uma glosa só pode ter um recurso administrativo ativo.

#### 7. Onde foram usados Factories, Services e Repositories

- **Factory**: `AuthorizationRequestFactory`, no módulo de Autorizações, cria uma solicitação completa com value objects e itens solicitados.
- **Domain Service**: `EligibilityService`, no módulo de Autorizações, valida elegibilidade cruzando beneficiário, plano e procedimento.
- **Application Service**: `AuthorizationService`, no projeto `Application`, coordena os casos de uso e chama o domínio sem concentrar regra de negócio.
- **Repositories**: `IAuthorizationRepository` e `IHospitalBillRepository` são contratos do domínio. `AuthorizationRepository`, no projeto `Infra`, é a implementação em memória usada na demonstração.

#### 8. Quais foram as decisões mais difíceis de modelagem

- Separar **módulos de negócio** de uma simples lista de entidades.
- Escolher `AuthorizationRequest` como Aggregate Root principal, já que o fluxo implementado começa pela autorização.
- Decidir o que implementar no recorte v1 e o que deixar como evolução ANS/TISS v2.
- Modelar urgência/emergência como exceção aprovada com auditoria posterior.
- Separar `Plan` como produto/regras do plano, deixando `Contrato` como evolução futura.
- Representar `Glosa` com rastreabilidade e possibilidade de recurso, em vez de tratá-la como simples desconto.

Validação esperada com SDK .NET compatível com `net10.0`:

```bash
dotnet build HealthInsurance.slnx
dotnet run --project Tests/Tests.csproj
dotnet run --project UI/UI.csproj
```

---

## Visão Geral do Projeto

### Cenário de Negócio

As operadoras de saúde suplementar no Brasil precisam gerenciar fluxos complexos:

1. **Recebimento de Solicitações**: Beneficiários e prestadores solicitam autorizações para procedimentos  
2. **Análise de Pertinência**: Decisão médica e verification de carência/cobertura  
3. **Execução**: Realização do procedimento com rastreabilidade  
4. **Faturamento**: Envio de lotes de guias ao sistema da operadora  
5. **Auditoria e Glosas**: Análise de conformidade e aplicação de descontos (glosas)

### Diferenças Fundamentais: Eletivo vs. Urgência

- **Atendimento Eletivo**: Passa por pré-autorização formal; maior prazo
- **Atendimento de Urgência/Emergência**: Autorização posterior (emergency first); análise rápida

---

## Versão 1.0: Modelo Conceitual Inicial

### Propósito

A **v1.0** representa o design inicial do sistema, onde estruturamos as entidades-chave sem compromisso com regulamentações externas. É uma **abstração funcional pura**, ideal para compreender os relacionamentos básicos.

### Diagrama de Classes (v1.0)

![Diagrama de Classes v1.0](WhatsApp%20Image%202026-03-26%20at%2011.59.25.jpeg)

### Tabela de Entidades (v1.0)

| Classe | Responsabilidade | Atributos Principais | Status v1.0 |
|--------|-----------------|----------------------|------------|
| **OperadoraSaude** | Entidade central que coordena planos e processos | CNPJ, Nome Fantasia | Conceitual |
| **Plano** | Produto de saúde oferecido | Código, Tipo, Regras de Carência | Simplificado |
| **Beneficiario** | Segurado/paciente | Carteirinha, Nome, Data Adesão | Básico |
| **Clinica** | Prestadora de serviços (rede credenciada) | CNPJ, Razão Social, Flag de Credenciamento | Genérico |
| **Procedimento** | Serviço médico | Código TUSS, Descrição, Tipo | Sem validação ANS |
| **SolicitacaoAutorizacao** | Fluxo de pré-autorização | Protocolo, Data, Status, Justificativa | Simplificado |
| **ExecucaoProcedimento** | Execução do procedimento autorizado | ID, Data/Hora, Observações Clínicas | Sem Profissional |
| **Faturamento** | Fatura emitida pelo prestador | Número, Data, Valor | Ad-hoc |
| **Glosa** | Desconto por não-conformidade | Código, Descrição, Valor | Sem Auditoria |

### Limitações Identificadas (v1.0)

- Sem conformidade com regulamentações ANS
- Plano não diferencia **Produto** (Plano) de **Venda** (Contrato com vigência)
- Faturamento é genérico; não segue padrão TISS/XML
- Sem rastreabilidade de **quem** assinou (profissional médico)
- Sem **CID** (Classificação Internacional de Doenças) para justificativa médica
- Glosa sem processo formal de Auditoria
- Sem conceito de **Lote** de faturamento  

---

## Versão 2.0: Modelo Padrão ANS/TISS

### Propósito

A **v2.0** incorpora os requisitos reais da regulamentação brasileira, alinhando-se aos padrões obrigatórios da ANS e TISS. Esta versão reflete a complexidade real do mercado de saúde suplementar.

### Diagrama de Classes (v2.0)

![Diagrama de Classes v2.0](FaturamentoXML%20Lote-2026-03-26-150705.png)

### Tabela de Entidades (v2.0)

| Classe | Responsabilidade | Novidades vs v1.0 | Status v2.0 |
|--------|-----------------|-------------------|------------|
| **OperadoraSaude** | Entidade central regulada | Agora com Registro ANS obrigatório | Regulamentado |
| **Plano** | Produto ANS puro | Código ANS obrigatório; Segmentação Assistencial | Padrão ANS |
| **Contrato** | Venda/adesão do plano | **Nova classe**: Separa Produto de Contrato; Inclui Vigência | Production-Ready |
| **Beneficiario** | Segurado com dados civis | Agora requer Data de Nascimento; Melhor rastreabilidade | Fortalecido |
| **Prestador** | Entidade prestadora (hospital, clínica) | Inclui CNES (código nacional de estabelecimento); CPF/CNPJ | Identificado |
| **ProfissionalSaude** | **Novo**: Médico/dentista que assina | CRM, UF, Especialidade; Rastreabilidade legal | Crítico |
| **ProcedimentoTUSS** | Tabela de procedimentos | Código TUSS obrigatório; Flag de Auditoria Prévia | Padrão Obrigatório |
| **DoencaCID** | **Novo**: Justificativa médica | Código CID-10; Rastreabilidade de motivo | Requerido |
| **GuiaTISS** | **Novo**: Documento padrão TISS | Substitui SolicitacaoAutorizacao + ExecucaoProcedimento; Tipos específicos | Padrão Mandatório |
| **FaturamentoXML** | **Novo**: Lote de faturamento | Substitui Faturamento genérico; XML com assinatura digital | Padrão Obrigatório |
| **Auditoria** | **Novo**: Processo formal de avaliação | Parecer médico; Decisão formal de aprovação/glosa | Compliance |
| **Glosa** | Desconto com rastreabilidade | Agora com Código de Motivo ANS; Resultado de Auditoria | Auditado |

---

## Changelog Arquitetural: Do v1.0 ao v2.0

### 1. Separação de Plano e Contrato

**Problema v1.0:**  
Plano representava tanto o **produto** quanto a **venda**, causando confusão entre regras do produto e dados da adesão do cliente.

**Solução v2.0:**
```
Plano (Produto ANS)
  ├─ Código ANS (regulamentado)
  ├─ Nome Comercial
  ├─ Segmentação Assistencial (Ambulatorial, Hospitalar, etc.)
  └─ Regras de Cobertura

Contrato (Venda/Adesão)
  ├─ Número do Contrato
  ├─ Vigência (início e fim)
  ├─ Tipo (Individual, Coletivo, etc.)
  └─ Status (Ativo, Cancelado, etc.)
```

**Benefício:**
- Planos reutilizáveis em múltiplos contratos
- Histórico de vigências e mudanças
- Conformidade com regulamentos ANS

---

### 2. Adoção da GuiaTISS e FaturamentoXML

**Problema v1.0:**  
Faturamento era genérico (`Faturamento`) e `SolicitacaoAutorizacao`/`ExecucaoProcedimento` eram entidades separadas, desalinhadas com a prática real.

**Solução v2.0:**
```
GuiaTISS (Documento Único)
  ├─ Tipo (Consulta, SADT, Internação, etc.)
  ├─ Caráter (Eletivo, Urgência, Emergência)
  ├─ Status (Pendente, Autorizada, Faturada)
  ├─ Itens (Procedimentos TUSS com CID)
  └─ Assinatura Digital do Profissional

FaturamentoXML (Lote Obrigatório)
  ├─ Número do Lote
  ├─ Data de Envio
  ├─ Valor Total Apresentado
  ├─ Assinatura Digital
  └─ Referência às Guias (1...*)
```

**Benefício:**
- Padrão obrigatório da ANS/TISS
- Rastreabilidade completa (quem assinou, quando, o quê)
- Faturamento em lotes, não isoladamente
- Auditoria facilitada

---

### 3. Inclusão de Profissional de Saúde e CID

**Problema v1.0:**  
`ExecucaoProcedimento` tinha apenas "observações clínicas"; sem rastreabilidade de **quem** realizou e **por quê** (CID).

**Solução v2.0:**
```
ProfissionalSaude (Novo)
  ├─ Nome
  ├─ Conselho Regional (CRM, COREN, etc.)
  ├─ UF do Conselho
  ├─ Especialidade
  └─ Assinatura Digital

GuiaTISS → ProfissionalSaude
  └─ Rastreabilidade legal: Quem autorizou/executou?

DoencaCID (Novo)
  ├─ Código CID-10
  ├─ Descrição
  └─ Justificativa Médica

GuiaTISS → DoencaCID (0..*)
  └─ Por quê este procedimento foi realizado?
```

**Benefício:**
- Conformidade legal (responsabilidade médica)
- Prevenção de fraude (rastreabilidade completa)
- Análise de pertinência médica (CID obrigatório)
- Auditoria interna/externa facilitada

---

### 4. Introdução da Auditoria Formal

**Problema v1.0:**  
Glosa era aplicada diretamente sem processo formal de avaliação. Sem decisão médica ou conformidade.

**Solução v2.0:**
```
Processo de Faturamento v2.0:

1. GuiaTISS Autorizada
            ↓
2. FaturamentoXML (Lote) Enviado
            ↓
3. Auditoria (Processo Formal)
   ├─ Avaliação de Pertinência Médica
   ├─ Verificação de Cobertura
   ├─ Análise de Regras de Negócio
   └─ Parecer Médico
            ↓
4. Decisão (Aprovada / Glosa Parcial / Glosa Total)
            ↓
5. Glosa (Se aplicável)
   ├─ Código de Motivo ANS
   ├─ Descrição
   └─ Valor Glosado
```

**Tabela de Status de Auditoria:**

| Status | Significado | Ação |
|--------|------------|------|
| **PENDENTE** | Aguardando análise | Fila de trabalho |
| **APROVADA** | Sem glosa; pagar 100% | Processar pagamento |
| **GLOSA_PARCIAL** | Pagar parte do solicitado | Gerar glosa + pagamento |
| **GLOSA_TOTAL** | Não pagar nada | Gerar glosa |

**Benefício:**
- Controle de fraude robusto
- Documentação formal de decisões
- Conformidade com regulamentações
- Histórico de auditoria para compliance

---

## 📝 Resumo das Mudanças Principais

### Comparativo v1.0 vs v2.0

| Aspecto | v1.0 | v2.0 | Motivação |
|--------|------|------|-----------|
| **Modelo de Plano** | Genérico | ANS + Contrato | Regulamentação |
| **Faturamento** | Ad-hoc (`Faturamento`) | TISS em Lotes (`FaturamentoXML`) | Padrão Mandatório |
| **Autorização** | Genérica (`SolicitacaoAutorizacao`) | Padrão TISS (`GuiaTISS`) | Interoperabilidade |
| **Profissional** | Implícito na clínica | Explícito com CRM/UF (`ProfissionalSaude`) | Rastreabilidade Legal |
| **Justificativa** | Texto livre | CID-10 obrigatório (`DoencaCID`) | Pertinência Médica |
| **Glosa** | Sem auditoria | Resultado de Auditoria formal | Compliance |
| **Conformidade** | Nenhuma | ANS + TISS + CRM + CID-10 | Exigência Legal |


## Link do canvas do excalidraw utilizado no projeto

https://excalidraw.com/#json=CAfUBVa2wkKpQ_3C9_ASO,5qYLOszq5q4c2iNDW4NHMA

---

## Referencias e Padrões Utilizados

- **DDD (Domain-Driven Design)**: Modelagem alinhada ao domínio de saúde suplementar
- **ANS (Agência Nacional de Saúde Suplementar)**: Regulamentações obrigatórias
- **TISS (Troca de Informações em Saúde Suplementar)**: Padrão de interoperabilidade
- **CID-10 (Classificação Internacional de Doenças)**: Padronização médica
- **CRM (Conselho Regional de Medicina)**: Rastreabilidade de profissionais
- **CNES (Cadastro Nacional de Estabelecimentos de Saúde)**: Identificação de prestadores

---
