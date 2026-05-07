using Dapper;
using CoreAdmin.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

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
        if (venta.Detalles == null || !venta.Detalles.Any())
            throw new Exception("La venta debe contener al menos un producto.");

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();
        try
        {
            // 2. Insertar Venta con número temporal
            var queryVenta = @"
                INSERT INTO VENTA (numero_venta, id_cliente, id_usuario, subtotal, descuento, total, metodo_pago, nota_adicional, estado)
                VALUES ('TEMP', @IdCliente, @IdUsuario, @Subtotal, @Descuento, @Total, @MetodoPago, @NotaAdicional, @Estado);
                SELECT last_insert_rowid();";
            
            venta.Estado = Constants.VentaEstado.Pagada;
            int idVenta = await connection.ExecuteScalarAsync<int>(queryVenta, venta, transaction);

            // Actualizar número de venta definitivo basado en el ID
            venta.NumeroVenta = $"FAC-{idVenta:D6}";
            await connection.ExecuteAsync("UPDATE VENTA SET numero_venta = @Num WHERE id_venta = @Id", new { Num = venta.NumeroVenta, Id = idVenta }, transaction);

            // 3. Insertar Detalles y Actualizar Stock de forma atómica
            foreach (var detalle in venta.Detalles)
            {
                detalle.IdVenta = idVenta;
                var queryDetalle = @"
                    INSERT INTO DETALLE_VENTA (id_venta, id_producto, cantidad, precio_unitario, subtotal)
                    VALUES (@IdVenta, @IdProducto, @Cantidad, @PrecioUnitario, @Subtotal)";
                await connection.ExecuteAsync(queryDetalle, detalle, transaction);

                // Obtener stock actual para el log de movimiento (antes de la actualización atómica)
                var prod = await connection.QuerySingleAsync<Producto>("SELECT stock FROM PRODUCTO WHERE id_producto = @Id", new { Id = detalle.IdProducto }, transaction);
                
                // Actualización Atómica en SQL para evitar condiciones de carrera
                int rowsAffected = await connection.ExecuteAsync(@"
                    UPDATE PRODUCTO 
                    SET stock = stock - @Cantidad 
                    WHERE id_producto = @Id AND stock >= @Cantidad", 
                    new { Cantidad = detalle.Cantidad, Id = detalle.IdProducto }, transaction);

                if (rowsAffected == 0)
                    throw new Exception($"Stock insuficiente para el producto ID {detalle.IdProducto} o el producto no existe.");

                int stockNuevo = prod.Stock - detalle.Cantidad;

                // Registrar Movimiento
                var queryMov = @"
                    INSERT INTO MOVIMIENTO_STOCK (id_producto, tipo_movimiento, cantidad, stock_anterior, stock_nuevo, motivo, id_usuario)
                    VALUES (@IdProducto, @Tipo, @Cantidad, @Anterior, @Nuevo, @Motivo, @IdUsuario)";
                
                await connection.ExecuteAsync(queryMov, new {
                    IdProducto = detalle.IdProducto,
                    Tipo = Constants.MovimientoTipo.Salida,
                    Cantidad = detalle.Cantidad,
                    Anterior = prod.Stock,
                    Nuevo = stockNuevo,
                    Motivo = $"Venta #{venta.NumeroVenta}",
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
            if (venta.Estado == Constants.VentaEstado.Anulada) throw new Exception("La venta ya se encuentra anulada.");

            var detalles = await connection.QueryAsync<DetalleVenta>("SELECT * FROM DETALLE_VENTA WHERE id_venta = @Id", new { Id = idVenta }, transaction);

            // Revertir Stock de forma atómica
            foreach (var d in detalles)
            {
                var prod = await connection.QuerySingleAsync<Producto>("SELECT stock FROM PRODUCTO WHERE id_producto = @Id", new { Id = d.IdProducto }, transaction);
                
                await connection.ExecuteAsync("UPDATE PRODUCTO SET stock = stock + @Cantidad WHERE id_producto = @Id", new { Cantidad = d.Cantidad, Id = d.IdProducto }, transaction);

                int stockNuevo = prod.Stock + d.Cantidad;

                // Registrar Movimiento de reversión
                var queryMov = @"
                    INSERT INTO MOVIMIENTO_STOCK (id_producto, tipo_movimiento, cantidad, stock_anterior, stock_nuevo, motivo, id_usuario)
                    VALUES (@IdProducto, @Tipo, @Cantidad, @Anterior, @Nuevo, @Motivo, @IdUsuario)";
                
                await connection.ExecuteAsync(queryMov, new {
                    IdProducto = d.IdProducto,
                    Tipo = Constants.MovimientoTipo.Entrada,
                    Cantidad = d.Cantidad,
                    Anterior = prod.Stock,
                    Nuevo = stockNuevo,
                    Motivo = $"Anulación Venta #{venta.NumeroVenta}",
                    IdUsuario = idUsuario
                }, transaction);
            }

            await connection.ExecuteAsync("UPDATE VENTA SET estado = @Estado WHERE id_venta = @Id", new { Estado = Constants.VentaEstado.Anulada, Id = idVenta }, transaction);

            transaction.Commit();
            _dashboardCache.Invalidate();
        }
        catch { transaction.Rollback(); throw; }
    }
}
