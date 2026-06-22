# Entrega Final do Projeto

## 1. Identificação do projeto

- **Nome do projeto:** DDD-Case-1 (Health Insurance System)
- **Integrantes do grupo:** José Emanuel, Mateus Souza Araújo, Victor Hugo Brito Coelho
- **Link do repositório:** https://github.com/victorhugobc7/DDD-Case-1
- **Tecnologia utilizada:** C#, .NET 10, Minimal APIs, SQLite, HTML/JS/CSS puro (Frontend)
- **Funcionalidade principal desenvolvida:** Sistema de Autorização de Procedimentos Médicos e Gerenciamento de Faturamento Hospitalar (contas, glosas e recursos).

---

## 2. Descrição do case

O case representa um sistema central de uma Operadora de Planos de Saúde. O problema de negócio consiste em avaliar e orquestrar as solicitações de procedimentos médicos (guias de autorização) feitas em nome dos beneficiários, considerando elegibilidade, carência e restrições etárias. Após a realização dos procedimentos, o sistema lida com o ciclo do faturamento, avaliando as cobranças hospitalares, aplicando cortes financeiros (glosas) quando são detectadas irregularidades e oferecendo a possibilidade da rede prestadora apresentar defesas (recursos administrativos) contendo evidências em anexo.

---

## 3. Estado do projeto antes da análise externa

Antes das refatorações (Entregável 02), o projeto possuía uma divisão em pastas que agrupava conceitos por tema de negócio de forma horizontal (como `Audit`, `Billing`, `Authorizations`), e não formava efetivamente fronteiras de Aggregates. 
- As entidades não estendiam de uma classe `Entity` unificada, resultando em repetição de lógicas de identificadores e validação.
- Não existia um tipo base para garantir o comportamento de `Value Object`.
- A persistência (Infra) muitas vezes encontrava dificuldades em identificar quais objetos eram raízes de persistência.

---

## 4. Alterações realizadas pelo outro grupo

As alterações foram feitas pelo nosso próprio grupo. analizamos o feedback do professor, como a estruturação de pasta e a nomenclatura dos arquivos para evoluir o projeto.

---

## 5. Avaliação das alterações recebidas

- **Quais alterações foram mantidas:** Todas as refatorações arquiteturais propostas foram mantidas. A organização em Aggregates e a hierarquia orientada pela herança de `AggregateRoot` organizaram o fluxo de dependências do UseCase -> Domain.
- **Quais alterações foram modificadas:** Nenhuma mudança drástica nas ideias originais foi feita, mas atualizamos todas as camadas superiores (`Application`, `Infra` e `Api`) para refletir e depender adequadamente da nova estrutura de namespaces sem comprometer a estabilidade do sistema.
- **Quais alterações foram rejeitadas:** Nenhuma das propostas arquiteturais foi rejeitada.
- **Justificativas:** O modelo resultante ficou imensamente mais claro, fácil de debugar e escalável. O isolamento de sub-entidades (como `BillItem` só sendo acessada através de `HospitalBill`) protege o sistema da corrupção de dados e inconsistências transacionais.

---

## 6. Melhorias adicionais realizadas pelo grupo original

- Desenvolvimento de um **Frontend Minimalista (UI)** usando HTML/JS/CSS puro injetado diretamente na API para facilitar a usabilidade.
- Substituição da coloração do sistema por uma estética mais "Light" (Neumorphism), refletindo melhor a seriedade de um sistema do setor de saúde, e isolamento de rotas e funcionalidades em permissões do paciente/operador vs. faturamento.
- Ajustes de estabilidade envolvendo as chaves únicas dos Planos para corrigir exceções transacionais na criação dos dados (*seed*).

---

## 7. Linguagem Ubíqua

- **Guia / Solicitação (AuthorizationRequest):** Pedido efetuado pelo paciente ou profissional para que a operadora de saúde custeie a execução de procedimentos médicos.
- **Glosa (Glosa):** Negativa total ou parcial de pagamento de um item de uma fatura hospitalar por parte da operadora (por conta de erro de faturamento ou quebra de regras contratuais).
- **Recurso Administrativo (AdministrativeAppeal):** Defesa enviada pelo hospital contra uma glosa aplicada, sempre devendo conter `Evidences` (documentos).
- **Beneficiário (Beneficiary):** Cliente portador do plano de saúde.
- **Fatura Hospitalar (HospitalBill):** A conta consolidada enviada por um prestador de serviços à operadora, que agrupa itens e pode sofrer glosas.

---

## 8. Módulos

