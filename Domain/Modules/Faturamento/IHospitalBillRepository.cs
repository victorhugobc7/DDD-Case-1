using System;
using System.Threading.Tasks;

namespace Domain.Modules.Faturamento;

public interface IHospitalBillRepository
{
    Task<HospitalBill> GetByIdAsync(Guid id);
    Task AddAsync(HospitalBill bill);
}
