using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using Tranga;

var app = new CommandApp<TrangaCli>();
return app.Run(args);

internal sealed class TrangaCli : Command<TrangaCli.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Directory to which downloaded Manga are saved")]
        [CommandOption("-d|--downloadLocation")]
        [DefaultValue(null)]
        public string? downloadLocation { get; init; }
        
        [Description("Directory in which application-data is saved")]
        [CommandOption("-w|--workingDirectory")]
        [DefaultValue(null)]
        public string? workingDirectory { get; init; }
        
        [Description("Enables the file-logger")]
        [CommandOption("-f")]
        [DefaultValue(null)]
        public bool? fileLogger { get; init; }
        
        [Description("Path to save logfile to")]
        [CommandOption("-l|--fPath")]
        [DefaultValue(null)]
        public string? fileLoggerPath { get; init; }
        
        [Description("Port on which to run API on")]
        [CommandOption("-p|--port")]
        [DefaultValue(null)]
        public int? apiPort { get; init; }
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        List<Logger.LoggerType> enabledLoggers = new();
        if(settings.fileLogger.HasValue && settings.fileLogger.Value == true)
            enabledLoggers.Add(Logger.LoggerType.FileLogger);
        string? logFilePath = settings.fileLoggerPath ?? "";//TODO path
        Logger logger = new(enabledLoggers.ToArray(), Console.Out, Console.OutputEncoding, logFilePath);

        TrangaSettings trangaSettings = new (settings.downloadLocation, settings.workingDirectory, settings.apiPort);

        Directory.CreateDirectory(trangaSettings.downloadLocation);
        Directory.CreateDirectory(trangaSettings.workingDirectory);

        Tranga.Tranga? api = null;

        Thread trangaApi = new Thread(() =>
        {
            api = new(logger, trangaSettings);
        });
        trangaApi.Start();
        
        HttpClient client = new();
        
        bool exit = false;
        while (!exit)
        {
            string menuSelect = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Menu")
                    .PageSize(10)
                    .MoreChoicesText("Up/Down")
                    .AddChoices(new[]
                    {
                        "CustomRequest",
                        "Log",
                        "Exit"
                    }));

            switch (menuSelect)
            {
                case "CustomRequest":
                    HttpMethod requestMethod = AnsiConsole.Prompt(
                        new SelectionPrompt<HttpMethod>()
                            .Title("Request Type")
                            .AddChoices(new[]
                            {
                                HttpMethod.Get,
                                HttpMethod.Delete, 
                                HttpMethod.Post
                            }));
                    string requestPath = AnsiConsole.Prompt(
                        new TextPrompt<string>("Request Path:"));
                    List<ValueTuple<string, string>> parameters = new();
                    while (AnsiConsole.Confirm("Add Parameter?"))
                    {
                        string name = AnsiConsole.Ask<string>("Parameter Name:");
                        string value = AnsiConsole.Ask<string>("Parameter Value:");
                        parameters.Add(new ValueTuple<string, string>(name, value));
                    }

                    string requestString = $"http://localhost:{trangaSettings.apiPortNumber}/{requestPath}";
                    if (parameters.Any())
                    {
                        requestString += "?";
                        foreach (ValueTuple<string, string> parameter in parameters)
                            requestString += $"{parameter.Item1}={parameter.Item2}&";
                    }

                    HttpRequestMessage request = new (requestMethod, requestString);
                    AnsiConsole.WriteLine($"Request: {request.Method} {request.RequestUri}");
                    HttpResponseMessage response;
                    if (AnsiConsole.Confirm("Send Request?"))
                        response = client.Send(request);
                    else break;
                    AnsiConsole.WriteLine(response.Content.ReadAsStringAsync().Result);
                    break;
                case "Log":
                    List<string> lines = logger.Tail(10).ToList();
                    Rows rows = new Rows(lines.Select(line => new Text(line)));
                    
                    AnsiConsole.Live(rows).Start(context =>
                    {
                        bool running = true;
                        while (running)
                        {
                            string[] newLines = logger.GetNewLines();
                            if (newLines.Length > 0)
                            {
                                lines.AddRange(newLines);
                                rows = new Rows(lines.Select(line => new Text(line)));
                                context.UpdateTarget(rows);
                            }
                            Thread.Sleep(100);
                            if (AnsiConsole.Console.Input.IsKeyAvailable())
                            {
                                AnsiConsole.Console.Input.ReadKey(true); //Do not process input
                                running = false;
                            }
                        }
                    });
                    break;
                case "Exit":
                    exit = true;
                    break;
            }
        }

        if (api is not null)
            api.keepRunning = false;
        
        return 0;
    }
}