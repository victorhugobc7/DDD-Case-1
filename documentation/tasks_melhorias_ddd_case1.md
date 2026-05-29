# Tasks para Atender o Relatorio de Melhorias do DDD-Case-1

## Contexto

Este backlog transforma as recomendacoes de `relatorio_analise_melhorias_ddd_case1.md` e `resumo_relatorio_analise_melhorias_ddd_case1.md` em tasks independentes, pensadas para serem executadas como uma task por PR.

Baseline confirmada antes da criacao deste backlog:

- `dotnet build HealthInsurance.slnx`: passou.
- `dotnet run --project Tests/Tests.csproj`: passou com 18/18 testes.

Os pontos ja avaliados como completos devem ser preservados, nao refeitos:

- `Domain` independente de infraestrutura.
- Inversao de dependencia com contratos de repository no dominio e implementacoes em `Infra`.
- Um repository por Aggregate Root persistido.
- `AuthorizationRequestFactory` justificada pela regra de urgencia/emergencia.

## Status da implementacao

Revisao de 28/05/2026: TASK-01 a TASK-11 foram implementadas no recorte v1. Este arquivo permanece como backlog original e rastreio do que foi atendido; os relatorios finais registram o estado atualizado.

## Definition of Done comum

Cada task deve terminar com:

- build da solucao passando;
- runner de testes passando;
- novos testes cobrindo a melhoria;
- documentacao atualizada quando houver mudanca de comportamento, linguagem publica ou arquitetura;
- `git diff --check` sem problemas de whitespace.

Comandos padrao:

```bash
dotnet build HealthInsurance.slnx
dotnet run --project Tests/Tests.csproj
git diff --check
```

## TASK-01 - Integrar elegibilidade ao fluxo real de autorizacao

**Prioridade:** Alta

**Problema:** `EligibilityService` existe e esta testado, mas `RequestAuthorizationUseCase`, `ApproveAuthorizationUseCase` e `AuthorizationService` ainda permitem o fluxo normal sem validar beneficiario, plano e procedimento.

**Objetivo:** tornar a elegibilidade obrigatoria no caminho real de solicitacao de autorizacao.

**Implementacao esperada:**

- Criar contratos de leitura no `Domain` para carregar:
  - `Beneficiary`;
  - `Plan`;
  - `ProcedureCatalogItem`.
- Implementar repositories ou consultas seedaveis em `Infra` para esses dados de referencia.
- Injetar esses contratos em `RequestAuthorizationUseCase`.
- Antes de criar `AuthorizationRequest`, carregar as entidades e chamar `EligibilityService.ValidateEligibility(...)`.
- Tratar ausencia de beneficiario, plano ou procedimento como erro de aplicacao com mensagem clara.
- Atualizar `AuthorizationService`, `UI` e testes para fornecer os novos dados necessarios.

**Criterios de aceite:**

- Uma solicitacao com beneficiario inativo falha no fluxo de aplicacao.
- Uma solicitacao com plano divergente falha no fluxo de aplicacao.
- Uma solicitacao em carencia falha no fluxo de aplicacao.
- Uma solicitacao com idade fora da regra falha no fluxo de aplicacao.
- Uma solicitacao elegivel continua sendo criada normalmente.
- Urgencia/emergencia tambem passa pela validacao de elegibilidade antes da excecao de aprovacao automatica.

**Validacao minima:**

- Adicionar testes de aplicacao que provem que `EligibilityService` e chamado indiretamente pelo fluxo de solicitacao.
- Manter os testes diretos existentes de `EligibilityService`.

## TASK-02 - Fechar invariantes dos Value Objects

**Prioridade:** Alta

**Problema:** `PlanNumber`, `ProcedureCode`, `CidCode`, `ProfessionalRegistry` e `Evidence` validam no construtor, mas usam propriedades com `public init`, permitindo object initializer contornar a validacao.

**Objetivo:** impedir que Value Objects sejam inicializados em estado invalido pela API publica.

**Implementacao esperada:**

- Trocar `public init` por propriedades somente leitura:
  - `public string Value { get; }`;
  - `public string DocumentUrl { get; }`;
  - `public string Description { get; }`.
- Manter toda validacao dentro dos construtores.
- Conferir se repositories e DTO mappers continuam usando construtores publicos.

**Criterios de aceite:**

- Nao existe `public init` nos Value Objects citados.
- Entradas vazias continuam gerando excecao.
- `CidCode` continua rejeitando formato invalido.
- O build e todos os testes passam.

