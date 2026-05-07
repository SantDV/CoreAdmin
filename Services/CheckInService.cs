using System;
using System.Threading.Tasks;
using CoreAdmin.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CoreAdmin.Services
{
    public class CheckInResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Cliente? Cliente { get; set; }
        public string StatusColor { get; set; } = string.Empty; // "success", "warning", "danger"
    }

    public class CheckInService
    {
        private readonly string _connectionString = DatabaseInitializer.ConnectionString;
        private readonly AsistenciaService _asistenciaService;

        public event Action<CheckInResult>? OnCheckInProcessed;

        public CheckInService(AsistenciaService asistenciaService)
        {
            _asistenciaService = asistenciaService;
        }

        public async Task ProcessCheckInAsync(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return;

            var result = await ValidateAndRegisterAsync(codigo);
            OnCheckInProcessed?.Invoke(result);
        }

        private async Task<CheckInResult> ValidateAndRegisterAsync(string codigo)
        {
            using var connection = new SqliteConnection(_connectionString);
            
            // Buscar cliente por código de asistencia
            var query = @"
                SELECT 
                    id_cliente AS IdCliente,
                    nombre,
                    apellido,
                    fecha_vencimiento AS FechaVencimiento,
                    estado
                FROM CLIENTE 
                WHERE codigo_asistencia = @Codigo";
            
            var cliente = await connection.QueryFirstOrDefaultAsync<Cliente>(query, new { Codigo = codigo });

            if (cliente == null)
            {
                return new CheckInResult 
                { 
                    Success = false, 
                    Message = "Código no encontrado", 
                    StatusColor = "danger" 
                };
            }

            if (cliente.Estado == 0)
            {
                return new CheckInResult 
                { 
                    Success = false, 
                    Message = "Cliente inactivo", 
                    Cliente = cliente,
                    StatusColor = "warning" 
                };
            }

            // Registrar en DB
            await _asistenciaService.RegistrarAsistenciaAsync(cliente.IdCliente);

            // Determinar estado de membresía
            var hoy = DateTime.Now.Date;
            var vencimiento = cliente.FechaVencimiento?.Date ?? DateTime.MinValue;
            
            if (vencimiento < hoy)
            {
                return new CheckInResult
                {
                    Success = true,
                    Message = $"¡Bienvenido! Pero tu plan ha VENCIDO ({vencimiento:dd/MM/yyyy})",
                    Cliente = cliente,
                    StatusColor = "danger"
                };
            }
            else if ((vencimiento - hoy).TotalDays <= 3)
            {
                return new CheckInResult
                {
                    Success = true,
                    Message = $"¡Bienvenido! Tu plan vence pronto ({vencimiento:dd/MM/yyyy})",
                    Cliente = cliente,
                    StatusColor = "warning"
                };
            }

            return new CheckInResult
            {
                Success = true,
                Message = "¡Ingreso Autorizado!",
                Cliente = cliente,
                StatusColor = "success"
            };
        }
    }
}
