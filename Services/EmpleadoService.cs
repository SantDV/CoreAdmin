using Dapper;
using CoreAdmin.Models;
using Microsoft.Data.Sqlite;

namespace CoreAdmin.Services;

public class EmpleadoService
{
    private readonly string _connectionString = DatabaseInitializer.ConnectionString;

    private const string SelectEmpleadoScript = @"
        SELECT 
            E.id_empleado AS IdEmpleado,
            E.documento AS Documento,
            E.nombre AS Nombre,
            E.apellido AS Apellido,
            E.direccion AS Direccion,
            E.telefono AS Telefono,
            E.id_usuario AS IdUsuario,
            E.estado AS Estado,
            E.fecha_registro AS FechaRegistro,
            U.nombre_usuario AS NombreUsuario,
            U.clave AS Clave,
            U.id_rol AS IdRol
        FROM EMPLEADO E
        LEFT JOIN USUARIO U ON E.id_usuario = U.id_usuario";

    public async Task<List<Empleado>> GetEmpleadosAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = SelectEmpleadoScript + " ORDER BY E.id_empleado DESC";
        return (await connection.QueryAsync<Empleado>(query)).ToList();
    }
    
    public async Task AddEmpleadoAsync(Empleado empleado)
    {
        try 
        {
            empleado.FechaRegistro = DateTime.Now;
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                // Hash password before saving
                string hashedPass = AuthService.HashPassword(empleado.Clave ?? "1234");

                var queryUsuario = @"
                    INSERT INTO USUARIO (nombre_usuario, clave, id_rol)
                    VALUES (@NombreUsuario, @Clave, @IdRol);
                    SELECT last_insert_rowid();";
                
                int idUsuario = await connection.ExecuteScalarAsync<int>(queryUsuario, new { 
                    empleado.NombreUsuario, 
                    Clave = hashedPass, 
                    empleado.IdRol 
                }, transaction);

                empleado.IdUsuario = idUsuario;
                var insertEmpleadoQuery = @"
                    INSERT INTO EMPLEADO (documento, nombre, apellido, direccion, telefono, id_usuario, estado, fecha_registro)
                    VALUES (@Documento, @Nombre, @Apellido, @Direccion, @Telefono, @IdUsuario, @Estado, @FechaRegistro)";
                await connection.ExecuteAsync(insertEmpleadoQuery, empleado, transaction);

                transaction.Commit();
            }
            catch { transaction.Rollback(); throw; }
        }
        catch (Exception ex)
        {
            throw new Exception("Error al añadir empleado: " + ex.Message);
        }
    }

    public async Task UpdateEmpleadoAsync(Empleado e)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();
        try
        {
            // Hash password before updating
            string hashedPass = AuthService.HashPassword(e.Clave ?? "1234");

            var queryUser = "UPDATE USUARIO SET nombre_usuario = @NombreUsuario, clave = @Clave, id_rol = @IdRol WHERE id_usuario = @IdUsuario";
            await connection.ExecuteAsync(queryUser, new { 
                e.NombreUsuario, 
                Clave = hashedPass, 
                e.IdRol, 
                e.IdUsuario 
            }, transaction);

            var updateEmpleadoQuery = @"
                UPDATE EMPLEADO SET 
                    documento = @Documento,
                    nombre = @Nombre,
                    apellido = @Apellido,
                    direccion = @Direccion,
                    telefono = @Telefono,
                    estado = @Estado
                WHERE id_empleado = @IdEmpleado";
            await connection.ExecuteAsync(updateEmpleadoQuery, e, transaction);

            transaction.Commit();
        }
        catch { transaction.Rollback(); throw; }
    }

    public async Task DeleteEmpleadoAsync(int idEmpleado, int? idUsuario)
    {
        try 
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                var deleteEmpleado = "DELETE FROM EMPLEADO WHERE id_empleado = @IdEmpleado";
                await connection.ExecuteAsync(deleteEmpleado, new { IdEmpleado = idEmpleado }, transaction);

                if (idUsuario.HasValue)
                {
                    var deleteUsuario = "DELETE FROM USUARIO WHERE id_usuario = @IdUsuario";
                    await connection.ExecuteAsync(deleteUsuario, new { IdUsuario = idUsuario.Value }, transaction);
                }
                transaction.Commit();
            }
            catch { transaction.Rollback(); throw; }
        }
        catch (Exception ex)
        {
            throw new Exception("Error al eliminar empleado: " + ex.Message);
        }
    }
}
