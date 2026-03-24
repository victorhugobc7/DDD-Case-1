class Procedimento
{
	public string codigoTUSS { get; set; } = string.Empty;
	public string descricao { get; set; } = string.Empty;
	public TipoProcedimento categoria { get; set; }
	public bool exigeAutorizacaoPrevia { get; set; }
	public double valorBase { get; set; }
}