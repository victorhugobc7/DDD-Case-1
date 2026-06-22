using System.Threading.Tasks;
using Domain.Plano;

namespace Domain.Plano.Interfaces;

public interface IPlanRepository
{
    Task<Plan?> GetByNumberAsync(string number);
    Task AddAsync(Plan plan);
}
