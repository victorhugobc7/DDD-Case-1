class GuiaTISS
{
    public string numeroGuia { get; set; } = string.Empty;
    public string tipoGuia { get; set; } = string.Empty;
    public string assinante { get; set; } = string.Empty;
    public StatusAutorizacao status { get; set; }

    public void autorizar()
    {
        status = StatusAutorizacao.Aprovada;
    }

    public void cancelar()
    {
        status = StatusAutorizacao.Negada;
    }
}
