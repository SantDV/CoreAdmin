using System.ComponentModel.DataAnnotations;

namespace CoreAdmin.Models;

public class Planes
{
    public int IdPlan { get; set; }
    
    [Required(ErrorMessage = "El nombre del plan es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre no puede exceder los 50 caracteres.")]
    public string? PlanNombre { get; set; }
    
    [Range(0.01, 1000000, ErrorMessage = "El precio debe ser mayor a 0.")]
    public decimal Precio { get; set; }
}
