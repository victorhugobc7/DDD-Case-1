using System;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Interfaces;

public interface IHospitalBillRepository
{
    Task<HospitalBill> GetByIdAsync(Guid id);
    Task AddAsync(HospitalBill bill);
}
