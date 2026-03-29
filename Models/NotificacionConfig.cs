namespace CoreAdmin.Models;

public class NotificacionConfig
{
    public int Id { get; set; }
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public bool SmtpSsl { get; set; } = true;

    public string WaApiUrl { get; set; } = string.Empty;
    public string WaInstance { get; set; } = string.Empty;
    public string WaToken { get; set; } = string.Empty;

    public string MensajeTemplate { get; set; } = "Hola {nombre}, te recordamos que tu membresía vence el {fecha}. ¡Te esperamos!";
    
    public bool EmailActivo { get; set; }
    public bool WaActivo { get; set; }
}
