class Prestador
{
    public string cnpjCpf { get; set; } = string.Empty;
    public string razaoSocial { get; set; } = string.Empty;
    public string cnes { get; set; } = string.Empty;

    public bool enviarLoteFaturamento(Faturamento f)
    {
        return !string.IsNullOrEmpty(f.numeroFatura);
    }
}
