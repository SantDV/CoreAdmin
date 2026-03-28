namespace CoreAdmin.Models;

public class MovimientoStock
{
    public int IdMovimiento { get; set; }
    public int IdProducto { get; set; }
    public string TipoMovimiento { get; set; } = "ENTRADA"; // ENTRADA, SALIDA
    public int Cantidad { get; set; }
    public int StockAnterior { get; set; }
    public int StockNuevo { get; set; }
    public string? Motivo { get; set; }
    public int IdUsuario { get; set; }
    public DateTime FechaMovimiento { get; set; }
}
