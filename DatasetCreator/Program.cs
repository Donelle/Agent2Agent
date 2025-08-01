using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DatasetCreator.Services;

namespace DatasetCreator;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Create host builder
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IEmbeddingService, OpenAIEmbeddingService>();
                services.AddSingleton<IRedisService, RedisService>();
                services.AddSingleton<IFileProcessorService, FileProcessorService>();
                services.AddSingleton<DatasetImporter>();
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .Build();

        // Create command line interface
        var rootCommand = new RootCommand("DatasetCreator - Import CSV and PDF files into Redis vector database");

        var inputOption = new Option<string>(
            "--input",
            description: "Input directory or file path")
        {
            IsRequired = false
        };
        inputOption.SetDefaultValue("./Data");

        var formatsOption = new Option<string[]>(
            "--formats",
            description: "File formats to process (csv, pdf)")
        {
            IsRequired = false,
            AllowMultipleArgumentsPerToken = true
        };
        formatsOption.SetDefaultValue(new[] { "csv", "pdf" });

        var clearExistingOption = new Option<bool>(
            "--clear-existing",
            description: "Clear existing data before import")
        {
            IsRequired = false
        };

        var verboseOption = new Option<bool>(
            "--verbose",
            description: "Enable verbose logging")
        {
            IsRequired = false
        };

        rootCommand.AddOption(inputOption);
        rootCommand.AddOption(formatsOption);
        rootCommand.AddOption(clearExistingOption);
        rootCommand.AddOption(verboseOption);

        rootCommand.SetHandler(async (input, formats, clearExisting, verbose) =>
        {
            // Note: Verbose logging can be configured in appsettings.json
            var importer = host.Services.GetRequiredService<DatasetImporter>();
            await importer.ImportAsync(input, formats, clearExisting);
        }, inputOption, formatsOption, clearExistingOption, verboseOption);

        return await rootCommand.InvokeAsync(args);
    }
}
