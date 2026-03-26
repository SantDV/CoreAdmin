using System.Net.Sockets;
using System.Text;
using CoreAdmin.Models;

namespace CoreAdmin.Services;

public class PrinterService
{
    private readonly SettingsService _settingsService;

    public PrinterService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task ImprimirPagoAsync(PagoReporte pago, string atendidoPor)
    {
        var settings = await _settingsService.GetPrinterSettingsAsync();

        byte[] commands = GenerateEscPosReceipt(pago, atendidoPor);

        if (settings.UseNetwork)
        {
            await SendToNetworkPrinter(settings.PrinterIp, settings.PrinterPort, commands);
        }
    }

    private byte[] GenerateEscPosReceipt(PagoReporte pago, string atendidoPor)
    {
        var bytes = new List<byte>();

        // ESC/POS Comandos básicos
        byte[] ESC = { 0x1B };
        byte[] GS = { 0x1D };
        byte[] Initialize = { 0x1B, 0x40 };
        byte[] Center = { 0x1B, 0x61, 0x01 };
        byte[] Left = { 0x1B, 0x61, 0x00 };
        byte[] BoldOn = { 0x1B, 0x45, 0x01 };
        byte[] BoldOff = { 0x1B, 0x45, 0x00 };
        byte[] DoubleHeight = { 0x1B, 0x21, 0x10 };
        byte[] ResetSize = { 0x1B, 0x21, 0x00 };
        byte[] Cut = { 0x1D, 0x56, 0x41, 0x00 }; // Partial cut for XPrinter

        // Feed command
        byte[] Feed = { 0x1B, 0x64, 0x05 }; // Feed 5 lines

        // Codificación
        var encoding = Encoding.GetEncoding("Windows-1252");

        bytes.AddRange(Initialize);
        bytes.AddRange(Center);
        bytes.AddRange(BoldOn);
        bytes.AddRange(DoubleHeight);
        bytes.AddRange(encoding.GetBytes("COREADMIN\n"));
        bytes.AddRange(ResetSize);
        bytes.AddRange(BoldOff);
        bytes.AddRange(encoding.GetBytes("Comprobante de Pago\n"));
        bytes.AddRange(encoding.GetBytes($"Ticket Nro: #{pago.IdPago:D5}\n"));
        bytes.AddRange(encoding.GetBytes("================================\n"));
        
        bytes.AddRange(Left);
        bytes.AddRange(encoding.GetBytes($"Fecha:   {pago.FechaRegistro:dd/MM/yyyy HH:mm}\n"));
        bytes.AddRange(encoding.GetBytes($"Cliente: {pago.Nombre} {pago.Apellido}\n"));
        bytes.AddRange(encoding.GetBytes($"ID:      {pago.IdCliente}\n"));
        bytes.AddRange(encoding.GetBytes("--------------------------------\n"));
        
        bytes.AddRange(BoldOn);
        bytes.AddRange(encoding.GetBytes($"PLAN:    {pago.PlanNombre}\n"));
        bytes.AddRange(encoding.GetBytes($"IMPORTE: ${pago.Monto:N2}\n"));
        bytes.AddRange(BoldOff);
        bytes.AddRange(encoding.GetBytes("--------------------------------\n"));
        
        bytes.AddRange(Center);
        bytes.AddRange(BoldOn);
        bytes.AddRange(DoubleHeight);
        bytes.AddRange(encoding.GetBytes($"TOTAL: ${pago.Monto:N2}\n"));
        bytes.AddRange(ResetSize);
        bytes.AddRange(BoldOff);
        
        bytes.AddRange(encoding.GetBytes("\n"));
        bytes.AddRange(encoding.GetBytes($"Atendido por: {atendidoPor}\n"));
        bytes.AddRange(encoding.GetBytes("¡Gracias por su pago!\n"));
        
        bytes.AddRange(Feed);
        bytes.AddRange(Cut);

        return bytes.ToArray();
    }

    private async Task SendToNetworkPrinter(string ip, int port, byte[] data)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(ip, port);
            using var stream = client.GetStream();
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();
        }
        catch (Exception ex)
        {
            // Podríamos loguear o lanzar la excepción para que Blazor la muestre
            throw new Exception($"Error de conexión con la impresora: {ex.Message}");
        }
    }
}
