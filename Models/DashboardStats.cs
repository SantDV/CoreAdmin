namespace GymDashboard.Models;

public class DashboardStats
{
    public int TotalClientes { get; set; }
    public int ClientesActivos { get; set; }
    public int ClientesInactivos { get; set; }
    
    public int RegistrosHoy { get; set; }
    public int RegistrosSemana { get; set; }
    public int RegistrosMes { get; set; }
    public int RegistrosAnio { get; set; }

    public int PlanesPorVencer { get; set; } 
    public int PlanesVencidos { get; set; }
}
