using System.Threading.Tasks;
using Domain.Procedures;

namespace Domain.Procedures;

public interface IProcedureCatalogRepository
{
    Task<ProcedureCatalogItem?> GetByCodeAsync(ProcedureCode code);
    Task AddAsync(ProcedureCatalogItem procedure);
}
