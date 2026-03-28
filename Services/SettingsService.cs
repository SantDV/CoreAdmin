using Dapper;
using CoreAdmin.Models;
using Microsoft.Data.Sqlite;
using System.IO;

namespace CoreAdmin.Services;

public class SettingsService
{
    private readonly string _connectionString = DatabaseInitializer.ConnectionString;

    public SettingsService()
    {
        // Asegura que id_plan mapee automáticamente a IdPlan sin alias explícitos si hay guiones bajos
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    // Orchestrates underlying WinForms IO manipulation to rescue SQLite files on demand
    public string CreateBackup()
    {
        try
        {
            var sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gym.db");
            if (!File.Exists(sourcePath))
            {
                sourcePath = "gym.db"; 
                if (!File.Exists(sourcePath)) return "Error: No se encontró la base de datos principal.";
            }

            var docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var backupDir = Path.Combine(docPath, "backup clientes");
            
            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }

            var fileName = $"backup_gym_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.db";
            var destPath = Path.Combine(backupDir, fileName);

            File.Copy(sourcePath, destPath, overwrite: true);
            return destPath; // Return physical location path
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    public async Task<List<Planes>> GetPlanesAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = "SELECT id_plan, plan_nombre, precio FROM PLANES ORDER BY precio ASC";
        return (await connection.QueryAsync<Planes>(query)).ToList();
    }

    public async Task SavePlanAsync(Planes plan)
    {
        try 
        {
            using var connection = new SqliteConnection(_connectionString);
            if (plan.IdPlan == 0)
            {
                var query = "INSERT INTO PLANES (plan_nombre, precio) VALUES (@PlanNombre, @Precio)";
                await connection.ExecuteAsync(query, plan);
            }
            else
            {
                var query = "UPDATE PLANES SET plan_nombre = @PlanNombre, precio = @Precio WHERE id_plan = @IdPlan";
                await connection.ExecuteAsync(query, plan);
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error al guardar el plan: " + ex.Message);
        }
    }

    public async Task DeletePlanAsync(int id)
    {
        try 
        {
            using var connection = new SqliteConnection(_connectionString);
            var query = "DELETE FROM PLANES WHERE id_plan = @Id";
            await connection.ExecuteAsync(query, new { Id = id });
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // Constraint violation
        {
            throw new Exception("No se puede eliminar el plan porque hay clientes asociados a él. Considere desactivarlo o cambiar los clientes de plan primero.");
        }
        catch (Exception ex)
        {
            throw new Exception("Error al eliminar el plan: " + ex.Message);
        }
    }

    public async Task<PrinterSettings> GetPrinterSettingsAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = "SELECT nombre_impresora as PrinterName, ip_impresora as PrinterIp, puerto_impresora as PrinterPort, usar_red as UseNetwork FROM CONFIGURACION_IMPRESORA LIMIT 1";
        return await connection.QueryFirstOrDefaultAsync<PrinterSettings>(query) ?? new PrinterSettings();
    }

    public async Task UpdatePrinterSettingsAsync(PrinterSettings settings)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = "UPDATE CONFIGURACION_IMPRESORA SET nombre_impresora = @PrinterName, ip_impresora = @PrinterIp, puerto_impresora = @PrinterPort, usar_red = @UseNetwork WHERE id = 1";
        
        // Si no existe (caso raro), insertar
        var rows = await connection.ExecuteAsync(query, settings);
        if (rows == 0)
        {
            await connection.ExecuteAsync("INSERT INTO CONFIGURACION_IMPRESORA (nombre_impresora, ip_impresora, puerto_impresora, usar_red) VALUES (@PrinterName, @PrinterIp, @PrinterPort, @UseNetwork)", settings);
        }
    }
}
