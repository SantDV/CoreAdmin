using System;

namespace CoreAdmin.Models;

public class NotificacionHistorial
{
    public int IdNotificacion { get; set; }
    public int IdCliente { get; set; }
    public string FechaVencimientoAviso { get; set; } = string.Empty;
    public DateTime FechaEnvio { get; set; } = DateTime.Now;
    public string Medio { get; set; } = string.Empty; // "WA" or "EMAIL"
    public int Estado { get; set; } // 1: Success, 0: Failed
}
