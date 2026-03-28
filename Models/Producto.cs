namespace CoreAdmin.Models;

public class Producto
{
    public int IdProducto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int IdCategoria { get; set; }
    public string? NombreCategoria { get; set; } // For display
    public decimal PrecioCompra { get; set; }
    public decimal PrecioVenta { get; set; }
    public int Stock { get; set; }
    public int StockMinimo { get; set; } = 5;
    public int Estado { get; set; } = 1;
    public DateTime FechaRegistro { get; set; }
}
