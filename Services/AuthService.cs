using Dapper;
using CoreAdmin.Models;
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;

namespace CoreAdmin.Services;

public class AuthService
{
    private readonly string _connectionString = DatabaseInitializer.ConnectionString;
    public Usuario? CurrentUser { get; private set; }
    
    public event Action? OnAuthStateChanged;

    public async Task<bool> LoginAsync(string username, string password)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = "SELECT id_usuario AS IdUsuario, nombre_usuario AS NombreUsuario, clave AS Clave, id_rol AS IdRol FROM USUARIO WHERE nombre_usuario = @Username";
            
        var user = await connection.QuerySingleOrDefaultAsync<Usuario>(query, new { Username = username });
        
        if (user != null)
        {
            var hashedInput = HashPassword(password);
            // Allow plain text for migration or direct hash compare
            if (user.Clave == password || user.Clave == hashedInput)
            {
                CurrentUser = user;
                NotifyStateChanged();
                return true;
            }
        }
        return false;
    }

    public static string HashPassword(string password)
    {
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLower();
    }

    public void Logout()
    {
        CurrentUser = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnAuthStateChanged?.Invoke();
}
