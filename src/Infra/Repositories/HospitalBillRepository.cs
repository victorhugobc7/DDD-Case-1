using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Domain.Common.Enums;
using Domain.Common.ValueObjects;
using Domain.Faturamento;
using Domain.Faturamento.Entities;
using Domain.Faturamento.Enums;
using Domain.Faturamento.Interfaces;
using Domain.Faturamento.ValueObjects;
using Infra.Data;
using Microsoft.Data.Sqlite;

namespace Infra.Repositories;

public class HospitalBillRepository : IHospitalBillRepository
{
    private readonly string _connectionString;

    public HospitalBillRepository()
        : this(HealthInsuranceDatabase.DefaultDatabasePath)
    {
    }

    public HospitalBillRepository(string databasePath)
    {
        _connectionString = HealthInsuranceDatabase.CreateConnectionString(databasePath);
        HealthInsuranceDatabase.InitializeAsync(_connectionString).GetAwaiter().GetResult();
    }

    public async Task<HospitalBill?> GetByIdAsync(Guid id)
    {
        await using var connection = await HealthInsuranceDatabase.OpenConnectionAsync(_connectionString);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id,
                   beneficiary_id,
                   executing_establishment,
                   status
              FROM hospital_bills
             WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", id.ToString());

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        var billId = Guid.Parse(reader.GetString(0));
        var beneficiaryId = Guid.Parse(reader.GetString(1));
        var executingEstablishment = reader.GetString(2);
        var status = Enum.Parse<HospitalBillStatus>(reader.GetString(3));

        await reader.DisposeAsync();

        var items = await LoadItemsAsync(connection, billId);
        return HospitalBill.Restore(billId, beneficiaryId, executingEstablishment, status, items);
    }

    public async Task AddAsync(HospitalBill bill)
    {
        await SaveAsync(bill);
    }

    public async Task UpdateAsync(HospitalBill bill)
    {
        await SaveAsync(bill);
    }

    private async Task SaveAsync(HospitalBill bill)
    {
        await using var connection = await HealthInsuranceDatabase.OpenConnectionAsync(_connectionString);
        using var transaction = connection.BeginTransaction();

        try
        {
            await UpsertBillAsync(connection, transaction, bill);
            await DeleteItemsAsync(connection, transaction, bill.Id);
            await InsertItemsAsync(connection, transaction, bill);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static async Task UpsertBillAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        HospitalBill bill)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO hospital_bills (
                id,
                beneficiary_id,
                executing_establishment,
                status
            )
            VALUES (
                $id,
                $beneficiaryId,
                $executingEstablishment,
                $status
            )
            ON CONFLICT(id) DO UPDATE SET
                beneficiary_id = excluded.beneficiary_id,
                executing_establishment = excluded.executing_establishment,
                status = excluded.status;
            """;

        command.Parameters.AddWithValue("$id", bill.Id.ToString());
        command.Parameters.AddWithValue("$beneficiaryId", bill.BeneficiaryId.ToString());
        command.Parameters.AddWithValue("$executingEstablishment", bill.ExecutingEstablishment);
        command.Parameters.AddWithValue("$status", bill.Status.ToString());

        await command.ExecuteNonQueryAsync();
    }

    private static async Task DeleteItemsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid billId)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM hospital_bill_items WHERE hospital_bill_id = $billId;";
        command.Parameters.AddWithValue("$billId", billId.ToString());

        await command.ExecuteNonQueryAsync();
    }

    private static async Task InsertItemsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        HospitalBill bill)
    {
        var position = 0;
        foreach (var item in bill.Items)
        {
            await InsertItemAsync(connection, transaction, bill.Id, item, position);
            await InsertGlosasAsync(connection, transaction, item);
            position++;
        }
    }

    private static async Task InsertItemAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid billId,
        BillItem item,
        int position)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO hospital_bill_items (
                id,
                hospital_bill_id,
                position,
                approved_authorization_id,
                description,
                quantity,
                unit_value,
                unit_currency
            )
            VALUES (
                $id,
                $billId,
                $position,
                $approvedAuthorizationId,
                $description,
                $quantity,
                $unitValue,
                $unitCurrency
            );
            """;

        command.Parameters.AddWithValue("$id", item.Id.ToString());
        command.Parameters.AddWithValue("$billId", billId.ToString());
        command.Parameters.AddWithValue("$position", position);
        command.Parameters.AddWithValue("$approvedAuthorizationId", item.ApprovedAuthorizationId.ToString());
        command.Parameters.AddWithValue("$description", item.Description);
        command.Parameters.AddWithValue("$quantity", item.Quantity);
        command.Parameters.AddWithValue("$unitValue", item.UnitValue.Amount.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$unitCurrency", item.UnitValue.Currency);

        await command.ExecuteNonQueryAsync();
    }

    private static async Task InsertGlosasAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        BillItem item)
    {
        foreach (var glosa in item.Glosas)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO hospital_bill_item_glosas (
                    id,
                    hospital_bill_item_id,
                    reason,
                    details,
                    is_clawback
                )
                VALUES (
                    $id,
                    $hospitalBillItemId,
                    $reason,
                    $details,
                    $isClawback
                );
                """;

            command.Parameters.AddWithValue("$id", glosa.Id.ToString());
            command.Parameters.AddWithValue("$hospitalBillItemId", item.Id.ToString());
            command.Parameters.AddWithValue("$reason", glosa.Reason.ToString());
            command.Parameters.AddWithValue("$details", glosa.Details);
            command.Parameters.AddWithValue("$isClawback", glosa.IsClawback ? 1 : 0);

            await command.ExecuteNonQueryAsync();

            if (glosa.Appeal != null)
                await InsertAppealAsync(connection, transaction, glosa.Appeal);
        }
    }

    private static async Task InsertAppealAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        AdministrativeAppeal appeal)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO administrative_appeals (
                id,
                glosa_id,
                status
            )
            VALUES (
                $id,
                $glosaId,
                $status
            );
            """;

        command.Parameters.AddWithValue("$id", appeal.Id.ToString());
        command.Parameters.AddWithValue("$glosaId", appeal.GlosaId.ToString());
        command.Parameters.AddWithValue("$status", appeal.Status.ToString());
        await command.ExecuteNonQueryAsync();

        var position = 0;
        foreach (var evidence in appeal.EvidenceDocuments)
        {
            await using var evidenceCommand = connection.CreateCommand();
            evidenceCommand.Transaction = transaction;
            evidenceCommand.CommandText = """
                INSERT INTO administrative_appeal_evidence (
                    id,
                    appeal_id,
                    position,
                    document_url,
                    description
                )
                VALUES (
                    $id,
                    $appealId,
                    $position,
                    $documentUrl,
                    $description
                );
                """;

            evidenceCommand.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
            evidenceCommand.Parameters.AddWithValue("$appealId", appeal.Id.ToString());
            evidenceCommand.Parameters.AddWithValue("$position", position);
            evidenceCommand.Parameters.AddWithValue("$documentUrl", evidence.DocumentUrl);
            evidenceCommand.Parameters.AddWithValue("$description", evidence.Description);
            await evidenceCommand.ExecuteNonQueryAsync();

            position++;
        }
    }

