using System;
using System.Globalization;
using System.Threading.Tasks;
using Domain.Beneficiario;
using Domain.Beneficiario.Enums;
using Domain.Beneficiario.Interfaces;
using Infra.Data;

namespace Infra.Repositories;

public class BeneficiaryRepository : IBeneficiaryRepository
{
    private readonly string _connectionString;

    public BeneficiaryRepository()
        : this(HealthInsuranceDatabase.DefaultDatabasePath)
    {
    }

    public BeneficiaryRepository(string databasePath)
    {
        _connectionString = HealthInsuranceDatabase.CreateConnectionString(databasePath);
        HealthInsuranceDatabase.InitializeAsync(_connectionString).GetAwaiter().GetResult();
    }

    public async Task<Beneficiary?> GetByIdAsync(Guid id)
    {
        await using var connection = await HealthInsuranceDatabase.OpenConnectionAsync(_connectionString);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id,
                   name,
                   birth_date,
                   join_date,
                   plan_id,
                   status
              FROM beneficiaries
             WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", id.ToString());

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new Beneficiary(
            Guid.Parse(reader.GetString(0)),
            reader.GetString(1),
            DateTime.Parse(reader.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            DateTime.Parse(reader.GetString(3), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            Guid.Parse(reader.GetString(4)),
            Enum.Parse<BeneficiaryStatus>(reader.GetString(5)));
    }

    public async Task AddAsync(Beneficiary beneficiary)
    {
        await using var connection = await HealthInsuranceDatabase.OpenConnectionAsync(_connectionString);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO beneficiaries (
                id,
                name,
                birth_date,
                join_date,
                plan_id,
                status
            )
            VALUES (
                $id,
                $name,
                $birthDate,
                $joinDate,
                $planId,
                $status
            )
            ON CONFLICT(id) DO UPDATE SET
                name = excluded.name,
                birth_date = excluded.birth_date,
                join_date = excluded.join_date,
                plan_id = excluded.plan_id,
                status = excluded.status;
            """;

        command.Parameters.AddWithValue("$id", beneficiary.Id.ToString());
        command.Parameters.AddWithValue("$name", beneficiary.Name);
        command.Parameters.AddWithValue("$birthDate", beneficiary.BirthDate.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$joinDate", beneficiary.JoinDate.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$planId", beneficiary.PlanId.ToString());
        command.Parameters.AddWithValue("$status", beneficiary.Status.ToString());

        await command.ExecuteNonQueryAsync();
    }
}
