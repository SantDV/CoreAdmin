namespace CoreAdmin.Models;

public static class Constants
{
    public static class VentaEstado
    {
        public const string Pagada = "Pagada";
        public const string Anulada = "Anulada";
    }

    public static class MovimientoTipo
    {
        public const string Entrada = "ENTRADA";
        public const string Salida = "SALIDA";
    }

    public static class ClienteEstado
    {
        public const int Activo = 1;
        public const int Inactivo = 0;
    }

    public static class PagoEstado
    {
        public const int Activo = 1;
        public const int Anulado = 0;
    }
}