    private static async Task<List<BillItem>> LoadItemsAsync(SqliteConnection connection, Guid billId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id,
                   approved_authorization_id,
                   description,
                   quantity,
                   unit_value,
                   unit_currency
              FROM hospital_bill_items
             WHERE hospital_bill_id = $billId
             ORDER BY position;
            """;
        command.Parameters.AddWithValue("$billId", billId.ToString());

        var itemRows = new List<(Guid Id, Guid ApprovedAuthorizationId, string Description, int Quantity, Money UnitValue)>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            itemRows.Add((
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                reader.GetString(2),
                reader.GetInt32(3),
                new Money(
                    decimal.Parse(reader.GetString(4), CultureInfo.InvariantCulture),
                    reader.GetString(5))));
        }

        await reader.DisposeAsync();

        var items = new List<BillItem>();
        foreach (var itemRow in itemRows)
        {
            var glosas = await LoadGlosasAsync(connection, itemRow.Id);
            items.Add(BillItem.Restore(
                itemRow.Id,
                itemRow.ApprovedAuthorizationId,
                itemRow.Description,
                itemRow.Quantity,
                itemRow.UnitValue,
                glosas));
        }

        return items;
    }

    private static async Task<List<Glosa>> LoadGlosasAsync(SqliteConnection connection, Guid itemId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id,
                   reason,
                   details,
                   is_clawback
              FROM hospital_bill_item_glosas
             WHERE hospital_bill_item_id = $itemId;
            """;
        command.Parameters.AddWithValue("$itemId", itemId.ToString());

        var glosaRows = new List<(Guid Id, GlosaReason Reason, string Details, bool IsClawback)>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            glosaRows.Add((
                Guid.Parse(reader.GetString(0)),
                Enum.Parse<GlosaReason>(reader.GetString(1)),
                reader.GetString(2),
                reader.GetInt32(3) == 1));
        }

        await reader.DisposeAsync();

        var glosas = new List<Glosa>();
        foreach (var glosaRow in glosaRows)
        {
            var appeal = await LoadAppealAsync(connection, glosaRow.Id);
            glosas.Add(Glosa.Restore(
                glosaRow.Id,
                itemId,
                glosaRow.Reason,
                glosaRow.Details,
                glosaRow.IsClawback,
                appeal));
        }

        return glosas;
    }

    private static async Task<AdministrativeAppeal?> LoadAppealAsync(SqliteConnection connection, Guid glosaId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id,
                   status
              FROM administrative_appeals
             WHERE glosa_id = $glosaId;
            """;
        command.Parameters.AddWithValue("$glosaId", glosaId.ToString());

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        var appealId = Guid.Parse(reader.GetString(0));
        var status = Enum.Parse<AppealStatus>(reader.GetString(1));
        await reader.DisposeAsync();

        var evidenceDocuments = await LoadEvidenceAsync(connection, appealId);
        return AdministrativeAppeal.Restore(appealId, glosaId, evidenceDocuments, status);
    }

    private static async Task<List<Evidence>> LoadEvidenceAsync(SqliteConnection connection, Guid appealId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT document_url,
                   description
              FROM administrative_appeal_evidence
             WHERE appeal_id = $appealId
             ORDER BY position;
            """;
        command.Parameters.AddWithValue("$appealId", appealId.ToString());

        var evidenceDocuments = new List<Evidence>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            evidenceDocuments.Add(new Evidence(reader.GetString(0), reader.GetString(1)));
        }

        return evidenceDocuments;
    }
}
