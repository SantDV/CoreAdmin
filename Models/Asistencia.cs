using System;

namespace CoreAdmin.Models
{
    public class Asistencia
    {
        public int IdAsistencia { get; set; }
        public int IdCliente { get; set; }
        public DateTime FechaEntrada { get; set; }
        public string Tipo { get; set; } = "Entrada";

        // Propiedades de navegación (opcionales para Dapper)
        public string ClienteNombre { get; set; } = string.Empty;
        public string ClienteApellido { get; set; } = string.Empty;
        public string ClientePlan { get; set; } = string.Empty;
        public string ClienteEstado { get; set; } = string.Empty;
    }
}
