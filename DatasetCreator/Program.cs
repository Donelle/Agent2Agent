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

        var fileOption = new Option<string>(
            "--file",
            description: "Process a single PDF file")
        {
            IsRequired = false
        };

        var stateOption = new Option<string>(
            "--state",
            description: "State abbreviation for the PDF file (e.g., CA, TX, FL)")
        {
            IsRequired = false
        };

        var chunkSizeOption = new Option<int>(
            "--chunk-size",
            description: "Chunk size for text splitting")
        {
            IsRequired = false
        };
        chunkSizeOption.SetDefaultValue(1000);

        var chunkOverlapOption = new Option<int>(
            "--chunk-overlap",
            description: "Chunk overlap for text splitting")
        {
            IsRequired = false
        };
        chunkOverlapOption.SetDefaultValue(200);

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
        rootCommand.AddOption(fileOption);
        rootCommand.AddOption(stateOption);
        rootCommand.AddOption(chunkSizeOption);
        rootCommand.AddOption(chunkOverlapOption);
        rootCommand.AddOption(formatsOption);
        rootCommand.AddOption(clearExistingOption);
        rootCommand.AddOption(verboseOption);

        rootCommand.SetHandler(async (input, file, state, chunkSize, chunkOverlap, formats, clearExisting, verbose) =>
        {
            // Note: Verbose logging can be configured in appsettings.json
            var importer = host.Services.GetRequiredService<DatasetImporter>();

            // Check if we're processing a single file with specific options
            if (!string.IsNullOrEmpty(file))
            {
                if (!File.Exists(file))
                {
                    Console.WriteLine($"Error: File not found: {file}");
                    return;
                }

                if (!Path.GetExtension(file).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Error: Only PDF files are supported for single file processing: {file}");
                    return;
                }

                if (string.IsNullOrEmpty(state))
                {
                    Console.WriteLine("Error: --state parameter is required when processing a single PDF file");
                    return;
                }

                await importer.ImportSingleFileAsync(file, state, chunkSize, chunkOverlap, clearExisting);
            }
            else
            {
                await importer.ImportAsync(input, formats, clearExisting);
            }
        }, inputOption, fileOption, stateOption, chunkSizeOption, chunkOverlapOption, formatsOption, clearExistingOption, verboseOption);

        return await rootCommand.InvokeAsync(args);
    }
}
