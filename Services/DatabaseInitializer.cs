using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CoreAdmin.Services;

public static class DatabaseInitializer
{
    private const string DbFile = "gym.db";
    public const string ConnectionString = $"Data Source={DbFile}";

    public static void Initialize()
    {
        if (!File.Exists(DbFile))
        {
            // Initialize schema if db does not exist
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var schemaSql = File.ReadAllText("schema.sql");
            connection.Execute(schemaSql);
            SeedData(connection);
        }
        else
        {
            // Just double check that the tables really exist in the db if for some reason it's empty
            using var connection = new SqliteConnection(ConnectionString);
            var isInitialized = connection.ExecuteScalar<int>("SELECT count(*) FROM sqlite_master WHERE type='table' AND name='CLIENTE'") > 0;
            if (!isInitialized)
            {
                var schemaSql = File.ReadAllText("schema.sql");
                connection.Execute(schemaSql);
                SeedData(connection);
            }

            // Ensure Printer Config table exists for existing databases
            connection.Execute(@"CREATE TABLE IF NOT EXISTS ""CONFIGURACION_IMPRESORA"" (
                ""id"" INTEGER PRIMARY KEY AUTOINCREMENT,
                ""nombre_impresora"" TEXT,
                ""ip_impresora"" TEXT,
                ""puerto_impresora"" INTEGER,
                ""usar_red"" INTEGER DEFAULT 0
            )");
            
            
            if (connection.ExecuteScalar<int>("SELECT count(*) FROM CONFIGURACION_IMPRESORA") == 0)
            {
                connection.Execute("INSERT INTO CONFIGURACION_IMPRESORA (nombre_impresora, ip_impresora, puerto_impresora, usar_red) VALUES ('XPrinter XP-V320N', '192.168.1.100', 9100, 1)");
            }

            // Migración para Ficha Médica en tabla CLIENTE
            AddColumnIfNotExist(connection, "CLIENTE", "tipo_sangre", "TEXT");
            AddColumnIfNotExist(connection, "CLIENTE", "alergias", "TEXT");
            AddColumnIfNotExist(connection, "CLIENTE", "enfermedades_cronicas", "TEXT");
            AddColumnIfNotExist(connection, "CLIENTE", "contacto_emergencia_nombre", "TEXT");
            AddColumnIfNotExist(connection, "CLIENTE", "contacto_emergencia_telefono", "TEXT");
            AddColumnIfNotExist(connection, "CLIENTE", "vencimiento_apto_medico", "TEXT");

            // Migración para Gastos y Anulaciones
            connection.Execute(@"CREATE TABLE IF NOT EXISTS ""GASTOS"" (
                ""id_gasto"" INTEGER PRIMARY KEY AUTOINCREMENT,
                ""descripcion"" TEXT NOT NULL,
                ""monto"" NUMERIC(10, 2) NOT NULL,
                ""categoria"" TEXT,
                ""fecha_registro"" TEXT DEFAULT (datetime('now', 'localtime')),
                ""estado"" INTEGER DEFAULT 1
            )");

            AddColumnIfNotExist(connection, "pagos", "estado", "INTEGER DEFAULT 1");
        }
    }

    private static void AddColumnIfNotExist(SqliteConnection connection, string tableName, string columnName, string columnType)
    {
        var columnExists = connection.ExecuteScalar<int>($@"
            SELECT count(*) FROM pragma_table_info('{tableName}') 
            WHERE name = '{columnName}'") > 0;

        if (!columnExists)
        {
            connection.Execute($"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType}");
        }
    }

    private static void SeedData(SqliteConnection connection)
    {
        if (connection.ExecuteScalar<int>("SELECT count(*) FROM GENERO") == 0)
        {
            connection.Execute("INSERT INTO GENERO (genero_nombre) VALUES ('Masculino'), ('Femenino'), ('Otro')");
        }
        if (connection.ExecuteScalar<int>("SELECT count(*) FROM PLANES") == 0)
        {
            connection.Execute("INSERT INTO PLANES (plan_nombre, precio) VALUES ('Básico', 1500), ('Premium', 2500), ('Anual', 15000)");
        }
    }
}
