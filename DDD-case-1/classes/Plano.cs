class Plano
{
    public string codigoPlano { get; set; } = string.Empty;
    public TipoPlano tipoContratado { get; set; }

    public void verificarCobertura(Procedimento p)
    {
    }

    public void verificarCarencia(Beneficiario b, Procedimento p)
    {
    }
}