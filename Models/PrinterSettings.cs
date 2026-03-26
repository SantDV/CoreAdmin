namespace CoreAdmin.Models;

public class PrinterSettings
{
    public string PrinterName { get; set; } = "Genérico / Sólo texto";
    public string PrinterIp { get; set; } = "192.168.1.100";
    public int PrinterPort { get; set; } = 9100;
    public bool UseNetwork { get; set; } = false;
    public bool ShowPrintDialog { get; set; } = true;
}
