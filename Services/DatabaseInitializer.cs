using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;
using System.Linq;
using System;
using System.Collections.Generic;

namespace CoreAdmin.Services;

public static class DatabaseInitializer
{
    private const string DbFile = "gym.db";
    public static string ConnectionString { get; set; } = $"Data Source={DbFile};Foreign Keys=True";


    public static void Initialize()
    {
        // Ensure the directory exists if needed (not applicable here as it's root)
        
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            // Global enforcement for this session
            connection.Execute("PRAGMA foreign_keys = ON;");
        }

        if (!File.Exists(DbFile))
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            var schemaSql = File.ReadAllText("schema.sql");
            connection.Execute(schemaSql);
            SeedData(connection);
        }
        else
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            connection.Execute("PRAGMA foreign_keys = ON;");

            var isInitialized = connection.ExecuteScalar<int>("SELECT count(*) FROM sqlite_master WHERE type='table' AND name='CLIENTE'") > 0;
            if (!isInitialized)
            {
                var schemaSql = File.ReadAllText("schema.sql");
                connection.Execute(schemaSql);
                SeedData(connection);
            }

            // Ensure Printer Config table exists
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

            // Standard migrations
            AddColumnIfNotExist(connection, "CLIENTE", "tipo_sangre", "TEXT");
            AddColumnIfNotExist(connection, "CLIENTE", "alergias", "TEXT");
            AddColumnIfNotExist(connection, "CLIENTE", "enfermedades_cronicas", "TEXT");
            AddColumnIfNotExist(connection, "CLIENTE", "contacto_emergencia_nombre", "TEXT");
            AddColumnIfNotExist(connection, "CLIENTE", "contacto_emergencia_telefono", "TEXT");
            AddColumnIfNotExist(connection, "CLIENTE", "vencimiento_apto_medico", "TEXT");

            connection.Execute(@"CREATE TABLE IF NOT EXISTS ""GASTOS"" (
                ""id_gasto"" INTEGER PRIMARY KEY AUTOINCREMENT,
                ""descripcion"" TEXT NOT NULL,
                ""monto"" NUMERIC(10, 2) NOT NULL,
                ""categoria"" TEXT,
                ""fecha_registro"" TEXT DEFAULT (datetime('now', 'localtime')),
                ""estado"" INTEGER DEFAULT 1
            )");

            connection.Execute(@"CREATE TABLE IF NOT EXISTS ""ASISTENCIA"" (
                ""id_asistencia"" INTEGER PRIMARY KEY AUTOINCREMENT,
                ""id_cliente"" INTEGER NOT NULL,
                ""fecha_entrada"" TEXT DEFAULT (datetime('now', 'localtime')),
                ""tipo"" TEXT DEFAULT 'Entrada',
                FOREIGN KEY(""id_cliente"") REFERENCES ""CLIENTE""(""id_cliente"")
            )");

            AddColumnIfNotExist(connection, "CLIENTE", "codigo_asistencia", "TEXT(10)");
            AddColumnIfNotExist(connection, "pagos", "estado", "INTEGER DEFAULT 1");

            // Cleaning duplicates and applying unique constraints
            CleanupAndEnforceUnique(connection);

            // Indices
            CreateIndexIfNotExist(connection, "idx_cliente_documento", "CLIENTE", "documento");
            CreateIndexIfNotExist(connection, "idx_cliente_nombre_completo", "CLIENTE", "nombre, apellido");
            CreateIndexIfNotExist(connection, "idx_cliente_estado", "CLIENTE", "estado");
            CreateIndexIfNotExist(connection, "idx_pagos_cliente", "pagos", "id_cliente");
            CreateIndexIfNotExist(connection, "idx_pagos_fecha", "pagos", "fecha_registro");
            CreateIndexIfNotExist(connection, "idx_pagos_estado", "pagos", "estado");
            CreateIndexInternal(connection, "idx_gastos_fecha", "GASTOS", "fecha_registro");
            CreateIndexInternal(connection, "idx_gastos_estado", "GASTOS", "estado");
            CreateIndexInternal(connection, "idx_asistencia_cliente", "ASISTENCIA", "id_cliente");
            CreateIndexInternal(connection, "idx_asistencia_fecha", "ASISTENCIA", "fecha_entrada");

            // Notifications
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

            EnsureAllClientsHaveAssistCode(connection);
        }
    }

    private static void CleanupAndEnforceUnique(SqliteConnection connection)
    {
        // Fix potential issues with NULLs or empty strings in documents before grouping
        connection.Execute("UPDATE CLIENTE SET documento = trim(documento) WHERE documento IS NOT NULL");

        // 1. Clean up CLIENTE duplicates
        var duplicateGroups = connection.Query<string>("SELECT documento FROM CLIENTE WHERE documento IS NOT NULL AND documento != '' GROUP BY documento HAVING count(*) > 1");
        foreach (var doc in duplicateGroups)
        {
            var ids = connection.Query<int>("SELECT id_cliente FROM CLIENTE WHERE documento = @Doc ORDER BY id_cliente", new { Doc = doc }).ToList();
            int mainId = ids[0];
            var otherIds = ids.Skip(1).ToList();

            foreach (var oldId in otherIds)
            {
                connection.Execute("UPDATE pagos SET id_cliente = @MainId WHERE id_cliente = @OldId", new { MainId = mainId, OldId = oldId });
                connection.Execute("UPDATE VENTA SET id_cliente = @MainId WHERE id_cliente = @OldId", new { MainId = mainId, OldId = oldId });
                connection.Execute("UPDATE ASISTENCIA SET id_cliente = @MainId WHERE id_cliente = @OldId", new { MainId = mainId, OldId = oldId });
                connection.Execute("UPDATE NOTIFICACIONES_HISTORIAL SET id_cliente = @MainId WHERE id_cliente = @OldId", new { MainId = mainId, OldId = oldId });
                connection.Execute("DELETE FROM CLIENTE WHERE id_cliente = @OldId", new { OldId = oldId });
            }
        }
        CreateIndexIfNotExist(connection, "idx_unique_cliente_documento", "CLIENTE", "documento", true);

        // 2. Clean up PRODUCTO duplicates (by code)
        connection.Execute("UPDATE PRODUCTO SET codigo = trim(codigo) WHERE codigo IS NOT NULL");
        var duplicateProds = connection.Query<string>("SELECT codigo FROM PRODUCTO WHERE codigo IS NOT NULL AND codigo != '' GROUP BY codigo HAVING count(*) > 1");
        foreach (var code in duplicateProds)
        {
            var ids = connection.Query<int>("SELECT id_producto FROM PRODUCTO WHERE codigo = @Code ORDER BY id_producto", new { Code = code }).ToList();
            int mainId = ids[0];
            var otherIds = ids.Skip(1).ToList();

            foreach (var oldId in otherIds)
            {
                connection.Execute("UPDATE DETALLE_VENTA SET id_producto = @MainId WHERE id_producto = @OldId", new { MainId = mainId, OldId = oldId });
                connection.Execute("UPDATE MOVIMIENTO_STOCK SET id_producto = @MainId WHERE id_producto = @OldId", new { MainId = mainId, OldId = oldId });
                connection.Execute("DELETE FROM PRODUCTO WHERE id_producto = @OldId", new { OldId = oldId });
            }
        }
        CreateIndexIfNotExist(connection, "idx_unique_producto_codigo", "PRODUCTO", "codigo", true);
    }

    private static void EnsureAllClientsHaveAssistCode(SqliteConnection connection)
    {
        var clientsWithoutCode = connection.Query<int>("SELECT id_cliente FROM CLIENTE WHERE (codigo_asistencia IS NULL OR codigo_asistencia = '')");
        if (!clientsWithoutCode.Any()) return;

        var random = new System.Random();
        foreach (var clientId in clientsWithoutCode)
        {
            string code;
            bool exists;
            do
            {
                code = random.Next(100000, 999999).ToString();
                exists = connection.ExecuteScalar<int>("SELECT count(*) FROM CLIENTE WHERE codigo_asistencia = @Code", new { Code = code }) > 0;
            } while (exists);

            connection.Execute("UPDATE CLIENTE SET codigo_asistencia = @Code WHERE id_cliente = @Id", new { Code = code, Id = clientId });
        }
    }

    private static void CreateIndexInternal(SqliteConnection connection, string indexName, string tableName, string columns)
    {
        CreateIndexIfNotExist(connection, indexName, tableName, columns);
    }

    private static void CreateIndexIfNotExist(SqliteConnection connection, string indexName, string tableName, string columns, bool unique = false)
    {
        var indexExists = connection.ExecuteScalar<int>($@"
            SELECT count(*) FROM sqlite_master 
            WHERE type = 'index' AND name = '{indexName}'") > 0;

        if (!indexExists)
        {
            connection.Execute($"CREATE {(unique ? "UNIQUE" : "")} INDEX IF NOT EXISTS {indexName} ON {tableName} ({columns})");
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
