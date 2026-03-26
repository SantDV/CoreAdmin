using Dapper;
using GymDashboard.Models;
using Microsoft.Data.Sqlite;
using System.IO;

namespace GymDashboard.Services;

public class SettingsService
{
    private readonly string _connectionString = DatabaseInitializer.ConnectionString;

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
        var query = "SELECT id_plan AS IdPlan, plan_nombre AS PlanNombre, precio AS Precio FROM PLANES ORDER BY precio ASC";
        return (await connection.QueryAsync<Planes>(query)).ToList();
    }

    public async Task AddPlanAsync(Planes plan)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = "INSERT INTO PLANES (plan_nombre, precio) VALUES (@PlanNombre, @Precio)";
        await connection.ExecuteAsync(query, plan);
    }

    public async Task UpdatePlanAsync(Planes plan)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = "UPDATE PLANES SET plan_nombre = @PlanNombre, precio = @Precio WHERE id_plan = @IdPlan";
        await connection.ExecuteAsync(query, plan);
    }

    public async Task DeletePlanAsync(int idPlan)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = "DELETE FROM PLANES WHERE id_plan = @Id";
        await connection.ExecuteAsync(query, new { Id = idPlan });
    }
}
