using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoreAdmin.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CoreAdmin.Services
{
    public class AsistenciaService
    {
        private readonly string _connectionString = DatabaseInitializer.ConnectionString;

        public AsistenciaService()
        {
            DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        public async Task<int> RegistrarAsistenciaAsync(int idCliente, string tipo = "Entrada")
        {
            using var connection = new SqliteConnection(_connectionString);
            var query = "INSERT INTO ASISTENCIA (id_cliente, tipo) VALUES (@IdCliente, @Tipo); SELECT last_insert_rowid();";
            return await connection.ExecuteScalarAsync<int>(query, new { IdCliente = idCliente, Tipo = tipo });
        }

        public async Task<List<Asistencia>> GetAsistenciasHoyAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            var query = @"
                SELECT 
                    a.id_asistencia as IdAsistencia,
                    a.id_cliente as IdCliente,
                    a.fecha_entrada as FechaEntrada,
                    a.tipo as Tipo,
                    c.nombre as ClienteNombre,
                    c.apellido as ClienteApellido,
                    p.plan_nombre as ClientePlan
                FROM ASISTENCIA a
                JOIN CLIENTE c ON a.id_cliente = c.id_cliente
                JOIN PLANES p ON c.id_plan = p.id_plan
                WHERE date(a.fecha_entrada) = date('now', 'localtime')
                ORDER BY a.fecha_entrada DESC";
            
            var result = await connection.QueryAsync<Asistencia>(query);
            return result.ToList();
        }

        public async Task<int> GetOcupacionActualAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            // Simplificado: Contamos entradas de hoy. En un sistema real restaríamos salidas.
            var query = "SELECT count(*) FROM ASISTENCIA WHERE date(fecha_entrada) = date('now', 'localtime') AND tipo = 'Entrada'";
            return await connection.ExecuteScalarAsync<int>(query);
        }
    }
}
