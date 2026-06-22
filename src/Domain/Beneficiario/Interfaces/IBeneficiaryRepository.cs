using System;
using System.Threading.Tasks;
using Domain.Beneficiario;

namespace Domain.Beneficiario.Interfaces;

public interface IBeneficiaryRepository
{
    Task<Beneficiary?> GetByIdAsync(Guid id);
    Task AddAsync(Beneficiary beneficiary);
}
