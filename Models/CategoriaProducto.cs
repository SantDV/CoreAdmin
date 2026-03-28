namespace CoreAdmin.Models;

public class CategoriaProducto
{
    public int IdCategoria { get; set; }
    public string NombreCategoria { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime FechaRegistro { get; set; }
}