**Validacao minima:**

- Adicionar testes para `PlanNumber`, `ProcedureCode`, `CidCode`, `ProfessionalRegistry` e `Evidence`.
- Garantir que nao ha regressao na persistencia SQLite.

## TASK-03 - Forcar alteracoes de entidades internas via Aggregate Root

**Prioridade:** Alta

**Problema:** `RequestedItem` e `BillItem` possuem metodos publicos que podem alterar estado fora da Aggregate Root, mesmo que o uso atual da aplicacao seja disciplinado.

**Objetivo:** garantir que alteracoes relevantes em entidades internas passem pela root correta.

**Implementacao esperada:**

- Restringir metodos de mutacao de `RequestedItem` para uso interno pelo dominio, mantendo `AuthorizationRequest` como ponto de entrada para decisao.
- Expor em `HospitalBill` metodos para aplicar glosa e glosa de auditoria em um item:
  - localizar item por id;
  - validar que o item pertence a conta;
  - delegar a criacao da glosa para o item.
- Restringir mutacoes diretas de `BillItem` sempre que a regra exigir controle por `HospitalBill`.
- Manter restauracao de persistencia funcionando sem expor setters publicos.

**Criterios de aceite:**

- Codigo de aplicacao nao chama mais metodos mutadores diretamente em `RequestedItem`.
- Glosas passam por `HospitalBill`, nao por acesso direto ao `BillItem` no fluxo de aplicacao.
- Tentativa de aplicar glosa em item inexistente falha com erro claro.
- As regras de aprovacao integral, parcial e negativa continuam protegidas por `AuthorizationRequest`.

**Validacao minima:**

- Teste de aprovacao parcial continua passando pela root.
- Teste de glosa por item usa `HospitalBill`.
- Teste de item inexistente na conta gera excecao.

## TASK-04 - Enriquecer `HospitalBill` como Aggregate Root

**Prioridade:** Media

**Problema:** `HospitalBill` ainda possui pouco comportamento de negocio. O total e calculado fora do dominio, e nao ha status de fechamento ou bloqueio de alteracoes apos fechamento.

**Objetivo:** aproximar faturamento do mesmo nivel de expressividade ja existente em `AuthorizationRequest`.

**Implementacao esperada:**

- Criar enum `HospitalBillStatus`.
- Adicionar status em `HospitalBill`, iniciando como aberta.
- Adicionar `TotalValue` no dominio.
- Adicionar comportamento `Close()`.
- Impedir `AddItem`, aplicacao de glosa e demais alteracoes quando a conta estiver fechada.
- Persistir status em `hospital_bills`.
- Atualizar `HospitalBillRepository` para salvar e restaurar o status.
- Atualizar `BillingUseCaseSupport` para usar `bill.TotalValue`.

**Criterios de aceite:**

- Conta aberta permite adicionar itens e aplicar glosa.
- Conta fechada rejeita novos itens.
- Conta fechada rejeita novas glosas.
- `HospitalBillDto.TotalValue` vem do dominio.
- Status e preservado apos recarregar do SQLite.

**Validacao minima:**

- Teste de fechamento de conta.
- Teste de bloqueio de alteracao apos fechamento.
- Teste de persistencia do status.

## TASK-05 - Melhorar DTO de solicitacao de autorizacao

**Prioridade:** Media

**Problema:** `AuthorizationRequestDto.MaterialsAndMedicines` usa `List<string>` e o use case transforma todo item em quantidade 1. Alem disso, `ClinicalJustification` recebe um CID, mas o nome sugere texto livre.

**Objetivo:** tornar a interface de entrada mais fiel ao dominio de itens solicitados e CID.

**Implementacao esperada:**

- Criar `RequestedItemDto` com:
  - `Description`;
  - `Quantity`;
  - `ItemType` opcional.
- Substituir `MaterialsAndMedicines` por `RequestedItems` em `AuthorizationRequestDto`.
- Renomear `ClinicalJustification` para `CidCode`.
- Atualizar `RequestAuthorizationUseCase.CreateRequestedItems(...)` para usar quantidade informada.
- Atualizar `UI`, testes e documentacao afetada.

**Criterios de aceite:**

- O DTO de autorizacao nao usa mais `MaterialsAndMedicines`.
- O DTO de autorizacao nao usa mais `ClinicalJustification` para representar CID.
- E possivel solicitar item com quantidade maior que 1.
- Quantidade menor ou igual a zero continua invalida.

