namespace CoreAdmin.Models;

public class Venta
{
    public int IdVenta { get; set; }
    public string NumeroVenta { get; set; } = string.Empty;
    public int? IdCliente { get; set; }
    public string? NombreCliente { get; set; } // For display
    public int IdUsuario { get; set; }
    public string? NombreUsuario { get; set; } // For display
    public DateTime FechaVenta { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
    public string MetodoPago { get; set; } = "Efectivo";
    public string Estado { get; set; } = "Completada";
    public string? NotaAdicional { get; set; }
    
    public List<DetalleVenta> Detalles { get; set; } = new();
}
