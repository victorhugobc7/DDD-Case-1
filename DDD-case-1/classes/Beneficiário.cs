class Beneficiario
{
	public string carteira { get; set; } = string.Empty;
	public string nome { get; set; } = string.Empty;
	public DateTime dataAdesao { get; set; }

	public bool verificarStatus()
	{
		var ativo = (DateTime.Now - dataAdesao).TotalDays >= 0;
		return ativo;
	}
}