**Validacao minima:**

- Teste de aplicacao criando autorizacao com item de quantidade 2 ou mais.
- Teste rejeitando item com quantidade invalida.
- Busca com `rg` confirma que nomes antigos nao aparecem no codigo executavel, exceto em historico ou documentos comparativos.

## TASK-06 - Completar fluxo de recurso administrativo de glosa

**Prioridade:** Media

**Problema:** `Glosa`, `AdministrativeAppeal` e `Evidence` existem no dominio, mas o recurso administrativo e suas evidencias nao sao persistidos nem expostos por caso de uso.

**Objetivo:** fechar o ciclo minimo de recurso administrativo contra glosa.

**Implementacao esperada:**

- Reforcar `AdministrativeAppeal` contra `Guid.Empty` em `id` e `glosaId`.
- Criar DTOs para entrada de recurso administrativo com evidencias.
- Criar use case para registrar recurso em uma glosa de item de conta.
- Expor metodo em `BillingService` ou service especifico de auditoria.
- Adicionar tabelas SQLite para:
  - recurso administrativo;
  - evidencias;
  - status do recurso.
- Atualizar `HospitalBillRepository` para salvar e restaurar glosas com seus recursos.

**Criterios de aceite:**

- Uma glosa pode receber um recurso administrativo com ao menos uma evidencia.
- Recurso sem evidencia e rejeitado.
- Segunda tentativa de recurso para a mesma glosa e rejeitada.
- Recurso, evidencias e status sao recarregados do SQLite.

**Validacao minima:**

- Teste de dominio para validacoes de `AdministrativeAppeal`.
- Teste de aplicacao registrando recurso.
- Teste de persistencia recarregando recurso e evidencias.

## TASK-07 - Padronizar linguagem ubiqua e modulos

**Prioridade:** Media

**Problema:** o projeto mistura pastas em portugues com classes e metodos em ingles. A organizacao tambem parte de tipos tecnicos antes de modulo de negocio.

**Objetivo:** reduzir friccao de linguagem e aproximar o codigo dos modulos de negocio.

**Implementacao esperada:**

- Padronizar identificadores tecnicos em ingles, seguindo o padrao majoritario das classes.
- Reorganizar o dominio por modulos de negocio, preservando comportamento:
  - `Authorizations`;
  - `Billing`;
  - `Audit`;
  - `Beneficiaries`;
  - `Plans`;
  - `Procedures`.
- Atualizar namespaces, `using`, documentacao e diagramas.
- Manter `Glosa` como excecao deliberada, documentada por ser termo forte do dominio brasileiro.

**Criterios de aceite:**

- Namespaces e pastas seguem um criterio consistente.
- O projeto compila sem aliases ou duplicacoes confusas.
- Documentacao explica o criterio de linguagem.
- Nao ha alteracao funcional nesta task.

**Validacao minima:**

- Build e testes passam sem mudanca de comportamento.
- Revisao por `rg` confirma que namespaces antigos nao permanecem no codigo executavel.

## TASK-08 - Adicionar `Money` para faturamento

**Prioridade:** Baixa

**Problema:** valores financeiros sao representados por `decimal` em `BillItem`, DTOs e mapeamentos, deixando moeda e regras de soma implicitas.

**Objetivo:** explicitar valores monetarios como conceito de dominio.

**Implementacao esperada:**

- Criar Value Object `Money` com:
  - `Amount`;
  - `Currency`, com padrao `BRL`;
  - validacao contra valor negativo;
  - validacao contra moeda vazia;
  - operacao de soma que rejeita moedas diferentes.
- Substituir `decimal` por `Money` no dominio de faturamento.
- Decidir formato de DTO: expor `Amount` e `Currency` explicitamente.
- Atualizar persistencia SQLite para salvar valor e moeda.
- Atualizar mapeamentos e testes.

**Criterios de aceite:**

- `BillItem.UnitValue` e `BillItem.TotalValue` usam `Money`.
- `HospitalBill.TotalValue` soma valores monetarios pelo Value Object.
- Moedas diferentes nao podem ser somadas.
- DTOs tornam valor e moeda explicitos.

**Validacao minima:**

- Teste de criacao de `Money`.
- Teste de soma valida.
- Teste de soma com moeda diferente.
- Teste de persistencia de valor e moeda.

