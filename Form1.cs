using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;
using CoreAdmin.Services;

namespace CoreAdmin;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();

        this.Text = "CoreAdmin Pro";
        this.Width = 1366;
        this.Height = 768;
        this.StartPosition = FormStartPosition.CenterScreen;

        DatabaseInitializer.Initialize();

        var services = new ServiceCollection();
        services.AddWindowsFormsBlazorWebView();
#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif
        
        services.AddSingleton<ClienteService>();
        services.AddSingleton<AuthService>();
        services.AddSingleton<EmpleadoService>();
        services.AddSingleton<ReportesService>();
        services.AddSingleton<SettingsService>();
        services.AddSingleton<PrinterService>();
        services.AddSingleton<GastoService>();
        services.AddSingleton<DashboardCacheService>();
        services.AddSingleton<FileService>();
        services.AddSingleton<InventoryService>();
        services.AddSingleton<VentaService>();
        services.AddSingleton<NotificationService>();
        services.AddHttpClient();

        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        var blazorWebView = new BlazorWebView
        {
            Dock = DockStyle.Fill,
            HostPage = @"wwwroot\index.html",
            Services = services.BuildServiceProvider()
        };

        blazorWebView.RootComponents.Add<Main>("#app");
        Controls.Add(blazorWebView);

        // Disparar chequeo de notificaciones en segundo plano
        var sp = blazorWebView.Services;
        _ = Task.Run(async () => {
            try {
                using var scope = sp.CreateScope();
                var ns = scope.ServiceProvider.GetRequiredService<NotificationService>();
                await ns.CheckAndSendExpirationsAsync();
            } catch { /* Ignorar errores en inicio silencioso */ }
        });
    }
}
