using System;

class Clinica
{
	public string cnpj { get; set; } = string.Empty;
	public string razaoSocial { get; set; } = string.Empty;
	public bool fazParteRedeCredenciada { get; set; }

	public SolicitacaoAutorizacao solicitarAutorizacao(Procedimento p, Beneficiario b)
	{
		var s = new SolicitacaoAutorizacao { numeroProtocolo = Guid.NewGuid().ToString(), dataSolicitacao = DateTime.Now, status = StatusAutorizacao.Pendente };
		return s;
	}

	public void enviarFaturamento(Faturamento f, OperadoraDeSaude op)
	{
		op.processarFaturamento(f);
	}
}