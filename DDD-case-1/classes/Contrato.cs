class Contrato
{
    public string numeroContrato { get; set; } = string.Empty;
    public System.DateTime dataInicio { get; set; }
    public System.DateTime dataFim { get; set; }
    public bool ativo { get; set; }

    public bool estaVigente()
    {
        var hoje = System.DateTime.Now;
        return ativo && hoje >= dataInicio && hoje <= dataFim;
    }
}
