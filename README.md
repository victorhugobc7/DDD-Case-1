# Caso de Negocio: Operadora de Saude

Este projeto modela um dominio de operadora de saude com foco em autorizacao, faturamento, glosa, recursos e auditoria.

## 1. Contexto da Operadora e Planos

Uma operadora de saude atende milhares de beneficiarios distribuidos em diferentes planos:

- Individual
- Empresarial
- Coletivo por adesao

Cada plano possui regras proprias de:

- Cobertura
- Carencia
- Rede credenciada
- Limites anuais ou por procedimento

Quando um paciente necessita de um procedimento (consulta, exame, cirurgia, terapia ou internacao), a clinica pode ou nao precisar solicitar uma autorizacao previa.

Algumas situacoes sao consideradas urgencia ou emergencia, permitindo execucao sem autorizacao formal. Isso nao garante pagamento posterior.

## 2. Solicitacao de Autorizacao

A solicitacao de autorizacao contem:

- Dados do paciente
- Numero do plano
- Codigo do procedimento
- Justificativa clinica (CID, laudo, relatorio medico)
- Profissional solicitante
- Estabelecimento executante
- Data prevista
- Materiais e medicamentos previstos

Uma autorizacao pode:

- Ser aprovada integralmente
- Ser aprovada parcialmente (exemplo: apenas 3 das 10 sessoes solicitadas)
- Ser negada com justificativa
- Ficar pendente por solicitacao de documentos adicionais

## 3. Conta Hospitalar e Glosa

Apos a execucao do procedimento, a clinica envia uma conta hospitalar contendo:

- Itens executados
- Quantidades
- Valores
- Taxas
- Diarias
- Materiais
- Medicamentos
- Honorarios

Durante a analise da conta, pode ocorrer glosa, que e a recusa total ou parcial de um item por diversos motivos:

- Procedimento nao autorizado
- Divergencia de codigo
- Excesso de quantidade
- Falta de documentacao
- Procedimento fora da cobertura contratual

## 4. Recursos e Auditoria

A clinica pode aceitar a glosa ou abrir um recurso administrativo, anexando novos documentos.

O recurso pode:

- Manter a decisao original
- Reverter a decisao original

Meses depois, uma auditoria interna pode revisar autorizacoes ja concedidas e contas ja pagas, gerando cobrancas retroativas.

## 5. Regras Adicionais

- Cada plano tem carencias especificas.
- Alguns procedimentos dependem de idade minima ou maxima.
- Certos materiais sao permitidos apenas para determinadas patologias.
- Existe franquia ou coparticipacao para alguns planos.
- Um beneficiario pode trocar de plano, mas certas carencias permanecem.
- Existem reembolsos quando o atendimento ocorre fora da rede.

