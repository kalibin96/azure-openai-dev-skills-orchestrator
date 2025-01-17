﻿using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.Memory.Qdrant;
using Microsoft.SemanticKernel.Memory;
using System.Reflection;
using Microsoft.SemanticKernel.Plugins.Memory;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;

class Program
{
    static string WafFileName = "azure-well-architected.pdf";
    static async Task Main(string[] args)
    {
        var kernelSettings = KernelSettings.LoadSettings();

        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(kernelSettings.LogLevel ?? LogLevel.Warning)
                .AddConsole()
                .AddDebug();
        });
       
        var memoryBuilder = new MemoryBuilder();
        var memory = memoryBuilder.WithLoggerFactory(loggerFactory)
                    .WithQdrantMemoryStore(kernelSettings.QdrantEndpoint, 1536)
                    .WithAzureTextEmbeddingGenerationService(kernelSettings.EmbeddingDeploymentOrModelId, kernelSettings.Endpoint, kernelSettings.ApiKey)
                    .Build();
        await ImportDocumentAsync(memory, WafFileName);
    }

    public static async Task ImportDocumentAsync(ISemanticTextMemory memory, string filename)
        {
            var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var filePath = Path.Combine(currentDirectory, filename);
            using var pdfDocument = PdfDocument.Open(File.OpenRead(filePath));
            var pages = pdfDocument.GetPages();
            foreach (var page in pages)
            {
                try
                {
                    var text = ContentOrderTextExtractor.GetText(page);
                    var descr = text.Take(100);
                    await memory.SaveInformationAsync(
                        collection: "waf-pages",
                        text: text,
                        id: $"{Guid.NewGuid()}",
                        description: $"Document: {descr}");
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
}