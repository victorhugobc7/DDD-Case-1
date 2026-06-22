using System.Threading.Tasks;
using Domain.Procedimento;
using Domain.Common.ValueObjects;

namespace Domain.Procedimento.Interfaces;

public interface IProcedureCatalogRepository
{
    Task<ProcedureCatalogItem?> GetByCodeAsync(ProcedureCode code);
    Task AddAsync(ProcedureCatalogItem procedure);
}
