using System;

class SolicitacaoAutorizacao
{
    public string numeroProtocolo { get; set; } = string.Empty;
    public DateTime dataSolicitacao { get; set; }
    public CaraterAtendimento carater { get; set; }
    public StatusAutorizacao status { get; set; }
    public string justificativaMedica { get; set; } = string.Empty;

    public void aprovar()
    {
        status = StatusAutorizacao.Aprovada;
    }

    public void negar()
    {
        status = StatusAutorizacao.Negada;
    }
}