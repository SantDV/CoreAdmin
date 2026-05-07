using Dapper;
using CoreAdmin.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace CoreAdmin.Services;

public class ClienteService
{
    private readonly string _connectionString = DatabaseInitializer.ConnectionString;
    private readonly DashboardCacheService _cache;
    private readonly LogService _logger;

    public ClienteService(DashboardCacheService cache, LogService logger)
    {
        _cache = cache;
        _logger = logger;
        DefaultTypeMap.MatchNamesWithUnderscores = true; 
    }

    private void ValidarCliente(Cliente cliente)
    {
        cliente.Documento = cliente.Documento?.Trim() ?? "";
        cliente.Nombre = cliente.Nombre?.Trim() ?? "";
        cliente.Apellido = cliente.Apellido?.Trim() ?? "";
        cliente.Email = cliente.Email?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(cliente.Documento)) throw new Exception("El documento es obligatorio.");
        if (string.IsNullOrWhiteSpace(cliente.Nombre)) throw new Exception("El nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(cliente.Apellido)) throw new Exception("El apellido es obligatorio.");
        
        if (cliente.Estado == 1) // 1 = Activo
        {
            if (cliente.IdPlan == null || cliente.IdPlan <= 0) throw new Exception("Debe seleccionar un plan válido para un cliente activo.");
            if (cliente.FechaVencimiento < cliente.FechaInicio) throw new Exception("La fecha de vencimiento no puede ser anterior a la de inicio.");
        }
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
            vencimiento_apto_medico AS VencimientoAptoMedico,
            codigo_asistencia AS CodigoAsistencia
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

    public async Task<List<Cliente>> GetClientesPorVencerAsync(int days = 7)
    {
        using var connection = new SqliteConnection(_connectionString);
        var hoy = DateTime.Now.Date.ToString("yyyy-MM-dd");
        var limite = DateTime.Now.Date.AddDays(days).ToString("yyyy-MM-dd");
        
        var query = SelectClienteScript + $@"
            WHERE estado = {Constants.ClienteEstado.Activo}
              AND date(fecha_vencimiento) >= date(@Hoy)
              AND date(fecha_vencimiento) <= date(@Limite)
            ORDER BY fecha_vencimiento ASC";
            
        var clientes = await connection.QueryAsync<Cliente>(query, new { Hoy = hoy, Limite = limite });
        return clientes.ToList();
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

    public async Task<List<Cliente>> GetClientesFiltradosAsync(string filtro)
    {
        using var connection = new SqliteConnection(_connectionString);
        var hoy = DateTime.Now.Date.ToString("yyyy-MM-dd");
        var inicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).ToString("yyyy-MM-dd");
        var enUnaSemana = DateTime.Now.Date.AddDays(7).ToString("yyyy-MM-dd");

        string condition = filtro switch
        {
            "activos" => $"estado = {Constants.ClienteEstado.Activo}",
            "vencidos" => $"estado = {Constants.ClienteEstado.Activo} AND date(fecha_vencimiento) < date(@Hoy)",
            "por_vencer" => $"estado = {Constants.ClienteEstado.Activo} AND date(fecha_vencimiento) >= date(@Hoy) AND date(fecha_vencimiento) <= date(@SieteDias)",
            "nuevos_mes" => $"date(fecha_registro) >= date(@InicioMes)",
            _ => "1=1"
        };

        var query = $"{SelectClienteScript} WHERE {condition} ORDER BY nombre ASC";
        var clientes = await connection.QueryAsync<Cliente>(query, new { Hoy = hoy, InicioMes = inicioMes, SieteDias = enUnaSemana });
        return clientes.ToList();
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

        var query = $@"
            SELECT 
                (SELECT count(*) FROM CLIENTE) AS TotalClientes,
                (SELECT count(*) FROM CLIENTE WHERE estado = {Constants.ClienteEstado.Activo}) AS ClientesActivos,
                (SELECT count(*) FROM CLIENTE WHERE estado = {Constants.ClienteEstado.Inactivo}) AS ClientesInactivos,
                (SELECT count(*) FROM CLIENTE WHERE date(fecha_registro) = date(@Hoy)) AS RegistrosHoy,
                (SELECT count(*) FROM CLIENTE WHERE date(fecha_registro) >= date(@InicioSemana)) AS RegistrosSemana,
                (SELECT count(*) FROM CLIENTE WHERE date(fecha_registro) >= date(@InicioMes)) AS RegistrosMes,
                (SELECT count(*) FROM CLIENTE WHERE date(fecha_registro) >= date(@InicioAnio)) AS RegistrosAnio,
                (SELECT count(*) FROM CLIENTE WHERE estado = {Constants.ClienteEstado.Activo} AND date(fecha_vencimiento) >= date(@Hoy) AND date(fecha_vencimiento) <= date(@EnUnaSemana)) AS PlanesPorVencer,
                (SELECT count(*) FROM CLIENTE WHERE estado = {Constants.ClienteEstado.Activo} AND date(fecha_vencimiento) < date(@Hoy)) AS PlanesVencidos,
                (SELECT COALESCE(SUM(monto), 0) FROM pagos WHERE estado = {Constants.PagoEstado.Activo} AND date(fecha_registro) >= date(@InicioMes)) AS IngresosMes,
                (SELECT COALESCE(SUM(monto), 0) FROM GASTOS WHERE estado = 1 AND date(fecha_registro) >= date(@InicioMes)) AS EgresosMes
        ";

        var stats = await connection.QuerySingleAsync<DashboardStats>(query, new { Hoy = hoy, InicioSemana = inicioSemana, InicioMes = inicioMes, InicioAnio = inicioAnio, EnUnaSemana = enUnaSemana });
        _cache.SetCachedStats(stats);
        return stats;
    }

    public async Task AddClienteAsync(Cliente cliente)
    {
        try 
        {
            ValidarCliente(cliente);
            cliente.FechaRegistro = DateTime.Now;
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            // Duplicate Check
            var exists = await connection.ExecuteScalarAsync<int>("SELECT count(*) FROM CLIENTE WHERE documento = @Doc", new { Doc = cliente.Documento }) > 0;
            if (exists) throw new Exception($"Ya existe un cliente registrado con el documento: {cliente.Documento}");

            using var transaction = connection.BeginTransaction();
            try
            {
                if (string.IsNullOrWhiteSpace(cliente.CodigoAsistencia))
                {
                    cliente.CodigoAsistencia = await GenerarCodigoUnicoAsync(connection, transaction);
                }

                var queryCliente = @"
                    INSERT INTO CLIENTE (documento, nombre, apellido, fecha_nacimiento, id_genero, direccion, telefono, email, id_plan, fecha_inicio, fecha_vencimiento, fecha_registro, estado, nota_adicional, huella, tipo_sangre, alergias, enfermedades_cronicas, contacto_emergencia_nombre, contacto_emergencia_telefono, vencimiento_apto_medico, codigo_asistencia)
                    VALUES (@Documento, @Nombre, @Apellido, @FechaNacimiento, @IdGenero, @Direccion, @Telefono, @Email, @IdPlan, @FechaInicio, @FechaVencimiento, @FechaRegistro, @Estado, @NotaAdicional, @Huella, @TipoSangre, @Alergias, @EnfermedadesCronicas, @ContactoEmergenciaNombre, @ContactoEmergenciaTelefono, @VencimientoAptoMedico, @CodigoAsistencia);
                    SELECT last_insert_rowid();";
                
                int idCliente = await connection.ExecuteScalarAsync<int>(queryCliente, cliente, transaction);

                var montoPlan = await connection.ExecuteScalarAsync<decimal?>("SELECT precio FROM PLANES WHERE id_plan = @IdPlan", new { IdPlan = cliente.IdPlan }, transaction) ?? 0;

                var queryPago = $@"
                    INSERT INTO pagos (monto, id_plan, id_cliente, fecha_registro, estado)
                    VALUES (@Monto, @IdPlan, @IdCliente, @FechaRegistro, {Constants.PagoEstado.Activo})";
                
                await connection.ExecuteAsync(queryPago, new {
                    Monto = montoPlan,
                    IdPlan = cliente.IdPlan,
                    IdCliente = idCliente,
                    FechaRegistro = cliente.FechaRegistro
                }, transaction);

                transaction.Commit();
                _cache.Invalidate();
            }
            catch (Exception ex) 
            { 
                transaction.Rollback(); 
                _logger.LogError("Error en transacción de AddClienteAsync", ex);
                throw; 
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Fallo al registrar cliente", ex);
            throw;
        }
    }

    public async Task RenovarClienteAsync(Cliente cliente)
    {
        try 
        {
            ValidarCliente(cliente);
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
                        vencimiento_apto_medico = @VencimientoAptoMedico, codigo_asistencia = @CodigoAsistencia
                    WHERE id_cliente = @IdCliente";
                await connection.ExecuteAsync(queryUpdate, cliente, transaction);

                var montoPlan = await connection.ExecuteScalarAsync<decimal?>("SELECT precio FROM PLANES WHERE id_plan = @IdPlan", new { IdPlan = cliente.IdPlan }, transaction) ?? 0;

                var queryPago = $@"
                    INSERT INTO pagos (monto, id_plan, id_cliente, estado)
                    VALUES (@Monto, @IdPlan, @IdCliente, {Constants.PagoEstado.Activo})";
                
                await connection.ExecuteAsync(queryPago, new {
                    Monto = montoPlan,
                    IdPlan = cliente.IdPlan,
                    IdCliente = cliente.IdCliente
                }, transaction);

                transaction.Commit();
                _cache.Invalidate();
            }
            catch (Exception ex) 
            { 
                transaction.Rollback(); 
                _logger.LogError("Error en transacción de RenovarClienteAsync", ex);
                throw; 
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Fallo al renovar cliente", ex);
            throw;
        }
    }

    public async Task UpdateClienteAsync(Cliente cliente)
    {
        try
        {
            ValidarCliente(cliente);
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
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
                        vencimiento_apto_medico = @VencimientoAptoMedico,
                        codigo_asistencia = @CodigoAsistencia
                    WHERE id_cliente = @IdCliente";
                await connection.ExecuteAsync(query, cliente, transaction);
                transaction.Commit();
                _cache.Invalidate();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError("Error al actualizar cliente", ex);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error al actualizar cliente", ex);
            throw;
        }
    }

    public async Task DeleteClienteAsync(int id)
    {
        try 
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            connection.Execute("PRAGMA foreign_keys = ON;"); // Ensure FKs are enabled
            var query = "DELETE FROM CLIENTE WHERE id_cliente = @Id";
            await connection.ExecuteAsync(query, new { Id = id });
            _cache.Invalidate();
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("FOREIGN KEY constraint failed") || ex.Message.Contains("constraint failed"))
            {
                using var connectionUpdate = new SqliteConnection(_connectionString);
                await connectionUpdate.OpenAsync();
                var queryUpdate = "UPDATE CLIENTE SET estado = 0 WHERE id_cliente = @Id";
                await connectionUpdate.ExecuteAsync(queryUpdate, new { Id = id });
                _cache.Invalidate();
                throw new Exception("Auditoría: Este cliente tiene historial de pagos/ventas y no puede ser borrado permanentemente. El sistema lo ha archivado pasándolo a estado INACTIVO.");
            }
            throw new Exception("Error al eliminar cliente: " + ex.Message);
        }
    }

