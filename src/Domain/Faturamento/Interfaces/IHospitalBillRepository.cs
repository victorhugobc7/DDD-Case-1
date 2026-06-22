using System;
using System.Threading.Tasks;
using Domain.Faturamento;

namespace Domain.Faturamento.Interfaces;

public interface IHospitalBillRepository
{
    Task<HospitalBill?> GetByIdAsync(Guid id);
    Task AddAsync(HospitalBill bill);
    Task UpdateAsync(HospitalBill bill);
}
