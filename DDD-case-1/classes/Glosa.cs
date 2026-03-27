class Glosa
{
	public string codigoMotivo { get; set; } = string.Empty;
	public string descricaoMotivo { get; set; } = string.Empty;
	public double valorGlosado { get; set; }

	public double aplicarGlosa(double valor)
	{
		return Math.Min(valor, valorGlosado);
	}
}