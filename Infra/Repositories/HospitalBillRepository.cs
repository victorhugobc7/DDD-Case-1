using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Domain.Aggregates.Auditoria;
using Domain.Aggregates.Faturamento;
using Domain.Enums.Auditoria;
using Domain.Repositories.Faturamento;
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
                   executing_establishment
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

        await reader.DisposeAsync();

        var bill = new HospitalBill(billId, beneficiaryId, executingEstablishment);
        var items = await LoadItemsAsync(connection, billId);
        foreach (var item in items)
        {
            bill.AddItem(item);
        }

        return bill;
    }

    public async Task AddAsync(HospitalBill bill)
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
                executing_establishment
            )
            VALUES (
                $id,
                $beneficiaryId,
                $executingEstablishment
            )
            ON CONFLICT(id) DO UPDATE SET
                beneficiary_id = excluded.beneficiary_id,
                executing_establishment = excluded.executing_establishment;
            """;

        command.Parameters.AddWithValue("$id", bill.Id.ToString());
        command.Parameters.AddWithValue("$beneficiaryId", bill.BeneficiaryId.ToString());
        command.Parameters.AddWithValue("$executingEstablishment", bill.ExecutingEstablishment);

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
                unit_value
            )
            VALUES (
                $id,
                $billId,
                $position,
                $approvedAuthorizationId,
                $description,
                $quantity,
                $unitValue
            );
            """;

        command.Parameters.AddWithValue("$id", item.Id.ToString());
        command.Parameters.AddWithValue("$billId", billId.ToString());
        command.Parameters.AddWithValue("$position", position);
        command.Parameters.AddWithValue("$approvedAuthorizationId", item.ApprovedAuthorizationId.ToString());
        command.Parameters.AddWithValue("$description", item.Description);
        command.Parameters.AddWithValue("$quantity", item.Quantity);
        command.Parameters.AddWithValue("$unitValue", item.UnitValue.ToString(CultureInfo.InvariantCulture));

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
                   unit_value
              FROM hospital_bill_items
             WHERE hospital_bill_id = $billId
             ORDER BY position;
            """;
        command.Parameters.AddWithValue("$billId", billId.ToString());

        var itemRows = new List<(Guid Id, Guid ApprovedAuthorizationId, string Description, int Quantity, decimal UnitValue)>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            itemRows.Add((
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                reader.GetString(2),
                reader.GetInt32(3),
                decimal.Parse(reader.GetString(4), CultureInfo.InvariantCulture)));
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

        var glosas = new List<Glosa>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            glosas.Add(Glosa.Restore(
                Guid.Parse(reader.GetString(0)),
                itemId,
                Enum.Parse<GlosaReason>(reader.GetString(1)),
                reader.GetString(2),
                reader.GetInt32(3) == 1));
        }

        return glosas;
    }
}