    public async Task AnularPagoAsync(int idPago)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = $"UPDATE pagos SET estado = {Constants.PagoEstado.Anulado} WHERE id_pago = @Id";
        await connection.ExecuteAsync(query, new { Id = idPago });
        _cache.Invalidate();
    }

    private async Task<string> GenerarCodigoUnicoAsync(SqliteConnection connection, SqliteTransaction transaction)
    {
        var random = new Random();
        string code;
        bool exists;
        int attempts = 0;
        do
        {
            code = random.Next(100000, 999999).ToString();
            exists = await connection.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM CLIENTE WHERE codigo_asistencia = @Code", 
                new { Code = code }, transaction) > 0;
            attempts++;
            if (attempts > 100) throw new Exception("No se pudo generar un código de asistencia único después de 100 intentos.");
        } while (exists);
        return code;
    }

    public async Task<List<PagoReporte>> GetPagosPorClienteAsync(int idCliente)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = @"
            SELECT 
                p.id_pago AS IdPago,
                p.id_cliente AS IdCliente,
                pl.plan_nombre AS PlanNombre,
                p.monto AS Monto,
                p.fecha_registro AS FechaRegistro,
                p.estado AS Estado
            FROM pagos p
            LEFT JOIN PLANES pl ON p.id_plan = pl.id_plan
            WHERE p.id_cliente = @IdCliente
            ORDER BY p.fecha_registro DESC";
            
        return (await connection.QueryAsync<PagoReporte>(query, new { IdCliente = idCliente })).ToList();
    }
}
