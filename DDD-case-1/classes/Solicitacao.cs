class Solicitacao
{
    public string numeroProtocolo { get; set; } = string.Empty;
    public DateTime dataSolicitacao { get; set; }
    public CaraterAtendimento carater { get; set; }
    public StatusAutorizacao status { get; set; }
    public string justificativaMedica { get; set; } = string.Empty;
    public string dataPrevista { get; set; } = string.Empty;

    public Plano plano { get; set; }

    private Beneficiario solicitante;

    public void aprovar()
    {
    }

    public void negar()
    {
    }

    public Solicitacao(Plano plano, String justificativaMedica, Beneficiario solicitante, String dataPrevista)
    {
        this.plano = plano;
        this.justificativaMedica = justificativaMedica;
        this.solicitante = solicitante;
        this.dataPrevista = dataPrevista;
    }

    //Regra de negócio - Verificar se o paciente pode pegar os remédios relacionados aquele procedimento
    public void verificarPatologia()
    {
        
    }
}