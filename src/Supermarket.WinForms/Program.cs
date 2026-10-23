using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Supermarket.Data;
using Supermarket.WinForms;
using Supermarket.WinForms.Infrastructure;
using Supermarket.WinForms.Views;

ApplicationConfiguration.Initialize();

HostApplicationBuilder builder;
try
{
    // The content root is the executable folder, not the working directory, so appsettings.json
    // is found even when the app is started from a shortcut.
    builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
    {
        Args = args,
        ContentRootPath = AppContext.BaseDirectory,
    });

    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);
}
catch (InvalidDataException ex)
{
    // Logging is not available yet: it is configured from the very file that failed to load.
    ShowStartupError("Configuración inválida", $"No se pudo leer la configuración.{Environment.NewLine}{Environment.NewLine}{ex.Message}");
    return 1;
}

var connectionString = builder.Configuration.GetConnectionString("Supermarket");
if (string.IsNullOrWhiteSpace(connectionString))
{
    ShowStartupError("Configuración incompleta", "Falta la cadena de conexión \"Supermarket\" en appsettings.json.");
    return 1;
}

builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.AddSqlServerData(connectionString);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ModuleFactory>();
builder.Services.AddSingleton<MainForm>();

using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Supermarket");

// Last line of defense: presenters already handle expected failures, this catches bugs.
Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
Application.ThreadException += (_, e) =>
{
    logger.UnhandledUiException(e.Exception);
    MessageBox.Show(
        "Ocurrió un error inesperado. Los detalles se registraron en el log de la aplicación.",
        "Error",
        MessageBoxButtons.OK,
        MessageBoxIcon.Error);
};
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
    logger.UnhandledException(e.ExceptionObject as Exception);

var options = host.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
if (options.InitializeOnStartup)
{
    try
    {
        // Runs before any window exists, so there is no synchronization context to deadlock on.
        host.Services.GetRequiredService<DatabaseInitializer>()
            .InitializeAsync(options.SeedSampleData)
            .GetAwaiter()
            .GetResult();
    }
    catch (Exception ex)
    {
        logger.DatabaseInitializationFailed(ex);
        ShowStartupError(
            "Error de conexión",
            $"No fue posible conectarse a la base de datos.{Environment.NewLine}{Environment.NewLine}" +
            $"Verifique que SQL Server esté en ejecución y que la cadena de conexión de appsettings.json sea correcta." +
            $"{Environment.NewLine}{Environment.NewLine}Detalle: {ex.Message}");
        return 1;
    }
}

Application.Run(host.Services.GetRequiredService<MainForm>());
return 0;

static void ShowStartupError(string title, string message) =>
    MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
