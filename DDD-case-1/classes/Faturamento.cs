class Faturamento
{
	public string numeroFatura { get; set; } = string.Empty;
	public DateTime dataApresentacao { get; set; }
	public double valorTotalCobrado { get; set; }
	public StatusFaturamento status { get; set; }
}