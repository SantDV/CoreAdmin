using Dapper;
using CoreAdmin.Models;
using Microsoft.Data.Sqlite;

namespace CoreAdmin.Services;

public class ClienteService
{
    private readonly string _connectionString = DatabaseInitializer.ConnectionString;
    private readonly DashboardCacheService _cache;

    public ClienteService(DashboardCacheService cache)
    {
        _cache = cache;
        DefaultTypeMap.MatchNamesWithUnderscores = true; 
    }

    private const string SelectClienteScript = @"
        SELECT 
            id_cliente AS IdCliente,
            documento AS Documento,
            nombre AS Nombre,
            apellido AS Apellido,
            fecha_nacimiento AS FechaNacimiento,
            id_genero AS IdGenero,
            direccion AS Direccion,
            telefono AS Telefono,
            email AS Email,
            id_plan AS IdPlan,
            fecha_inicio AS FechaInicio,
            fecha_vencimiento AS FechaVencimiento,
            fecha_registro AS FechaRegistro,
            estado AS Estado,
            nota_adicional AS NotaAdicional,
            huella AS Huella,
            tipo_sangre AS TipoSangre,
            alergias AS Alergias,
            enfermedades_cronicas AS EnfermedadesCronicas,
            contacto_emergencia_nombre AS ContactoEmergenciaNombre,
            contacto_emergencia_telefono AS ContactoEmergenciaTelefono,
            vencimiento_apto_medico AS VencimientoAptoMedico
        FROM CLIENTE";

    public async Task<List<Cliente>> GetClientesAsync(string searchTerm = "", int limit = 50, int offset = 0)
    {
        using var connection = new SqliteConnection(_connectionString);
        string query;
        var parameters = new DynamicParameters();
        parameters.Add("Limit", limit);
        parameters.Add("Offset", offset);

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            query = SelectClienteScript + " ORDER BY id_cliente DESC LIMIT @Limit OFFSET @Offset";
        }
        else
        {
            query = SelectClienteScript + @"
                WHERE documento LIKE @Search 
                   OR nombre LIKE @Search 
                   OR apellido LIKE @Search 
                   OR email LIKE @Search
                ORDER BY id_cliente DESC LIMIT @Limit OFFSET @Offset";
            parameters.Add("Search", $"%{searchTerm}%");
        }
        