1. **Beneficiario:** Módulo encarregado por gerenciar o ciclo de vida do segurado (Ativação, Suspensão, Cálculo de Idade).
2. **Plano:** Gestão de planos comerciais e suas respectivas tabelas de Carência.
3. **Procedimento:** Catálogo "ReadOnly" dos procedimentos médicos cobertos, restrições e faixas de idade permitidas.
4. **Solicitacao:** Emissão, avaliação, aprovação (integral/parcial) ou recusa das guias.
5. **Faturamento:** Faturamento das solicitações prontas, e ciclo da auditoria em forma de Glosa e Recurso.

---

## 9. Entities

- **RequestedItem**
  - **Identidade:** Possui um `Guid Id` único gerado na inclusão.
  - **Responsabilidades:** Registrar o que foi solicitado e guardar quanto foi de fato aprovado.
  - **Comportamentos/Regras:** Limita a quantidade aprovada à solicitada.
  - **Ciclo de vida:** Nasce e morre atrelado a um `AuthorizationRequest`.
  - **Justificativa:** Podem existir vários itens na guia e os analistas podem aprovar alguns e negar outros, eles precisam possuir status independentes entre si, exigindo identidade.
- **BillItem** (Item de Fatura) e **Glosa** / **AdministrativeAppeal**: Seguem a mesma premissa. São vitais, mutáveis em estado e dependem exclusivamente da raiz para serem gerenciados, mantendo assim o ciclo de persistência restrito.

---

## 10. Value Objects

