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

            // Índices para optimización de rendimiento
            CreateIndexIfNotExist(connection, "idx_cliente_documento", "CLIENTE", "documento");
            CreateIndexIfNotExist(connection, "idx_cliente_nombre_completo", "CLIENTE", "nombre, apellido");
            CreateIndexIfNotExist(connection, "idx_cliente_estado", "CLIENTE", "estado");
            CreateIndexIfNotExist(connection, "idx_pagos_cliente", "pagos", "id_cliente");
            CreateIndexIfNotExist(connection, "idx_pagos_fecha", "pagos", "fecha_registro");
            CreateIndexIfNotExist(connection, "idx_pagos_estado", "pagos", "estado");
            CreateIndexInternal(connection, "idx_gastos_fecha", "GASTOS", "fecha_registro");
            CreateIndexInternal(connection, "idx_gastos_estado", "GASTOS", "estado");

            // --- TABLAS PARA NOTIFICACIONES ---
            connection.Execute(@"CREATE TABLE IF NOT EXISTS ""CONFIGURACION_NOTIFICACIONES"" (
                ""id"" INTEGER PRIMARY KEY AUTOINCREMENT,
                ""smtp_host"" TEXT,
                ""smtp_port"" INTEGER,
                ""smtp_user"" TEXT,
                ""smtp_password"" TEXT,
                ""smtp_ssl"" INTEGER DEFAULT 1,
                ""wa_api_url"" TEXT,
                ""wa_instance"" TEXT,
                ""wa_token"" TEXT,
                ""mensaje_template"" TEXT,
                ""email_activo"" INTEGER DEFAULT 0,
                ""wa_activo"" INTEGER DEFAULT 0
            )");

            connection.Execute(@"CREATE TABLE IF NOT EXISTS ""NOTIFICACIONES_HISTORIAL"" (
                ""id_notificacion"" INTEGER PRIMARY KEY AUTOINCREMENT,
                ""id_cliente"" INTEGER,
                ""fecha_vencimiento_aviso"" TEXT,
                ""fecha_envio"" TEXT DEFAULT (datetime('now', 'localtime')),
                ""medio"" TEXT,
                ""estado"" INTEGER DEFAULT 1,
                FOREIGN KEY(""id_cliente"") REFERENCES ""CLIENTE""(""id_cliente"")
            )");

            if (connection.ExecuteScalar<int>("SELECT count(*) FROM CONFIGURACION_NOTIFICACIONES") == 0)
            {
                connection.Execute(@"INSERT INTO CONFIGURACION_NOTIFICACIONES 
                    (smtp_host, smtp_port, mensaje_template, wa_api_url) 
                    VALUES ('smtp.gmail.com', 587, 'Hola {nombre}, te recordamos que tu membresía vence el {fecha}. ¡Te esperamos!', 'https://api.evolution.com')");
            }
        }
    }

    private static void CreateIndexInternal(SqliteConnection connection, string indexName, string tableName, string columns)
    {
        // Reutilizamos el método existente pero con un nombre más corto para limpieza
        CreateIndexIfNotExist(connection, indexName, tableName, columns);
    }

    private static void CreateIndexIfNotExist(SqliteConnection connection, string indexName, string tableName, string columns)
    {
        var indexExists = connection.ExecuteScalar<int>($@"
            SELECT count(*) FROM sqlite_master 
            WHERE type = 'index' AND name = '{indexName}'") > 0;

        if (!indexExists)
        {
            connection.Execute($"CREATE INDEX IF NOT EXISTS {indexName} ON {tableName} ({columns})");
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
