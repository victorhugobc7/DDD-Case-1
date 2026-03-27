class ProfissionalSaude
{
    public string nome { get; set; } = string.Empty;
    public string conselhoRegionalCRM { get; set; } = string.Empty;
    public string numeroRegistro { get; set; } = string.Empty;
    public string especialidade { get; set; } = string.Empty;

    public bool assinarGuia(GuiaTISS g)
    {
        g.assinante = nome;
        return true;
    }
}
