using System;
using System.Threading.Tasks;
using Domain.Procedures;
using Infra.Data;

namespace Infra.Repositories;

public class ProcedureCatalogRepository : IProcedureCatalogRepository
{
    private readonly string _connectionString;

    public ProcedureCatalogRepository()
        : this(HealthInsuranceDatabase.DefaultDatabasePath)
    {
    }

    public ProcedureCatalogRepository(string databasePath)
    {
        _connectionString = HealthInsuranceDatabase.CreateConnectionString(databasePath);
        HealthInsuranceDatabase.InitializeAsync(_connectionString).GetAwaiter().GetResult();
    }

    public async Task<ProcedureCatalogItem?> GetByCodeAsync(ProcedureCode code)
    {
        await using var connection = await HealthInsuranceDatabase.OpenConnectionAsync(_connectionString);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT code,
                   description,
                   type,
                   minimum_age,
                   maximum_age
              FROM procedure_catalog_items
             WHERE code = $code;
            """;
        command.Parameters.AddWithValue("$code", code.Value);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new ProcedureCatalogItem(
            new ProcedureCode(reader.GetString(0)),
            reader.GetString(1),
            Enum.Parse<ProcedureType>(reader.GetString(2)),
            reader.IsDBNull(3) ? null : reader.GetInt32(3),
            reader.IsDBNull(4) ? null : reader.GetInt32(4));
    }

    public async Task AddAsync(ProcedureCatalogItem procedure)
    {
        await using var connection = await HealthInsuranceDatabase.OpenConnectionAsync(_connectionString);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO procedure_catalog_items (
                code,
                description,
                type,
                minimum_age,
                maximum_age
            )
            VALUES (
                $code,
                $description,
                $type,
                $minimumAge,
                $maximumAge
            )
            ON CONFLICT(code) DO UPDATE SET
                description = excluded.description,
                type = excluded.type,
                minimum_age = excluded.minimum_age,
                maximum_age = excluded.maximum_age;
            """;

        command.Parameters.AddWithValue("$code", procedure.Code.Value);
        command.Parameters.AddWithValue("$description", procedure.Description);
        command.Parameters.AddWithValue("$type", procedure.Type.ToString());
        command.Parameters.AddWithValue("$minimumAge", (object?)procedure.MinimumAge ?? DBNull.Value);
        command.Parameters.AddWithValue("$maximumAge", (object?)procedure.MaximumAge ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }
}
