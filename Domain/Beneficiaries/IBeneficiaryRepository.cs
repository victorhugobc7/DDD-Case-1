using System;
using System.Threading.Tasks;
using Domain.Beneficiaries;

namespace Domain.Beneficiaries;

public interface IBeneficiaryRepository
{
    Task<Beneficiary?> GetByIdAsync(Guid id);
    Task AddAsync(Beneficiary beneficiary);
}
