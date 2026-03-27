// See https://aka.ms/new-console-template for more information
using System;

var operadora = new OperadoraDeSaude { cnpj = "00.000.000/0001-00", nomeFantasia = "SaudeExemplo" };
var plano = new Plano { codigoPlano = "PL-001", tipoContratado = TipoPlano.Individual };
var beneficiario = new Beneficiario { carteira = "12345", nome = "João Silva", dataAdesao = DateTime.Now.AddDays(-40) };
var procedimento = new Procedimento { codigoTUSS = "001", descricao = "Consulta Generalista", categoria = TipoProcedimento.Consulta, exigeAutorizacaoPrevia = true, valorBase = 200 };
var clinica = new Clinica { cnpj = "11.111.111/1111-11", razaoSocial = "Clinica Central", fazParteRedeCredenciada = true };

beneficiario.verificarStatus();
plano.verificarCarencia(beneficiario, procedimento);
plano.verificarCobertura(procedimento);

var solicitacao = clinica.solicitarAutorizacao(procedimento, beneficiario);
operadora.avaliarAutorizacao(solicitacao, procedimento);

if (solicitacao.status == StatusAutorizacao.Aprovada)
{
	var exec = new ExecucaoProcedimento { idExecucao = Guid.NewGuid().ToString(), dataHoraRealizacao = DateTime.Now };
	exec.registrar();

	var faturamento = new Faturamento { numeroFatura = Guid.NewGuid().ToString(), dataApresentacao = DateTime.Now, valorTotalCobrado = procedimento.valorBase, status = StatusFaturamento.EmAnalise };
	clinica.enviarFaturamento(faturamento, operadora);
	operadora.processarFaturamento(faturamento);
}
else
{
	Console.WriteLine("Solicitação não autorizada. Encerrando fluxo.");
}

Console.WriteLine("Fluxo finalizado.");