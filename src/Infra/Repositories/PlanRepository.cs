using System;
using System.Globalization;
using System.Threading.Tasks;
using Domain.Common.Enums;
using Domain.Plano;
using Domain.Plano.Enums;
using Domain.Plano.Interfaces;
using Infra.Data;

namespace Infra.Repositories;

public class PlanRepository : IPlanRepository
{
    private readonly string _connectionString;

    public PlanRepository()
        : this(HealthInsuranceDatabase.DefaultDatabasePath)
    {
    }

    public PlanRepository(string databasePath)
    {
        _connectionString = HealthInsuranceDatabase.CreateConnectionString(databasePath);
        HealthInsuranceDatabase.InitializeAsync(_connectionString).GetAwaiter().GetResult();
    }

    public async Task<Plan?> GetByNumberAsync(string number)
    {
        await using var connection = await HealthInsuranceDatabase.OpenConnectionAsync(_connectionString);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id,
                   number,
                   type,
                   copay_percentage
              FROM plans
             WHERE number = $number;
            """;
        command.Parameters.AddWithValue("$number", number);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        var plan = new Plan(
            Guid.Parse(reader.GetString(0)),
            reader.GetString(1),
            Enum.Parse<PlanType>(reader.GetString(2)),
            decimal.Parse(reader.GetString(3), CultureInfo.InvariantCulture));

        await reader.DisposeAsync();
        await LoadGracePeriodsAsync(connection, plan);

        return plan;
    }

    public async Task AddAsync(Plan plan)
    {
        await using var connection = await HealthInsuranceDatabase.OpenConnectionAsync(_connectionString);
        using var transaction = connection.BeginTransaction();

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO plans (
                    id,
                    number,
                    type,
                    copay_percentage
                )
                VALUES (
                    $id,
                    $number,
                    $type,
                    $copayPercentage
                )
                ON CONFLICT(id) DO UPDATE SET
                    number = excluded.number,
                    type = excluded.type,
                    copay_percentage = excluded.copay_percentage;
                """;

            command.Parameters.AddWithValue("$id", plan.Id.ToString());
            command.Parameters.AddWithValue("$number", plan.Number);
            command.Parameters.AddWithValue("$type", plan.Type.ToString());
            command.Parameters.AddWithValue("$copayPercentage", plan.CopayPercentage.ToString(CultureInfo.InvariantCulture));
            await command.ExecuteNonQueryAsync();

            await DeleteGracePeriodsAsync(connection, transaction, plan.Id);
            await InsertGracePeriodsAsync(connection, transaction, plan);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static async Task LoadGracePeriodsAsync(Microsoft.Data.Sqlite.SqliteConnection connection, Plan plan)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT procedure_type,
                   days
              FROM plan_grace_periods
             WHERE plan_id = $planId;
            """;
        command.Parameters.AddWithValue("$planId", plan.Id.ToString());

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            plan.SetGracePeriod(
                Enum.Parse<ProcedureType>(reader.GetString(0)),
                reader.GetInt32(1));
        }
    }

    private static async Task DeleteGracePeriodsAsync(
        Microsoft.Data.Sqlite.SqliteConnection connection,
        Microsoft.Data.Sqlite.SqliteTransaction transaction,
        Guid planId)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM plan_grace_periods WHERE plan_id = $planId;";
        command.Parameters.AddWithValue("$planId", planId.ToString());

        await command.ExecuteNonQueryAsync();
    }

    private static async Task InsertGracePeriodsAsync(
        Microsoft.Data.Sqlite.SqliteConnection connection,
        Microsoft.Data.Sqlite.SqliteTransaction transaction,
        Plan plan)
    {
        foreach (var gracePeriod in plan.GracePeriodsInDays)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO plan_grace_periods (
                    plan_id,
                    procedure_type,
                    days
                )
                VALUES (
                    $planId,
                    $procedureType,
                    $days
                );
                """;

            command.Parameters.AddWithValue("$planId", plan.Id.ToString());
            command.Parameters.AddWithValue("$procedureType", gracePeriod.Key.ToString());
            command.Parameters.AddWithValue("$days", gracePeriod.Value);

            await command.ExecuteNonQueryAsync();
        }
    }
}
