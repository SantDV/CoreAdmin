namespace CoreAdmin.Models;

public class Cliente
{
    public int IdCliente { get; set; }
    public string? Documento { get; set; }
    public string? Nombre { get; set; }
    public string? Apellido { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public int? IdGenero { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public int? IdPlan { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public DateTime? FechaRegistro { get; set; }
    public int Estado { get; set; } = 1;
    public string? NotaAdicional { get; set; }
    public string? Huella { get; set; }
    public string? TipoSangre { get; set; }
    public string? Alergias { get; set; }
    public string? EnfermedadesCronicas { get; set; }
    public string? ContactoEmergenciaNombre { get; set; }
    public string? ContactoEmergenciaTelefono { get; set; }
    public DateTime? VencimientoAptoMedico { get; set; }
    public string? CodigoAsistencia { get; set; }
}
