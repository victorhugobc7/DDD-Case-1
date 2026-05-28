using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace Infra.Data;

public static class HealthInsuranceDatabase
{
    public const string DefaultDatabasePath = "health-insurance.db";

    public static string CreateConnectionString(string databasePath)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath
        };

        return builder.ToString();
    }

    public static async Task<SqliteConnection> OpenConnectionAsync(string connectionString)
    {
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        await command.ExecuteNonQueryAsync();

        return connection;
    }

    public static async Task InitializeAsync(string connectionString)
    {
        await using var connection = await OpenConnectionAsync(connectionString);
        await using var command = connection.CreateCommand();

        command.CommandText = """
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS authorization_requests (
                id TEXT PRIMARY KEY,
                beneficiary_id TEXT NOT NULL,
                plan_number TEXT NOT NULL,
                procedure_code TEXT NOT NULL,
                clinical_justification TEXT NOT NULL,
                requesting_professional TEXT NOT NULL,
                executing_establishment TEXT NOT NULL,
                expected_date TEXT NOT NULL,
                status TEXT NOT NULL,
                is_urgent_or_emergency INTEGER NOT NULL,
                requires_post_payment_audit INTEGER NOT NULL,
                denial_reason TEXT NULL,
                pending_reason TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS authorization_requested_items (
                id TEXT PRIMARY KEY,
                authorization_id TEXT NOT NULL,
                position INTEGER NOT NULL,
                description TEXT NOT NULL,
                requested_quantity INTEGER NOT NULL,
                approved_quantity INTEGER NOT NULL,
                FOREIGN KEY (authorization_id)
                    REFERENCES authorization_requests(id)
                    ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS beneficiaries (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                birth_date TEXT NOT NULL,
                join_date TEXT NOT NULL,
                plan_id TEXT NOT NULL,
                status TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS plans (
                id TEXT PRIMARY KEY,
                number TEXT NOT NULL UNIQUE,
                type TEXT NOT NULL,
                copay_percentage TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS plan_grace_periods (
                plan_id TEXT NOT NULL,
                procedure_type TEXT NOT NULL,
                days INTEGER NOT NULL,
                PRIMARY KEY (plan_id, procedure_type),
                FOREIGN KEY (plan_id)
                    REFERENCES plans(id)
                    ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS procedure_catalog_items (
                code TEXT PRIMARY KEY,
                description TEXT NOT NULL,
                type TEXT NOT NULL,
                minimum_age INTEGER NULL,
                maximum_age INTEGER NULL
            );

            CREATE TABLE IF NOT EXISTS hospital_bills (
                id TEXT PRIMARY KEY,
                beneficiary_id TEXT NOT NULL,
                executing_establishment TEXT NOT NULL,
                status TEXT NOT NULL DEFAULT 'Open'
            );

            CREATE TABLE IF NOT EXISTS hospital_bill_items (
                id TEXT PRIMARY KEY,
                hospital_bill_id TEXT NOT NULL,
                position INTEGER NOT NULL,
                approved_authorization_id TEXT NOT NULL,
                description TEXT NOT NULL,
                quantity INTEGER NOT NULL,
                unit_value TEXT NOT NULL,
                unit_currency TEXT NOT NULL DEFAULT 'BRL',
                FOREIGN KEY (hospital_bill_id)
                    REFERENCES hospital_bills(id)
                    ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS hospital_bill_item_glosas (
                id TEXT PRIMARY KEY,
                hospital_bill_item_id TEXT NOT NULL,
                reason TEXT NOT NULL,
                details TEXT NOT NULL,
                is_clawback INTEGER NOT NULL,
                FOREIGN KEY (hospital_bill_item_id)
                    REFERENCES hospital_bill_items(id)
                    ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS administrative_appeals (
                id TEXT PRIMARY KEY,
                glosa_id TEXT NOT NULL UNIQUE,
                status TEXT NOT NULL,
                FOREIGN KEY (glosa_id)
                    REFERENCES hospital_bill_item_glosas(id)
                    ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS administrative_appeal_evidence (
                id TEXT PRIMARY KEY,
                appeal_id TEXT NOT NULL,
                position INTEGER NOT NULL,
                document_url TEXT NOT NULL,
                description TEXT NOT NULL,
                FOREIGN KEY (appeal_id)
                    REFERENCES administrative_appeals(id)
                    ON DELETE CASCADE
            );
            """;

        await command.ExecuteNonQueryAsync();

        await EnsureColumnAsync(connection, "hospital_bills", "status", "TEXT NOT NULL DEFAULT 'Open'");
        await EnsureColumnAsync(connection, "hospital_bill_items", "unit_currency", "TEXT NOT NULL DEFAULT 'BRL'");
    }

    private static async Task EnsureColumnAsync(
        SqliteConnection connection,
        string tableName,
        string columnName,
        string columnDefinition)
    {
        await using var inspectCommand = connection.CreateCommand();
        inspectCommand.CommandText = $"PRAGMA table_info({tableName});";

        await using var reader = await inspectCommand.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (reader.GetString(1) == columnName)
                return;
        }

        await reader.DisposeAsync();

        await using var alterCommand = connection.CreateCommand();
        alterCommand.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};";
        await alterCommand.ExecuteNonQueryAsync();
    }
}
