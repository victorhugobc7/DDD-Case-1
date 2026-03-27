class FaturamentoXML
{
    public string numeroLote { get; set; } = string.Empty;
    public System.DateTime dataEnvio { get; set; }
    public double valorTotalApresentado { get; set; }
    public StatusFaturamento status { get; set; }

    public bool submeter()
    {
        return !string.IsNullOrEmpty(numeroLote);
    }
}
