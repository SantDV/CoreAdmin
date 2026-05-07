using Microsoft.Data.Sqlite;
using Dapper;

namespace CoreAdmin;

public static class Migrator
{
    public static void Run()
    {
        var connectionString = "Data Source=gym.db";
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        // Ensure column exists (just in case migration hasn't run yet)
        try {
            connection.Execute("ALTER TABLE CLIENTE ADD COLUMN codigo_asistencia TEXT(10)");
        } catch { /* Ignore if exists */ }

        // Update first client with a test code
        var id = connection.ExecuteScalar<int>("SELECT id_cliente FROM CLIENTE LIMIT 1");
        if (id > 0) {
            connection.Execute("UPDATE CLIENTE SET codigo_asistencia = '123456' WHERE id_cliente = @Id", new { Id = id });
            Console.WriteLine($"Cliente ID {id} actualizado con código 123456 para pruebas.");
        } else {
            Console.WriteLine("No se encontraron clientes para actualizar.");
        }
    }
}
