class Auditoria
{
    public System.DateTime dataAuditoria { get; set; }
    public string parecerMedico { get; set; } = string.Empty;
    public string decisao { get; set; } = string.Empty;

    public bool avaliarPertinenciaMedica()
    {
        return !string.IsNullOrEmpty(parecerMedico);
    }
}