        return (await connection.QueryAsync<Cliente>(query, parameters)).ToList();
    }

    public async Task<List<Genero>> GetGenerosAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = "SELECT id_genero AS IdGenero, genero_nombre AS GeneroNombre FROM GENERO";
        var generos = await connection.QueryAsync<Genero>(query);
        return generos.ToList();
    }

    public async Task<List<Planes>> GetPlanesAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = "SELECT id_plan AS IdPlan, plan_nombre AS PlanNombre, precio AS Precio FROM PLANES";
        var planes = await connection.QueryAsync<Planes>(query);
        return planes.ToList();
    }

    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        var cached = _cache.GetCachedStats();
        if (cached != null) return cached;

        using var connection = new SqliteConnection(_connectionString);
        var hoy = DateTime.Now.Date.ToString("yyyy-MM-dd");
        var inicioSemana = DateTime.Now.Date.AddDays(-(int)DateTime.Now.DayOfWeek).ToString("yyyy-MM-dd");
        var inicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).ToString("yyyy-MM-dd");
        var inicioAnio = new DateTime(DateTime.Now.Year, 1, 1).ToString("yyyy-MM-dd");
        var enUnaSemana = DateTime.Now.Date.AddDays(7).ToString("yyyy-MM-dd");

        var query = @"
            SELECT 
                (SELECT count(*) FROM CLIENTE) AS TotalClientes,
                (SELECT count(*) FROM CLIENTE WHERE estado = 1) AS ClientesActivos,
                (SELECT count(*) FROM CLIENTE WHERE estado = 0) AS ClientesInactivos,
                (SELECT count(*) FROM CLIENTE WHERE date(fecha_registro) = date(@Hoy)) AS RegistrosHoy,
                (SELECT count(*) FROM CLIENTE WHERE date(fecha_registro) >= date(@InicioSemana)) AS RegistrosSemana,
                (SELECT count(*) FROM CLIENTE WHERE date(fecha_registro) >= date(@InicioMes)) AS RegistrosMes,
                (SELECT count(*) FROM CLIENTE WHERE date(fecha_registro) >= date(@InicioAnio)) AS RegistrosAnio,
                (SELECT count(*) FROM CLIENTE WHERE estado = 1 AND date(fecha_vencimiento) >= date(@Hoy) AND date(fecha_vencimiento) <= date(@EnUnaSemana)) AS PlanesPorVencer,
                (SELECT count(*) FROM CLIENTE WHERE estado = 1 AND date(fecha_vencimiento) < date(@Hoy)) AS PlanesVencidos,
                (SELECT COALESCE(SUM(monto), 0) FROM pagos WHERE estado = 1 AND date(fecha_registro) >= date(@InicioMes)) AS IngresosMes,
                (SELECT COALESCE(SUM(monto), 0) FROM GASTOS WHERE estado = 1 AND date(fecha_registro) >= date(@InicioMes)) AS EgresosMes
        ";

        var stats = await connection.QuerySingleAsync<DashboardStats>(query, new { Hoy = hoy, InicioSemana = inicioSemana, InicioMes = inicioMes, InicioAnio = inicioAnio, EnUnaSemana = enUnaSemana });
        _cache.SetCachedStats(stats);
        return stats;
    }

    public async Task<List<Cliente>> GetClientesPorVencerAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        var hoy = DateTime.Now.Date.ToString("yyyy-MM-dd");
        var enUnaSemana = DateTime.Now.Date.AddDays(7).ToString("yyyy-MM-dd");

        var query = SelectClienteScript + @" 
            WHERE estado = 1 
              AND date(fecha_vencimiento) >= date(@Hoy) 
              AND date(fecha_vencimiento) <= date(@EnUnaSemana)
            ORDER BY date(fecha_vencimiento) ASC
        ";

        var result = await connection.QueryAsync<Cliente>(query, new { Hoy = hoy, EnUnaSemana = enUnaSemana });
        return result.ToList();
    }

    public async Task AddClienteAsync(Cliente cliente)
    {
        try 
        {
            cliente.FechaRegistro = DateTime.Now;
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                var queryCliente = @"
                    INSERT INTO CLIENTE (documento, nombre, apellido, fecha_nacimiento, id_genero, direccion, telefono, email, id_plan, fecha_inicio, fecha_vencimiento, fecha_registro, estado, nota_adicional, huella, tipo_sangre, alergias, enfermedades_cronicas, contacto_emergencia_nombre, contacto_emergencia_telefono, vencimiento_apto_medico)
                    VALUES (@Documento, @Nombre, @Apellido, @FechaNacimiento, @IdGenero, @Direccion, @Telefono, @Email, @IdPlan, @FechaInicio, @FechaVencimiento, @FechaRegistro, @Estado, @NotaAdicional, @Huella, @TipoSangre, @Alergias, @EnfermedadesCronicas, @ContactoEmergenciaNombre, @ContactoEmergenciaTelefono, @VencimientoAptoMedico);
                    SELECT last_insert_rowid();";
                
                int idCliente = await connection.ExecuteScalarAsync<int>(queryCliente, cliente, transaction);

                var montoPlan = await connection.ExecuteScalarAsync<decimal?>("SELECT precio FROM PLANES WHERE id_plan = @IdPlan", new { IdPlan = cliente.IdPlan }, transaction) ?? 0;

                var queryPago = @"
                    INSERT INTO pagos (monto, id_plan, id_cliente, fecha_registro, estado)
                    VALUES (@Monto, @IdPlan, @IdCliente, @FechaRegistro, 1)";
                
                await connection.ExecuteAsync(queryPago, new {
                    Monto = montoPlan,
                    IdPlan = cliente.IdPlan,
                    IdCliente = idCliente,
                    FechaRegistro = cliente.FechaRegistro
                }, transaction);

                transaction.Commit();
                _cache.Invalidate();
            }
            catch { transaction.Rollback(); throw; }
        }
        catch (Exception ex)
        {
            throw new Exception("Error al registrar cliente: " + ex.Message);
        }
    }

    public async Task RenovarClienteAsync(Cliente cliente)
    {
        try 
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                var queryUpdate = @"
                    UPDATE CLIENTE SET 
                        documento = @Documento, nombre = @Nombre, apellido = @Apellido, fecha_nacimiento = @FechaNacimiento,
                        id_genero = @IdGenero, direccion = @Direccion, telefono = @Telefono, email = @Email,
                        id_plan = @IdPlan, fecha_inicio = @FechaInicio, fecha_vencimiento = @FechaVencimiento,
                        estado = @Estado, nota_adicional = @NotaAdicional, huella = @Huella,
                        tipo_sangre = @TipoSangre, alergias = @Alergias, enfermedades_cronicas = @EnfermedadesCronicas,
                        contacto_emergencia_nombre = @ContactoEmergenciaNombre, contacto_emergencia_telefono = @ContactoEmergenciaTelefono,
                        vencimiento_apto_medico = @VencimientoAptoMedico
                    WHERE id_cliente = @IdCliente";
                await connection.ExecuteAsync(queryUpdate, cliente, transaction);

                var montoPlan = await connection.ExecuteScalarAsync<decimal?>("SELECT precio FROM PLANES WHERE id_plan = @IdPlan", new { IdPlan = cliente.IdPlan }, transaction) ?? 0;

                var queryPago = @"
                    INSERT INTO pagos (monto, id_plan, id_cliente, estado)
                    VALUES (@Monto, @IdPlan, @IdCliente, 1)";
                
                await connection.ExecuteAsync(queryPago, new {
                    Monto = montoPlan,
                    IdPlan = cliente.IdPlan,
                    IdCliente = cliente.IdCliente
                }, transaction);

                transaction.Commit();
                _cache.Invalidate();
            }
            catch { transaction.Rollback(); throw; }
        }
        catch (Exception ex)
        {
            throw new Exception("Error al renovar cliente: " + ex.Message);
        }
    }

    public async Task DeleteClienteAsync(int id)
    {
        try 
        {
            using var connection = new SqliteConnection(_connectionString);
            var query = "DELETE FROM CLIENTE WHERE id_cliente = @Id";
            await connection.ExecuteAsync(query, new { Id = id });
            _cache.Invalidate();
        }
        catch (Exception ex)
        {
            throw new Exception("Error al eliminar cliente: " + ex.Message);
        }
    }

    public async Task UpdateClienteAsync(Cliente cliente)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = @"
            UPDATE CLIENTE SET 
                documento = @Documento,
                nombre = @Nombre,
                apellido = @Apellido,
                fecha_nacimiento = @FechaNacimiento,
                id_genero = @IdGenero,
                direccion = @Direccion,
                telefono = @Telefono,
                email = @Email,
                id_plan = @IdPlan,
                fecha_inicio = @FechaInicio,
                fecha_vencimiento = @FechaVencimiento,
                estado = @Estado,
                nota_adicional = @NotaAdicional,
                huella = @Huella,
                tipo_sangre = @TipoSangre,
                alergias = @Alergias,
                enfermedades_cronicas = @EnfermedadesCronicas,
                contacto_emergencia_nombre = @ContactoEmergenciaNombre,
                contacto_emergencia_telefono = @ContactoEmergenciaTelefono,
                vencimiento_apto_medico = @VencimientoAptoMedico
            WHERE id_cliente = @IdCliente";
        await connection.ExecuteAsync(query, cliente);
        _cache.Invalidate();
    }

    public async Task AnularPagoAsync(int idPago)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = "UPDATE pagos SET estado = 0 WHERE id_pago = @Id";
        await connection.ExecuteAsync(query, new { Id = idPago });
        _cache.Invalidate();
    }
}
