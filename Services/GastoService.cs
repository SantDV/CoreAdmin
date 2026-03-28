using Dapper;
using CoreAdmin.Models;
using Microsoft.Data.Sqlite;

namespace CoreAdmin.Services;

public class GastoService
{
    private readonly string _connectionString = DatabaseInitializer.ConnectionString;

    public GastoService()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public async Task<List<Gasto>> GetGastosAsync(string searchTerm = "", int limit = 50, int offset = 0)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = @"
            SELECT 
                id_gasto AS IdGasto,
                descripcion AS Descripcion,
                monto AS Monto,
                categoria AS Categoria,
                fecha_registro AS FechaRegistro,
                estado AS Estado
            FROM GASTOS
            WHERE estado = 1 ";

        var parameters = new DynamicParameters();
        parameters.Add("Limit", limit);
        parameters.Add("Offset", offset);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query += " AND (descripcion LIKE @Search OR categoria LIKE @Search) ";
            parameters.Add("Search", $"%{searchTerm}%");
        }

        query += " ORDER BY id_gasto DESC LIMIT @Limit OFFSET @Offset";
        
        return (await connection.QueryAsync<Gasto>(query, parameters)).ToList();
    }

    public async Task AddGastoAsync(Gasto gasto)
    {
        gasto.FechaRegistro = DateTime.Now;
        using var connection = new SqliteConnection(_connectionString);
        var query = @"
            INSERT INTO GASTOS (descripcion, monto, categoria, fecha_registro, estado)
            VALUES (@Descripcion, @Monto, @Categoria, @FechaRegistro, 1)";
        await connection.ExecuteAsync(query, gasto);
    }

    public async Task UpdateGastoAsync(Gasto gasto)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = @"
            UPDATE GASTOS SET 
                descripcion = @Descripcion,
                monto = @Monto,
                categoria = @Categoria,
                estado = @Estado
            WHERE id_gasto = @IdGasto";
        await connection.ExecuteAsync(query, gasto);
    }

    public async Task AnularGastoAsync(int idGasto)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = "UPDATE GASTOS SET estado = 0 WHERE id_gasto = @Id";
        await connection.ExecuteAsync(query, new { Id = idGasto });
    }

    public async Task<decimal> GetTotalGastosMesAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        var inicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).ToString("yyyy-MM-dd");
        var query = "SELECT SUM(monto) FROM GASTOS WHERE estado = 1 AND date(fecha_registro) >= date(@InicioMes)";
        return await connection.ExecuteScalarAsync<decimal?>(query, new { InicioMes = inicioMes }) ?? 0;
    }
}
