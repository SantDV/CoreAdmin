using Dapper;
using CoreAdmin.Models;
using Microsoft.Data.Sqlite;

namespace CoreAdmin.Services;

public class VentaService
{
    private readonly string _connectionString = DatabaseInitializer.ConnectionString;
    private readonly DashboardCacheService _dashboardCache;

    public VentaService(DashboardCacheService dashboardCache)
    {
        _dashboardCache = dashboardCache;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public async Task<string> ProcessVentaAsync(Venta venta)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();
        try
        {
            // 1. Generar número de venta si no existe
            if (string.IsNullOrEmpty(venta.NumeroVenta))
            {
                var count = await connection.ExecuteScalarAsync<int>("SELECT count(*) FROM VENTA", null, transaction);
                venta.NumeroVenta = $"V-{(count + 1):D6}";
            }

            // 2. Insertar Venta
            var queryVenta = @"
                INSERT INTO VENTA (numero_venta, id_cliente, id_usuario, subtotal, descuento, total, metodo_pago, nota_adicional)
                VALUES (@NumeroVenta, @IdCliente, @IdUsuario, @Subtotal, @Descuento, @Total, @MetodoPago, @NotaAdicional);
                SELECT last_insert_rowid();";
            
            int idVenta = await connection.ExecuteScalarAsync<int>(queryVenta, venta, transaction);

            // 3. Insertar Detalles y Actualizar Stock
            foreach (var detalle in venta.Detalles)
            {
                detalle.IdVenta = idVenta;
                var queryDetalle = @"
                    INSERT INTO DETALLE_VENTA (id_venta, id_producto, cantidad, precio_unitario, subtotal)
                    VALUES (@IdVenta, @IdProducto, @Cantidad, @PrecioUnitario, @Subtotal)";
                await connection.ExecuteAsync(queryDetalle, detalle, transaction);

                // Actualizar Stock del producto
                var prod = await connection.QuerySingleAsync<Producto>("SELECT stock FROM PRODUCTO WHERE id_producto = @Id", new { Id = detalle.IdProducto }, transaction);
                int stockNuevo = prod.Stock - detalle.Cantidad;

                if (stockNuevo < 0) throw new Exception($"Stock insuficiente para el producto ID {detalle.IdProducto}");

                await connection.ExecuteAsync("UPDATE PRODUCTO SET stock = @Nuevo WHERE id_producto = @Id", new { Nuevo = stockNuevo, Id = detalle.IdProducto }, transaction);

                // Registrar Movimiento
                var queryMov = @"
                    INSERT INTO MOVIMIENTO_STOCK (id_producto, tipo_movimiento, cantidad, stock_anterior, stock_nuevo, motivo, id_usuario)
                    VALUES (@IdProducto, 'SALIDA', @Cantidad, @Anterior, @Nuevo, 'Venta #' || @NumVenta, @IdUsuario)";
                
                await connection.ExecuteAsync(queryMov, new {
                    IdProducto = detalle.IdProducto,
                    Cantidad = detalle.Cantidad,
                    Anterior = prod.Stock,
                    Nuevo = stockNuevo,
                    NumVenta = venta.NumeroVenta,
                    IdUsuario = venta.IdUsuario
                }, transaction);
            }

            transaction.Commit();
            _dashboardCache.Invalidate();
            return venta.NumeroVenta;
        }
        catch { transaction.Rollback(); throw; }
    }

    public async Task<List<Venta>> GetVentasAsync(DateTime? desde = null, DateTime? hasta = null, string search = "")
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = @"
            SELECT v.*, c.nombre || ' ' || c.apellido AS NombreCliente, u.nombre_usuario AS NombreUsuario
            FROM VENTA v
            LEFT JOIN CLIENTE c ON v.id_cliente = c.id_cliente
            INNER JOIN USUARIO u ON v.id_usuario = u.id_usuario
            WHERE 1=1";
        
        var parameters = new DynamicParameters();
        if (desde.HasValue) { query += " AND date(v.fecha_venta) >= date(@Desde)"; parameters.Add("Desde", desde.Value.ToString("yyyy-MM-dd")); }
        if (hasta.HasValue) { query += " AND date(v.fecha_venta) <= date(@Hasta)"; parameters.Add("Hasta", hasta.Value.ToString("yyyy-MM-dd")); }
        if (!string.IsNullOrEmpty(search)) { query += " AND (v.numero_venta LIKE @Search OR c.nombre LIKE @Search OR c.apellido LIKE @Search)"; parameters.Add("Search", $"%{search}%"); }

        query += " ORDER BY v.fecha_venta DESC";
        
        return (await connection.QueryAsync<Venta>(query, parameters)).ToList();
    }

    public async Task<List<DetalleVenta>> GetDetallesVentaAsync(int idVenta)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = @"
            SELECT d.*, p.nombre AS NombreProducto
            FROM DETALLE_VENTA d
            INNER JOIN PRODUCTO p ON d.id_producto = p.id_producto
            WHERE d.id_venta = @IdVenta";
        return (await connection.QueryAsync<DetalleVenta>(query, new { IdVenta = idVenta })).ToList();
    }

    public async Task AnularVentaAsync(int idVenta, int idUsuario)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();
        try
        {
            var venta = await connection.QuerySingleAsync<Venta>("SELECT * FROM VENTA WHERE id_venta = @Id", new { Id = idVenta }, transaction);
            if (venta.Estado == "Anulada") throw new Exception("La venta ya se encuentra anulada.");

            var detalles = await connection.QueryAsync<DetalleVenta>("SELECT * FROM DETALLE_VENTA WHERE id_venta = @Id", new { Id = idVenta }, transaction);

            // Revertir Stock
            foreach (var d in detalles)
            {
                var prod = await connection.QuerySingleAsync<Producto>("SELECT stock FROM PRODUCTO WHERE id_producto = @Id", new { Id = d.IdProducto }, transaction);
                int stockNuevo = prod.Stock + d.Cantidad;

                await connection.ExecuteAsync("UPDATE PRODUCTO SET stock = @Nuevo WHERE id_producto = @Id", new { Nuevo = stockNuevo, Id = d.IdProducto }, transaction);

                // Registrar Movimiento de reversión
                var queryMov = @"
                    INSERT INTO MOVIMIENTO_STOCK (id_producto, tipo_movimiento, cantidad, stock_anterior, stock_nuevo, motivo, id_usuario)
                    VALUES (@IdProducto, 'ENTRADA', @Cantidad, @Anterior, @Nuevo, 'Anulación Venta #' || @NumVenta, @IdUsuario)";
                
                await connection.ExecuteAsync(queryMov, new {
                    IdProducto = d.IdProducto,
                    Cantidad = d.Cantidad,
                    Anterior = prod.Stock,
                    Nuevo = stockNuevo,
                    NumVenta = venta.NumeroVenta,
                    IdUsuario = idUsuario
                }, transaction);
            }

            await connection.ExecuteAsync("UPDATE VENTA SET estado = 'Anulada' WHERE id_venta = @Id", new { Id = idVenta }, transaction);

            transaction.Commit();
            _dashboardCache.Invalidate();
        }
        catch { transaction.Rollback(); throw; }
    }
}