## TASK-09 - Formalizar composicao com injecao de dependencia

**Prioridade:** Baixa

**Problema:** `UI/Program.cs` monta repositories e services manualmente. Isso e suficiente para console, mas dificulta evolucao e testes de composicao.

**Objetivo:** centralizar composicao da aplicacao sem mudar a arquitetura de camadas.

**Implementacao esperada:**

- Adicionar pacote de DI apropriado ao projeto `UI`, se necessario.
- Registrar repositories, domain services, use cases e application services.
- Reduzir instanciacao manual em `Program.cs`.
- Manter `UI` como unica camada que conhece implementacoes concretas de `Infra`.

**Criterios de aceite:**

- Console continua executando o fluxo demonstrativo.
- Composicao fica em um bloco claro e centralizado.
- `Application` continua sem referencia para `Infra`.
- `Domain` continua sem dependencia tecnica.

**Validacao minima:**

- `dotnet run --project UI/UI.csproj` executa sem erro.
- Build e testes passam.

## TASK-10 - Atualizar rastreabilidade e diagrama do codigo executavel

**Prioridade:** Media

**Problema:** a documentacao explica bem o modelo, mas ainda falta uma tabela formal regra -> classe/metodo -> teste. Os diagramas tambem misturam codigo atual com evolucoes futuras ANS/TISS.

**Objetivo:** facilitar defesa tecnica e deixar claro o que esta implementado no recorte v1.

**Implementacao esperada:**

- Atualizar `documentacao_modelagem_ddd_saude.md` com uma tabela de rastreabilidade contendo:
  - regra de negocio;
  - classe/metodo;
  - teste correspondente;
  - status: implementada, integrada, futura ou deliberadamente fora do recorte.
- Criar diagrama Mermaid apenas do codigo executavel atual.
- Separar visualmente recorte v1 de evolucoes ANS/TISS v2.
- Atualizar qualquer trecho que ainda sugira que conceitos futuros ja estao implementados.

**Criterios de aceite:**

- O leitor consegue identificar onde cada regra principal esta no codigo.
- Regras existentes mas nao integradas ficam marcadas corretamente ate suas tasks serem implementadas.
- Diagrama v1 mostra apenas objetos executaveis atuais.
- Evolucoes ANS/TISS ficam em secao propria de futuro.

**Validacao minima:**

- Revisar com `rg` termos como `ANS`, `TISS`, `GuiaTISS`, `FaturamentoXML`, `EligibilityService`, `Glosa` e `AdministrativeAppeal`.
- `git diff --check` passa.

## TASK-11 - Atualizar relatorios apos implementacao

**Prioridade:** Media

**Problema:** os relatorios atuais apontam itens como parcialmente atendidos. Depois que as tasks forem implementadas, esses documentos precisarao refletir o novo estado do codigo.

**Objetivo:** manter `relatorio_analise_melhorias_ddd_case1.md` e `resumo_relatorio_analise_melhorias_ddd_case1.md` coerentes com o checkout atualizado.

**Implementacao esperada:**

- Revisar ambos os relatorios contra o codigo real.
- Alterar criterios de "atende parcialmente" para "atende completamente" apenas quando a implementacao realmente justificar.
- Manter pendencias futuras explicitas quando forem escolhas deliberadas de escopo.
- Atualizar conclusao, checklist final e resumo executivo.

**Criterios de aceite:**

- Nenhum item implementado continua descrito como pendente.
- Nenhum item ainda pendente e marcado como atendido.
- Os relatorios continuam distinguindo recorte academico v1 de evolucao futura.

**Validacao minima:**

- Conferir manualmente os checklists dos dois relatorios.
- Rodar `rg -n "parcial|parcialmente|nao integrado|init publico|MaterialsAndMedicines|ClinicalJustification" relatorio_analise_melhorias_ddd_case1.md resumo_relatorio_analise_melhorias_ddd_case1.md`.
- `git diff --check` passa.

## Sequencia recomendada

1. TASK-02
2. TASK-05
3. TASK-01
4. TASK-03
5. TASK-04
6. TASK-06
7. TASK-10
8. TASK-11
9. TASK-08
10. TASK-09
11. TASK-07

Essa ordem reduz risco: primeiro fecha invariantes pequenas, depois melhora contrato de entrada, integra elegibilidade, fortalece aggregates e so entao mexe em documentacao consolidada, dinheiro, DI e reorganizacao estrutural.
