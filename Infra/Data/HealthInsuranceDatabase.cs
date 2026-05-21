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

            CREATE TABLE IF NOT EXISTS hospital_bills (
                id TEXT PRIMARY KEY,
                beneficiary_id TEXT NOT NULL,
                executing_establishment TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS hospital_bill_items (
                id TEXT PRIMARY KEY,
                hospital_bill_id TEXT NOT NULL,
                position INTEGER NOT NULL,
                approved_authorization_id TEXT NOT NULL,
                description TEXT NOT NULL,
                quantity INTEGER NOT NULL,
                unit_value TEXT NOT NULL,
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
            """;

        await command.ExecuteNonQueryAsync();
    }
}
