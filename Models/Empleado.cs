namespace GymDashboard.Models;

public class Empleado
{
    public int IdEmpleado { get; set; }
    public string? Documento { get; set; }
    public string? Nombre { get; set; }
    public string? Apellido { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public int? IdUsuario { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime? FechaRegistro { get; set; }
    
    // Virtual Properties (Mapped strictly from USUARIO JOIN)
    public string? NombreUsuario { get; set; }
    public string? Clave { get; set; }
    public int? IdRol { get; set; }
}
