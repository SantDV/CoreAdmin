using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;
using GymDashboard.Services;

namespace GymDashboard;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();

        this.Text = "Gym Dashboard Pro";
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

        var blazorWebView = new BlazorWebView
        {
            Dock = DockStyle.Fill,
            HostPage = @"wwwroot\index.html",
            Services = services.BuildServiceProvider()
        };

        blazorWebView.RootComponents.Add<Main>("#app");
        Controls.Add(blazorWebView);
    }
}