- **Money**, **CidCode**, **ProcedureCode**, **PlanNumber**, **ProfessionalRegistry**, **Evidence**.
- **Atributos & Validações:** Exemplo: `Money` contém um `decimal Amount`. Não pode receber valores corrompidos.
- **Regras protegidas:** São imutáveis. Operações monetárias sobre `Money` geram uma nova instância.
- **Critérios de igualdade:** São avaliados como iguais baseados em seus valores e não em uma chave de ID (C# `record` garante igualdade estrutural automática).
- **Justificativa:** Reduz a "Obsessão por Tipos Primitivos" espalhada nas regras de negócio. Evita que um número inteiro qualquer se passe por um `ProcedureCode`.

---

## 11. Aggregates e Aggregate Roots

**Aggregate de Faturamento:**
- **Aggregate Root:** `HospitalBill`.
- **Objetos Internos:** `BillItem`, `Glosa`, `AdministrativeAppeal`, `Evidence` (VO), `Money` (VO).
- **Fronteira de consistência:** Garante que recursos só existam contra glosas válidas e pertencentes à mesma fatura.
- **Invariantes:** Uma fatura fechada não pode receber novas glosas; valores glosados não podem ser maiores que o valor cobrado do item.
- **Justificativa da modelagem:** Manter toda a parte contábil/clínica atrelada diretamente à raiz (`HospitalBill`) resolve cenários onde se glosava um item fora de contexto ou duplicava auditorias incorretamente.

**Demais Aggregates:** `Solicitacao`, `Beneficiario`, `Plano`, `Procedimento` operam como Aggregates e Aggregate Roots por conta própria devido à falta de complexidade profunda entre grafos filhados.

---

## 12. Factories

- **AuthorizationRequestFactory:** Utilizada dentro do UseCase para criação de Guias. A Factory isola a complexidade que existe em receber DTOs que chegam da API, montar todos os Value Objects (`PlanNumber`, `ProcedureCode`, `CidCode`, etc) e então instanciar o Request de forma segura e atômica. Se um dos códigos do pedido for inválido, o processo falha internamente sem sujar a Aggregate base.

---

## 13. Domain Services

- **AuthorizationEligibilityValidator:** Recebe entidades de domínios diferentes (`Beneficiary`, `Plan`, e `ProcedureCatalogItem`) para definir uma lógica de negócio conjunta (Aprovação de elegibilidade/carência baseada em datas e idades). Essa lógica não cabia no `Beneficiary` pois este não deve conhecer catálogos médicos, nem caberia no `Procedure` pois ele não entende sobre o tempo de carência de um plano. O Domain Service unifica os três componentes independentes.

---

## 14. Repositories

Foram estabelecidas as abstrações `IAuthorizationRepository`, `IHospitalBillRepository`, `IPlanRepository`, `IBeneficiaryRepository`, e `IProcedureCatalogRepository`.
- **Separação:** Ficam declaradas no Domain/Application, e são implementadas explicitamente em `Infra/Repositories/`.
- **Persistência:** Limitada aos Aggregate Roots. É impossível buscar um `RequestedItem` direto na base; o repositório é forçado a buscar a Solicitacao inteira, aplicar a modificação, e persistir de volta.

---

## 15. Regras de negócio

| Regra de negócio | Classe responsável | Forma de proteção |
|---|---|---|
| Paciente com carência ativa tem guia negada | `AuthorizationEligibilityValidator` | Lança exception/validação no momento que intercruza Plano vs Beneficiário |
| Fatura fechada não pode sofrer nova glosa | `HospitalBill` | `ApplyGlosa` verifica estado do Enum (`HospitalBillStatus.Closed`) no inicio da função |
| Quantidade aprovada não supera a pedida | `RequestedItem` | Exceção de negócio na chamada da aprovação. |
| O ID da entidade nunca pode ser vazio | `Entity` (Classe Base) | Condicional de guarda no construtor genérico (`Guid.Empty`). |

---

## 16. Aplicação de Supple Design

- Adotamos um padrão pesado de **Side-Effect Free Functions** ao encapsular métricas quantitativas dentro do VO `Money` (cálculos geram novas instâncias).
- Os repositórios retornam classes de domínio com estado já populado sem `Setters` públicos abertos. O modelo é inteiramente encapsulado (**Intention-Revealing Interfaces**) fornecendo métodos que ditam regras claras (ex: `CloseBill()` em vez de `Status = Status.Closed`).
- Reuso generalizado por polimorfismo das classes de núcleo (`AggregateRoot`, `Entity`, `ValueObject`).

---

## 17. Arquitetura final

Foi implementada uma "Onion/Clean Architecture" simplificada:
- **Domain (`src/Domain`):** Não possui referências para nenhum outro local do sistema. Classes de negócio puras, interfaces de serviços de domínio.
- **Application (`src/Application`):** Regras de orquestração. Guarda os DTOs e os `UseCases`. Importa o Domínio.
- **Infrastructure (`src/Infra`):** Implementa acesso aos dados. Puxa os dados com a biblioteca C# `Microsoft.Data.Sqlite`, remonta o AggregateRoot e injeta nas regras do Domínio.
- **API/UI (`src/Api`):** Camada exposta (REST via ASP.NET Core Minimal APIs). Executa roteamento, serialização (CORS/JSON), e provê a interface HTML diretamente pelo `PhysicalFileProvider`.

---

## 18. Diagrama do modelo de domínio
!(chrome_2F8LPQWaLy.png)

---

## 19. Testes e validações realizadas

- **Funcionalidades testadas:** Fluxo completo desde a emissão da guia médica, aprovação parcial/total, faturamento no sistema da clínica, submissão de glosa e emissão do recurso da glosa.
- **Testes Automatizados:** O projeto inclui um projeto isolado em `tests/` (`Program.cs`) que roda um conjunto de simulações orientadas aos métodos dos domínios antigos e lógicas centrais para assegurar que nada foi quebrado.
- **Validações de Integração Manuais:** Uso de `Swagger` e do mini client (frontend) gerado na UI para validar interações E2E (End-to-End) na API via Post. Restaurações e persistência em SQLite foram validadas no console do banco local `health-insurance.db`.

---

## 20. Instruções para execução

Para rodar o projeto, será necessário ter o **.NET SDK** (recomendado versão 10, conforme o `TargetFramework` da aplicação) e um terminal.

1. Navegue até a pasta base da API através do terminal:
   ```bash
   cd src/Api
   ```
2. Execute o projeto usando o comando principal do .NET:
   ```bash
   dotnet run
   ```
3. O servidor subirá e hospedará a API nas portas padrões do Kestrel (ex: `http://localhost:5000`).
4. Para acessar a Interface da Aplicação, acesse diretamente no browser a URL levantada acompanhada do arquivo inicial estático (se injetado). Apenas bater em `/api/seed` em uma requisição `POST` popula o banco de dados temporário.

---

## 21. Limitações e trabalhos futuros

- **Persistência Profunda:** A montagem e desmontagem dos Aggregates via instrução SQL pura na camada de repositório pode ficar muito prolixa e dura para realizar manutenções. O próximo passo lógico é adoção do `Entity Framework Core` com Mapeamento Fluente para manter o domínio limpo.
- **Autenticação:** Atualmente o sistema confia no ID passado livremente na chamada (falta de JWT/Identity Server real).
- **Notificações em Eventos de Domínio (Domain Events):** Os Aggregates, ao mudarem de status (ex: Guia Aprovada), deveriam disparar eventos (`IDomainEvent`) processados pelo MediatR em background, em vez da camada de aplicação forçar comandos diretos subsequentes.

---

## 22. Conclusão

Através da entrega das 3 etapas deste case, absorvemos a complexidade da refatoração em um domínio pesado. Pudemos compreender como a "Linguagem Ubíqua" tem mais peso na arquitetura de software do que o banco de dados em si. O processo de limpeza do `Application` layer transformando-o num mero coordenador, injetando todo o trabalho lógico da empresa de forma rica dentro de `Entities` e `Value Objects`, resultou em um sistema extremamente resiliente e blindado contra interações espúrias que pudessem quebrar fluxos de Faturamento e Auditoria médica.
