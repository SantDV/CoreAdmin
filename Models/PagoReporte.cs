namespace CoreAdmin.Models;

public class PagoReporte
{
    public int IdPago { get; set; }
    public int? IdCliente { get; set; }
    public string? Nombre { get; set; }
    public string? Apellido { get; set; }
    public string? PlanNombre { get; set; }
    public decimal Monto { get; set; }
    public DateTime? FechaRegistro { get; set; }
}
