using Dapper;
using GymDashboard.Models;
using Microsoft.Data.Sqlite;

namespace GymDashboard.Services;

public class AuthService
{
    private readonly string _connectionString = DatabaseInitializer.ConnectionString;
    public Usuario? CurrentUser { get; private set; }
    
    public event Action? OnAuthStateChanged;

    public async Task<bool> LoginAsync(string username, string password)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = @"
            SELECT 
                id_usuario AS IdUsuario, 
                nombre_usuario AS NombreUsuario, 
                clave AS Clave, 
                id_rol AS IdRol 
            FROM USUARIO 
            WHERE nombre_usuario = @Username AND clave = @Password";
            
        var user = await connection.QuerySingleOrDefaultAsync<Usuario>(query, new { Username = username, Password = password });
        
        if (user != null)
        {
            CurrentUser = user;
            NotifyStateChanged();
            return true;
        }
        return false;
    }

    public void Logout()
    {
        CurrentUser = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnAuthStateChanged?.Invoke();
}
