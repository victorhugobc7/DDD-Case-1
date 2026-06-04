using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Domain.Authorizations;
using Domain.Plans;
using Domain.Procedures;
using Domain.ProviderNetwork;
using Infra.Data;
using Microsoft.Data.Sqlite;

namespace Infra.Repositories;

public class AuthorizationRepository : IAuthorizationRepository
{
    private readonly string _connectionString;

    public AuthorizationRepository()
        : this(HealthInsuranceDatabase.DefaultDatabasePath)
    {
    }

    public AuthorizationRepository(string databasePath)
    {
        _connectionString = HealthInsuranceDatabase.CreateConnectionString(databasePath);
        HealthInsuranceDatabase.InitializeAsync(_connectionString).GetAwaiter().GetResult();
    }

    public async Task<AuthorizationRequest?> GetByIdAsync(Guid id)
    {
        await using var connection = await HealthInsuranceDatabase.OpenConnectionAsync(_connectionString);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id,
                   beneficiary_id,
                   plan_number,
                   procedure_code,
                   clinical_justification,
                   requesting_professional,
                   executing_establishment,
                   expected_date,
                   status,
                   is_urgent_or_emergency,
                   requires_post_payment_audit,
                   denial_reason,
                   pending_reason
              FROM authorization_requests
             WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", id.ToString());

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        var authorizationId = Guid.Parse(reader.GetString(0));
        var beneficiaryId = Guid.Parse(reader.GetString(1));
        var planNumber = reader.GetString(2);
        var procedureCode = reader.GetString(3);
        var cidCode = reader.GetString(4);
        var requestingProfessional = reader.GetString(5);
        var executingEstablishment = reader.GetString(6);
        var expectedDate = DateTime.Parse(reader.GetString(7), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        var status = Enum.Parse<AuthorizationStatus>(reader.GetString(8));
        var isUrgentOrEmergency = reader.GetInt32(9) == 1;
        var requiresPostPaymentAudit = reader.GetInt32(10) == 1;
        var denialReason = reader.IsDBNull(11) ? null : reader.GetString(11);
        var pendingReason = reader.IsDBNull(12) ? null : reader.GetString(12);

        await reader.DisposeAsync();

        var items = await LoadItemsAsync(connection, authorizationId);

        return AuthorizationRequest.Restore(
            authorizationId,
            beneficiaryId,
            new PlanNumber(planNumber),
            new ProcedureCode(procedureCode),
            new CidCode(cidCode),
            new ProfessionalRegistry(requestingProfessional),
            executingEstablishment,
            expectedDate,
            items,
            status,
            isUrgentOrEmergency,
            requiresPostPaymentAudit,
            denialReason,
            pendingReason);
    }

    public async Task AddAsync(AuthorizationRequest authorizationRequest)
    {
        await SaveAsync(authorizationRequest);
    }

    public async Task UpdateAsync(AuthorizationRequest authorizationRequest)
    {
        await SaveAsync(authorizationRequest);
    }

    private async Task SaveAsync(AuthorizationRequest authorizationRequest)
    {
        await using var connection = await HealthInsuranceDatabase.OpenConnectionAsync(_connectionString);
        using var transaction = connection.BeginTransaction();

        try
        {
            await UpsertAuthorizationAsync(connection, transaction, authorizationRequest);
            await DeleteItemsAsync(connection, transaction, authorizationRequest.Id);
            await InsertItemsAsync(connection, transaction, authorizationRequest);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static async Task UpsertAuthorizationAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        AuthorizationRequest authorizationRequest)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO authorization_requests (
                id,
                beneficiary_id,
                plan_number,
                procedure_code,
                clinical_justification,
                requesting_professional,
                executing_establishment,
                expected_date,
                status,
                is_urgent_or_emergency,
                requires_post_payment_audit,
                denial_reason,
                pending_reason
            )
            VALUES (
                $id,
                $beneficiaryId,
                $planNumber,
                $procedureCode,
                $cidCode,
                $requestingProfessional,
                $executingEstablishment,
                $expectedDate,
                $status,
                $isUrgentOrEmergency,
                $requiresPostPaymentAudit,
                $denialReason,
                $pendingReason
            )
            ON CONFLICT(id) DO UPDATE SET
                beneficiary_id = excluded.beneficiary_id,
                plan_number = excluded.plan_number,
                procedure_code = excluded.procedure_code,
                clinical_justification = excluded.clinical_justification,
                requesting_professional = excluded.requesting_professional,
                executing_establishment = excluded.executing_establishment,
                expected_date = excluded.expected_date,
                status = excluded.status,
                is_urgent_or_emergency = excluded.is_urgent_or_emergency,
                requires_post_payment_audit = excluded.requires_post_payment_audit,
                denial_reason = excluded.denial_reason,
                pending_reason = excluded.pending_reason;
            """;

        command.Parameters.AddWithValue("$id", authorizationRequest.Id.ToString());
        command.Parameters.AddWithValue("$beneficiaryId", authorizationRequest.BeneficiaryId.ToString());
        command.Parameters.AddWithValue("$planNumber", authorizationRequest.PlanNumber.Value);
        command.Parameters.AddWithValue("$procedureCode", authorizationRequest.ProcedureCode.Value);
        command.Parameters.AddWithValue("$cidCode", authorizationRequest.CidCode.Value);
        command.Parameters.AddWithValue("$requestingProfessional", authorizationRequest.RequestingProfessional.Value);
        command.Parameters.AddWithValue("$executingEstablishment", authorizationRequest.ExecutingEstablishment);
        command.Parameters.AddWithValue("$expectedDate", authorizationRequest.ExpectedDate.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$status", authorizationRequest.Status.ToString());
        command.Parameters.AddWithValue("$isUrgentOrEmergency", authorizationRequest.IsUrgentOrEmergency ? 1 : 0);
        command.Parameters.AddWithValue("$requiresPostPaymentAudit", authorizationRequest.RequiresPostPaymentAudit ? 1 : 0);
        command.Parameters.AddWithValue("$denialReason", (object?)authorizationRequest.DenialReason ?? DBNull.Value);
        command.Parameters.AddWithValue("$pendingReason", (object?)authorizationRequest.PendingReason ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    private static async Task DeleteItemsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid authorizationId)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM authorization_requested_items WHERE authorization_id = $authorizationId;";
        command.Parameters.AddWithValue("$authorizationId", authorizationId.ToString());

        await command.ExecuteNonQueryAsync();
    }

    private static async Task InsertItemsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        AuthorizationRequest authorizationRequest)
    {
        var position = 0;
        foreach (var item in authorizationRequest.Items)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO authorization_requested_items (
                    id,
                    authorization_id,
                    position,
                    description,
                    requested_quantity,
                    approved_quantity
                )
                VALUES (
                    $id,
                    $authorizationId,
                    $position,
                    $description,
                    $requestedQuantity,
                    $approvedQuantity
                );
                """;

            command.Parameters.AddWithValue("$id", item.Id.ToString());
            command.Parameters.AddWithValue("$authorizationId", authorizationRequest.Id.ToString());
            command.Parameters.AddWithValue("$position", position);
            command.Parameters.AddWithValue("$description", item.Description);
            command.Parameters.AddWithValue("$requestedQuantity", item.RequestedQuantity);
            command.Parameters.AddWithValue("$approvedQuantity", item.ApprovedQuantity);

            await command.ExecuteNonQueryAsync();
            position++;
        }
    }

    private static async Task<List<RequestedItem>> LoadItemsAsync(SqliteConnection connection, Guid authorizationId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id,
                   description,
                   requested_quantity,
                   approved_quantity
              FROM authorization_requested_items
             WHERE authorization_id = $authorizationId
             ORDER BY position;
            """;
        command.Parameters.AddWithValue("$authorizationId", authorizationId.ToString());

        var items = new List<RequestedItem>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(RequestedItem.Restore(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetInt32(3)));
        }

        return items;
    }
}
