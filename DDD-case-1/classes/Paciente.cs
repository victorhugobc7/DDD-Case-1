class Paciente
{
	public string carteira { get; set; } = string.Empty;
	public string nome { get; set; } = string.Empty;
    private Beneficiario beneficiario;

    void atribuirBeneficiario( Beneficiario novoBeneficiario)
    {
        this.beneficiario = novoBeneficiario;
    }
}