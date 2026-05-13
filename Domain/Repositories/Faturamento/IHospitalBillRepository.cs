using System;
using System.Threading.Tasks;
using Domain.Aggregates.Faturamento;

namespace Domain.Repositories.Faturamento;

public interface IHospitalBillRepository
{
    Task<HospitalBill> GetByIdAsync(Guid id);
    Task AddAsync(HospitalBill bill);
}
