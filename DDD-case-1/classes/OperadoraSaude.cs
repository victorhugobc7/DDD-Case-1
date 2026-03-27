using System;

class OperadoraSaude
{
	public string cnpj { get; set; } = string.Empty;
	public string nomeFantasia { get; set; } = string.Empty;

	public void processarFaturamento(Faturamento f)
	{
		if (f.valorTotalCobrado <= 0)
		{
			f.status = StatusFaturamento.Glosado;
		}
		else
		{
			f.status = StatusFaturamento.Aprovado;
		}
	}

	public void avaliarAutorizacao(SolicitacaoAutorizacao s, Procedimento p)
	{
		if (p.valorBase <= 1000)
		{
			s.aprovar();
		}
		else
		{
			s.negar();
		}
	}
}