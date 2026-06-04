using System.Threading.Tasks;
using Domain.Plans;

namespace Domain.Plans;

public interface IPlanRepository
{
    Task<Plan?> GetByNumberAsync(string number);
    Task AddAsync(Plan plan);
}
