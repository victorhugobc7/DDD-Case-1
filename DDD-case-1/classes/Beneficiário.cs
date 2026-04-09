class Beneficiario
{
	public string carteira { get; set; } = string.Empty;
	public string nome { get; set; } = string.Empty;
	public DateTime dataAdesao { get; set; }
	
	private Plano plano;
	private Solicitacao[] solicitacoesAtivas;

	public void verificarStatus()
	{
		//TODO: Verificar se está ativo aq
	}

	public void trocarPlano(Plano novoPlano)
	{
		this.plano = novoPlano;
	}

	public void verificarReembolso(Solicitacao solicitacaoParaReembolsar)
	{
		//TODO: verificar como será feito o reembolso
	}

	public void criarNovaSolicitação()
	{
		//solicitacoesAtivas.Append(new Solicitacao(
			//Aqui irão os dados da solicitação
		//));
	}


}