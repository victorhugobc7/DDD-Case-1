using System;

class Plano
{
    public string codigoPlano { get; set; } = string.Empty;
    public TipoPlano tipoContratado { get; set; }

    public bool verificarCobertura(Procedimento p)
    {
        return !string.IsNullOrEmpty(p.codigoTUSS);
    }

    public bool verificarCarencia(Beneficiario b, Procedimento p)
    {
        var dias = (DateTime.Now - b.dataAdesao).TotalDays;
        return dias >= 30;
    }
}