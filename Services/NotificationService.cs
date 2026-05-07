using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CoreAdmin.Models;
using Dapper;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Http;
using MimeKit;

namespace CoreAdmin.Services;

public class NotificationService
{
    private readonly string _connectionString = DatabaseInitializer.ConnectionString;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ClienteService _clienteService;

    public NotificationService(IHttpClientFactory httpClientFactory, ClienteService clienteService)
    {
        _httpClientFactory = httpClientFactory;
        _clienteService = clienteService;
    }

    public async Task<NotificacionConfig> GetConfigAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        var config = await connection.QueryFirstOrDefaultAsync<NotificacionConfig>("SELECT * FROM CONFIGURACION_NOTIFICACIONES LIMIT 1");
        return config ?? new NotificacionConfig();
    }

    public async Task SaveConfigAsync(NotificacionConfig config)
    {
        using var connection = new SqliteConnection(_connectionString);
        var exists = await connection.ExecuteScalarAsync<int>("SELECT count(*) FROM CONFIGURACION_NOTIFICACIONES");
        if (exists > 0)
        {
            await connection.ExecuteAsync(@"UPDATE CONFIGURACION_NOTIFICACIONES SET 
                smtp_host = @SmtpHost, smtp_port = @SmtpPort, smtp_user = @SmtpUser, 
                smtp_password = @SmtpPassword, smtp_ssl = @SmtpSsl, wa_api_url = @WaApiUrl, 
                wa_instance = @WaInstance, wa_token = @WaToken, mensaje_template = @MensajeTemplate, 
                email_activo = @EmailActivo, wa_activo = @WaActivo WHERE id = 1", config);
        }
        else
        {
            await connection.ExecuteAsync(@"INSERT INTO CONFIGURACION_NOTIFICACIONES 
                (smtp_host, smtp_port, smtp_user, smtp_password, smtp_ssl, wa_api_url, wa_instance, wa_token, mensaje_template, email_activo, wa_activo) 
                VALUES (@SmtpHost, @SmtpPort, @SmtpUser, @SmtpPassword, @SmtpSsl, @WaApiUrl, @WaInstance, @WaToken, @MensajeTemplate, @EmailActivo, @WaActivo)", config);
        }
    }

    public async Task CheckAndSendExpirationsAsync()
    {
        var config = await GetConfigAsync();
        if (!config.EmailActivo && !config.WaActivo) return;

        // Buscamos clientes que venzan en exactamente 3 días
        var fechaObjetivo = DateTime.Now.Date.AddDays(3).ToString("yyyy-MM-dd");
        
        using var connection = new SqliteConnection(_connectionString);
        var query = @"SELECT * FROM CLIENTE WHERE estado = 1 AND date(fecha_vencimiento) = date(@Fecha)";
        var clientes = (await connection.QueryAsync<Cliente>(query, new { Fecha = fechaObjetivo })).ToList();

        foreach (var cliente in clientes)
        {
            // Verificar si ya fue notificado por este vencimiento
            var yaNotificado = await connection.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM NOTIFICACIONES_HISTORIAL WHERE id_cliente = @Id AND fecha_vencimiento_aviso = @Fecha", 
                new { Id = cliente.IdCliente, Fecha = fechaObjetivo }) > 0;

            if (yaNotificado) continue;

            bool successEmail = false;
            bool successWA = false;

            if (config.EmailActivo && !string.IsNullOrEmpty(cliente.Email))
            {
                successEmail = await SendEmailAsync(cliente, config);
                await RegistrarHistorial(cliente.IdCliente, fechaObjetivo, "EMAIL", successEmail ? 1 : 0);
            }

            if (config.WaActivo && !string.IsNullOrEmpty(cliente.Telefono))
            {
                successWA = await SendWhatsAppAsync(cliente, config);
                await RegistrarHistorial(cliente.IdCliente, fechaObjetivo, "WA", successWA ? 1 : 0);
            }
        }
    }

    public async Task<bool> SendEmailAsync(Cliente cliente, NotificacionConfig config)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Gimnasio CoreAdmin", config.SmtpUser ?? ""));
            message.To.Add(new MailboxAddress($"{cliente.Nombre} {cliente.Apellido}", cliente.Email ?? ""));
            message.Subject = "Tu membresía está por vencer - CoreAdmin";

            var body = ReemplazarTags(config.MensajeTemplate, cliente);
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(config.SmtpHost, config.SmtpPort, config.SmtpSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
            await client.AuthenticateAsync(config.SmtpUser ?? "", config.SmtpPassword ?? "");
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> SendWhatsAppAsync(Cliente cliente, NotificacionConfig config)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("apikey", config.WaToken);

            var url = $"{config.WaApiUrl.TrimEnd('/')}/message/sendText/{config.WaInstance}";
            var telefono = NormalizarTelefono(cliente.Telefono!);
            
            var payload = new
            {
                number = telefono,
                options = new { delay = 1200, presence = "composing", linkPreview = false },
                textMessage = new { text = ReemplazarTags(config.MensajeTemplate, cliente) }
            };

            var response = await client.PostAsJsonAsync(url, payload);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task RegistrarHistorial(int idCliente, string fechaVencimiento, string medio, int estado)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync(@"INSERT INTO NOTIFICACIONES_HISTORIAL 
            (id_cliente, fecha_vencimiento_aviso, medio, estado) 
            VALUES (@Id, @Fecha, @Medio, @Estado)", 
            new { Id = idCliente, Fecha = fechaVencimiento, Medio = medio, Estado = estado });
    }

    private string ReemplazarTags(string template, Cliente cliente)
    {
        return template
            .Replace("{nombre}", cliente.Nombre)
            .Replace("{apellido}", cliente.Apellido)
            .Replace("{fecha}", cliente.FechaVencimiento?.ToString("dd/MM/yyyy") ?? "");
    }

    private string NormalizarTelefono(string tel)
    {
        // Limpiamos caracteres no numéricos
        var sb = new StringBuilder();
        foreach (char c in tel) if (char.IsDigit(c)) sb.Append(c);
        
        var limpio = sb.ToString();
        
        // Si no tiene prefijo de país, asumimos el del usuario o agregamos uno común si es necesario.
        // Evolution API suele requerir el número completo con código de país.
        return limpio; 
    }

    public async Task<List<NotificacionHistorial>> GetHistorialRecienteAsync(int limit = 20)
    {
        using var connection = new SqliteConnection(_connectionString);
        return (await connection.QueryAsync<NotificacionHistorial>(
            "SELECT * FROM NOTIFICACIONES_HISTORIAL ORDER BY id_notificacion DESC LIMIT @Limit", 
            new { Limit = limit })).ToList();
    }
}
