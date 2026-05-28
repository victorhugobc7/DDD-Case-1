using System;
using System.Threading.Tasks;
using Domain.Billing;

namespace Domain.Billing;

public interface IHospitalBillRepository
{
    Task<HospitalBill?> GetByIdAsync(Guid id);
    Task AddAsync(HospitalBill bill);
    Task UpdateAsync(HospitalBill bill);
}
