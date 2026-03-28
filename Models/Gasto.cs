using System;
using System.ComponentModel.DataAnnotations;

namespace CoreAdmin.Models;

public class Gasto
{
    public int IdGasto { get; set; }
    
    [Required(ErrorMessage = "La descripción es obligatoria")]
    public string? Descripcion { get; set; }
    
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
    public decimal Monto { get; set; }
    
    public string? Categoria { get; set; }
    
    public DateTime? FechaRegistro { get; set; }
    
    public int Estado { get; set; } = 1; // 1: Activo, 0: Anulado
}
