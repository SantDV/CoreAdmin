using Dapper;
using CoreAdmin.Models;
using Microsoft.Data.Sqlite;

namespace CoreAdmin.Services;

public class ReportesService
{
    private readonly string _connectionString = DatabaseInitializer.ConnectionString;

    public async Task<List<PagoReporte>> GetReportePagosAsync(DateTime? desde = null, DateTime? hasta = null, string searchTerm = "", int limit = 100, int offset = 0)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = @"
            SELECT 
                p.id_pago AS IdPago,
                p.id_cliente AS IdCliente,
                c.nombre AS Nombre,
                c.apellido AS Apellido,
                pl.plan_nombre AS PlanNombre,
                p.monto AS Monto,
                p.fecha_registro AS FechaRegistro
            FROM pagos p
            LEFT JOIN CLIENTE c ON p.id_cliente = c.id_cliente
            LEFT JOIN PLANES pl ON p.id_plan = pl.id_plan
            WHERE p.estado = 1 ";

        var parameters = new DynamicParameters();
        parameters.Add("Limit", limit);
        parameters.Add("Offset", offset);

        if (desde.HasValue)
        {
            query += " AND date(p.fecha_registro) >= date(@Desde)";
            parameters.Add("Desde", desde.Value.ToString("yyyy-MM-dd"));
        }
        if (hasta.HasValue)
        {
            // Cover through the end of the day
            query += " AND p.fecha_registro <= @Hasta";
            parameters.Add("Hasta", hasta.Value.ToString("yyyy-MM-dd 23:59:59"));
        }
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query += " AND (c.nombre LIKE @Search OR c.apellido LIKE @Search OR pl.plan_nombre LIKE @Search)";
            parameters.Add("Search", $"%{searchTerm}%");
        }

        query += " ORDER BY p.id_pago DESC LIMIT @Limit OFFSET @Offset";

        return (await connection.QueryAsync<PagoReporte>(query, parameters)).ToList();
    }

    public async Task<byte[]> GenerateExcelReportAsync(List<PagoReporte> data)
    {
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Reporte Financiero");

        // Headers
        worksheet.Cell(1, 1).Value = "ID Comprobante";
        worksheet.Cell(1, 2).Value = "Cliente";
        worksheet.Cell(1, 3).Value = "Plan";
        worksheet.Cell(1, 4).Value = "Fecha";
        worksheet.Cell(1, 5).Value = "Monto";

        // Styling headers
        var headerRange = worksheet.Range(1, 1, 1, 5);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

        // Data
        for (int i = 0; i < data.Count; i++)
        {
            var row = i + 2;
            var item = data[i];
            worksheet.Cell(row, 1).Value = item.IdPago;
            worksheet.Cell(row, 2).Value = $"{item.Nombre} {item.Apellido}";
            worksheet.Cell(row, 3).Value = item.PlanNombre;
            worksheet.Cell(row, 4).Value = item.FechaRegistro;
            worksheet.Cell(row, 5).Value = item.Monto;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
