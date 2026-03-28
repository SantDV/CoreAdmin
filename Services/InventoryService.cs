using Dapper;
using CoreAdmin.Models;
using Microsoft.Data.Sqlite;

namespace CoreAdmin.Services;

public class InventoryService
{
    private readonly string _connectionString = DatabaseInitializer.ConnectionString;
    private readonly DashboardCacheService _dashboardCache;

    public InventoryService(DashboardCacheService dashboardCache)
    {
        _dashboardCache = dashboardCache;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    #region Categories
    public async Task<List<CategoriaProducto>> GetCategoriasAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = "SELECT id_categoria AS IdCategoria, nombre_categoria AS NombreCategoria, descripcion, estado, fecha_registro AS FechaRegistro FROM CATEGORIA_PRODUCTO WHERE estado = 1";
        return (await connection.QueryAsync<CategoriaProducto>(query)).ToList();
    }

    public async Task AddCategoriaAsync(CategoriaProducto categoria)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = "INSERT INTO CATEGORIA_PRODUCTO (nombre_categoria, descripcion) VALUES (@NombreCategoria, @Descripcion)";
        await connection.ExecuteAsync(query, categoria);
    }

    public async Task UpdateCategoriaAsync(CategoriaProducto categoria)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = "UPDATE CATEGORIA_PRODUCTO SET nombre_categoria = @NombreCategoria, descripcion = @Descripcion WHERE id_categoria = @IdCategoria";
        await connection.ExecuteAsync(query, categoria);
    }

    public async Task DeleteCategoriaAsync(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = "UPDATE CATEGORIA_PRODUCTO SET estado = 0 WHERE id_categoria = @Id";
        await connection.ExecuteAsync(query, new { Id = id });
    }
    #endregion

    #region Products
    public async Task<List<Producto>> GetProductosAsync(string search = "", int? idCategoria = null)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = @"
            SELECT p.*, c.nombre_categoria AS NombreCategoria 
            FROM PRODUCTO p
            INNER JOIN CATEGORIA_PRODUCTO c ON p.id_categoria = c.id_categoria
            WHERE p.estado = 1";
        
        var parameters = new DynamicParameters();
        if (!string.IsNullOrEmpty(search))
        {
            query += " AND (p.nombre LIKE @Search OR p.codigo LIKE @Search)";
            parameters.Add("Search", $"%{search}%");
        }
        if (idCategoria.HasValue)
        {
            query += " AND p.id_categoria = @IdCat";
            parameters.Add("IdCat", idCategoria.Value);
        }

        return (await connection.QueryAsync<Producto>(query, parameters)).ToList();
    }

    public async Task AddProductoAsync(Producto producto, int idUsuario)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();
        try
        {
            var query = @"
                INSERT INTO PRODUCTO (codigo, nombre, descripcion, id_categoria, precio_compra, precio_venta, stock, stock_minimo)
                VALUES (@Codigo, @Nombre, @Descripcion, @IdCategoria, @PrecioCompra, @PrecioVenta, @Stock, @StockMinimo);
                SELECT last_insert_rowid();";
            
            int id = await connection.ExecuteScalarAsync<int>(query, producto, transaction);

            // Registro inicial de stock si es > 0
            if (producto.Stock > 0)
            {
                var queryMov = @"
                    INSERT INTO MOVIMIENTO_STOCK (id_producto, tipo_movimiento, cantidad, stock_anterior, stock_nuevo, motivo, id_usuario)
                    VALUES (@IdProducto, 'ENTRADA', @Cantidad, 0, @Cantidad, 'Registro inicial', @IdUsuario)";
                await connection.ExecuteAsync(queryMov, new { IdProducto = id, Cantidad = producto.Stock, IdUsuario = idUsuario }, transaction);
            }

            transaction.Commit();
            _dashboardCache.Invalidate();
        }
        catch { transaction.Rollback(); throw; }
    }

    public async Task UpdateProductoAsync(Producto producto)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = @"
            UPDATE PRODUCTO SET 
                codigo = @Codigo, nombre = @Nombre, descripcion = @Descripcion, 
                id_categoria = @IdCategoria, precio_compra = @PrecioCompra, 
                precio_venta = @PrecioVenta, stock_minimo = @StockMinimo
            WHERE id_producto = @IdProducto";
        await connection.ExecuteAsync(query, producto);
        _dashboardCache.Invalidate();
    }

    public async Task DeleteProductoAsync(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = "UPDATE PRODUCTO SET estado = 0 WHERE id_producto = @Id";
        await connection.ExecuteAsync(query, new { Id = id });
        _dashboardCache.Invalidate();
    }

    public async Task AjustarStockAsync(int idProducto, int cantidad, string motivo, int idUsuario)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();
        try
        {
            var p = await connection.QuerySingleAsync<Producto>("SELECT stock FROM PRODUCTO WHERE id_producto = @Id", new { Id = idProducto }, transaction);
            int stockNuevo = p.Stock + cantidad;

            if (stockNuevo < 0) throw new Exception("No hay stock suficiente para realizar este ajuste.");

            await connection.ExecuteAsync("UPDATE PRODUCTO SET stock = @Nuevo WHERE id_producto = @Id", new { Nuevo = stockNuevo, Id = idProducto }, transaction);

            var queryMov = @"
                INSERT INTO MOVIMIENTO_STOCK (id_producto, tipo_movimiento, cantidad, stock_anterior, stock_nuevo, motivo, id_usuario)
                VALUES (@IdProducto, @Tipo, @Cantidad, @Anterior, @Nuevo, @Motivo, @IdUsuario)";
            
            await connection.ExecuteAsync(queryMov, new {
                IdProducto = idProducto,
                Tipo = cantidad >= 0 ? "ENTRADA" : "SALIDA",
                Cantidad = Math.Abs(cantidad),
                Anterior = p.Stock,
                Nuevo = stockNuevo,
                Motivo = motivo,
                IdUsuario = idUsuario
            }, transaction);

            transaction.Commit();
            _dashboardCache.Invalidate();
        }
        catch { transaction.Rollback(); throw; }
    }
    #endregion
}
