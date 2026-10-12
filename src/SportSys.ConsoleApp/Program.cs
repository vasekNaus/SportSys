using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SportSys.Contract;
using SportSys.Contract.Services;

partial class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length != 3 || !args[0].Equals("import-kis", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("Použití: SportSys.ConsoleApp import-kis <cesta-k-xlsx> <id-sezony>");
            return;
        }

        if (!int.TryParse(args[2], out var seasonId) || seasonId <= 0)
        {
            Console.Error.WriteLine("ID sezony musí být kladné celé číslo.");
            return;
        }

        using var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
                if (context.HostingEnvironment.IsDevelopment())
                    config.AddUserSecrets<Program>(optional: true);
            })
            .ConfigureServices((context, services) =>
                services.AddSportSysServices(context.Configuration))
            .Build();

        using var scope = host.Services.CreateScope();
        var importService = scope.ServiceProvider.GetRequiredService<KisMatchImportService>();
        var result = await importService.ImportAsync(args[1], seasonId);

        Console.WriteLine(
            $"Import dokončen: vloženo {result.Inserted}, existující {result.Existing}, přeskočeno {result.Skipped}.");

        foreach (var warning in result.Warnings)
            Console.WriteLine($"  [řádek {warning.RowNumber}] {warning.Message}");
    }
}
