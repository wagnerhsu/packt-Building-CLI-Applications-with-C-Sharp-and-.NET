using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.Text.Json;
using BookMarkr;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Spectre.Console;

FreeSerilogLoggerOnShutdown();
/***** THE ROOT COMMAND *******************/
var rootCommand = new RootCommand("Bookmarkr is a bookmark manager provided as a CLI application.")
{
};

rootCommand.SetHandler(OnHandleRootCommand);


/***** THE LINK COMMAND *******************/
var linkCommand = new Command("link", "Manage bookmarks links")
{
};

rootCommand.AddCommand(linkCommand);

/***** THE ADD COMMAND *******************/
var nameOption = new Option<string[]>(
    ["--name", "-n"], // equivalent to new string[] { "--name", "-n" }
    "The name of the bookmark"
);
nameOption.IsRequired = true;
nameOption.Arity = ArgumentArity.OneOrMore;
nameOption.AllowMultipleArgumentsPerToken = true;


var urlOption = new Option<string[]>(
    ["--url", "-u"],
    "The URL of the bookmark"
);
urlOption.IsRequired = true;
urlOption.Arity = ArgumentArity.OneOrMore;
urlOption.AllowMultipleArgumentsPerToken = true;
var categoryOption = new Option<string[]>(
    ["--category", "-c"],
    "The category to which the bookmark is associated"
);

categoryOption.Arity = ArgumentArity.OneOrMore;
categoryOption.AllowMultipleArgumentsPerToken = true;

categoryOption.SetDefaultValue("Read later");
categoryOption.FromAmong("Read later", "Tech books", "Cooking", "Social media");
categoryOption.AddCompletions("Read later", "Tech books", "Cooking", "Social media");


var addLinkCommand = new Command("add", "Add a new bookmark link")
{
    nameOption,
    urlOption,
    categoryOption
};

linkCommand.AddCommand(addLinkCommand);

addLinkCommand.SetHandler(OnHandleAddLinkCommand, nameOption, urlOption, categoryOption);


/****** the export command *****/
var outputfileOption = new Option<FileInfo>(
    ["--file", "-f"],
    "The output file that will store the bookmarks"
)
{
    IsRequired = true
};

outputfileOption.LegalFileNamesOnly();

var exportCommand = new Command("export", "Exports all bookmarks to a file")
{
    outputfileOption
};

rootCommand.AddCommand(exportCommand);

exportCommand.SetHandler(async (context) =>
{
    FileInfo? outputfileOptionValue = context.ParseResult.GetValueForOption(outputfileOption);
    var token = context.GetCancellationToken();
    await OnExportCommand(outputfileOptionValue!, token);
    //await OnInteractiveExportCommand(outputfileOptionValue!, token);
});


/****** the import command *****/
var inputfileOption = new Option<FileInfo>(
    ["--file", "-f"],
    "The input file that contains the bookmarks to be imported"
)
{
    IsRequired = true
};

inputfileOption.LegalFileNamesOnly();
inputfileOption.ExistingOnly();

var importCommand = new Command("import", "Imports all bookmarks from a file")
{
    inputfileOption
};

rootCommand.AddCommand(importCommand);

importCommand.SetHandler(OnImportCommand, inputfileOption);


/***** THE LINK COMMAND *******************/
var interactiveCommand = new Command("interactive", "Manage bookmarks interactively")
{
};

rootCommand.AddCommand(interactiveCommand);

interactiveCommand.SetHandler(OnInteractiveCommand);
/***** THE BUILDER PATTERN *******************/
var parser = new CommandLineBuilder(rootCommand)
    .UseHost(_ => Host.CreateDefaultBuilder(),
        host =>
        {
            host.ConfigureServices(services =>
            {
                // ** configuration moved to appsettings.json
                services.AddSerilog((config) =>
                {
                    var configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json")
                        .Build();
                    config.ReadFrom.Configuration(configuration);
                });
            });
        })
    .UseDefaults()
    .Build();

return await parser.InvokeAsync(args);

static void FreeSerilogLoggerOnShutdown()
{
    // This event is raised when the process is about to exit, allowing you to perform cleanup tasks or save data.
    AppDomain.CurrentDomain.ProcessExit += (s, e) => ExecuteShutdownTasks();
    // This event is triggered when the user presses Ctrl+C or Ctrl+Break. While it doesn't cover all shutdown scenarios, it's useful for handling user-initiated terminations.
    Console.CancelKeyPress += (s, e) => ExecuteShutdownTasks();
}


// Code to execute before shutdown
static void ExecuteShutdownTasks()
{
    Console.WriteLine("Performing shutdown tasks...");
    // Perform cleanup tasks, save data, etc.
    Log.CloseAndFlush();
}

/***** HANDLER METHODS *******************/
static void OnHandleRootCommand()
{
    Console.WriteLine("Hello from the root command!");
}

static void OnHandleAddLinkCommand(string[] names, string[] urls, string[] categories)
{
    //Helper.BookmarkService.AddLink(name, url, category);
    Helper.BookmarkService.AddLinks(names, urls, categories);
    Helper.BookmarkService.ListAll();
}

static async Task OnExportCommand(FileInfo outputfile, CancellationToken token)
{
    try
    {
        Console.WriteLine("Starting export operation...");
        var bookmarks = Helper.BookmarkService.GetAll();
        string json = JsonSerializer.Serialize(bookmarks, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(outputfile.FullName, json, token);
    }
    catch (OperationCanceledException ex)
    {
        var requested = ex.CancellationToken.IsCancellationRequested
            ? "Cancellation was requested by you."
            : "Cancellation was NOT requested by you.";
        Helper.ShowWarningMessage(["Operation was cancelled.", requested, $"Cancellation reason: {ex.Message}"]);
    }
    catch (JsonException ex)
    {
        Helper.ShowErrorMessage([
            $"Failed to serialize bookmarks to JSON.",
            $"Error message {ex.Message}"
        ]);
    }
    catch (UnauthorizedAccessException ex)
    {
        Helper.ShowErrorMessage([
            $"Insufficient permissions to access the file {outputfile.FullName}",
            $"Error message {ex.Message}"
        ]);
    }
    catch (DirectoryNotFoundException ex)
    {
        Helper.ShowErrorMessage([
            $"The file {outputfile.FullName} cannot be found due to an invalid path",
            $"Error message {ex.Message}"
        ]);
    }
    catch (PathTooLongException ex)
    {
        Helper.ShowErrorMessage([
            $"The provided path is exceeding the maximum length.",
            $"Error message {ex.Message}"
        ]);
    }
    catch (Exception ex)
    {
        Helper.ShowErrorMessage([
            $"An unknown exception occurred.",
            $"Error message {ex.Message}"
        ]);
    }
}

static void OnImportCommand(FileInfo inputfile)
{
    string json = File.ReadAllText(inputfile.FullName);
    List<Bookmark> bookmarks = JsonSerializer.Deserialize<List<Bookmark>>(json) ?? new List<Bookmark>();

    foreach (var bookmark in bookmarks)
    {
        var conflict = Helper.BookmarkService.Import(bookmark);
        if (conflict is not null)
        {
            Log.Information(
                $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} | Bookmark updated | name changed from '{conflict.OldName}' to '{conflict.NewName}' for URL '{conflict.Url}'");
        }
    }
}

static void OnInteractiveCommand()
{
    bool isRunning = true;
    while (isRunning)
    {
        AnsiConsole.Write(new FigletText("Bookmarkr").Centered().Color(Color.SteelBlue));

        var selectedOperation = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[blue]What do you wanna do?[/]")
                .AddChoices([
                    "Export bookmarks to file",
                    "View Bookmarks",
                    "Exit Program"
                ])
        );

        switch (selectedOperation)
        {
            case "Export bookmarks to file":
                ExportBookmarks();
                break;
            case "View Bookmarks":
                ViewBookmarks();
                break;
            default:
                isRunning = false;
                break;
        }
    }
}


static void ExportBookmarks()
{
    // ask for the outputfilePath
    var outputfilePath = AnsiConsole.Prompt(
        new TextPrompt<string>("Please provide the output file name (default: 'bookmarks.json')")
            .DefaultValue("bookmarks.json"));

    // export the bookmarks to the specified file, while showing progress.
    AnsiConsole.Progress()
        .AutoRefresh(true) // Turns on auto refresh
        .AutoClear(false) // Avoids removing the task list when completed
        .HideCompleted(false) // Avoids hiding tasks as they are completed
        .Columns(
        [
            new TaskDescriptionColumn(), // Shows the task description
            new ProgressBarColumn(), // Shows the progress bar
            new PercentageColumn(), // Shows the current percentage
            new RemainingTimeColumn(), // Shows the remaining time
            new SpinnerColumn(), // Shows the spinner, indicating that the operation is ongoing
        ])
        .Start(ctx =>
        {
            // Get the list of all bookmarks
            var bookmarks = Helper.BookmarkService.GetAll();

            // export the bookmarks to the file
            // 1. Create the task
            var task = ctx.AddTask("[yellow]exporting all bookmarks to file...[/]");

            // 2. Set the total steps for the progress bar
            task.MaxValue = bookmarks.Count;

            // 3. Open the file for writing
            using (StreamWriter writer = new StreamWriter(outputfilePath))
            {
                while (!ctx.IsFinished)
                {
                    foreach (var bookmark in bookmarks)
                    {
                        // 3.1. Serialize the current bookmark as JSON and write it to the file asynchronously
                        writer.WriteLine(JsonSerializer.Serialize(bookmark));

                        // 3.2. Increment the progress bar
                        task.Increment(1);

                        // 3.3. Slow down the process so we can see the progress (since this operation is not that much time-consuming)
                        Thread.Sleep(1500);
                    }
                }
            }
        });
    AnsiConsole.MarkupLine("[green]All bookmarks have been successfully exported![/]");
}

static void ViewBookmarks()
{
    // Create the tree
    var root = new Tree("Bookmarks");

    // Add some nodes
    var techBooksCategory = root.AddNode("[yellow]Tech Books[/]");
    var carsCategory = root.AddNode("[yellow]Cars[/]");
    var socialMediaCategory = root.AddNode("[yellow]Social Media[/]");
    var cookingCategory = root.AddNode("[yellow]Cooking[/]");

    // add bookmarks for the Tech Book category
    var techBooks = Helper.BookmarkService.GetBookmarksByCategory("Tech Books");
    foreach (var techbook in techBooks)
    {
        techBooksCategory.AddNode($"{techbook.Name} | {techbook.Url}");
    }

    // add bookmarks for the Cars category
    var cars = Helper.BookmarkService.GetBookmarksByCategory("Cars");
    foreach (var car in cars)
    {
        carsCategory.AddNode($"{car.Name} | {car.Url}");
    }

    // add bookmarks for the Social Media category
    var socialMedias = Helper.BookmarkService.GetBookmarksByCategory("Social Media");
    foreach (var socialMedia in socialMedias)
    {
        socialMediaCategory.AddNode($"{socialMedia.Name} | {socialMedia.Url}");
    }

    // add bookmarks for the Cooking category
    var cookings = Helper.BookmarkService.GetBookmarksByCategory("Cooking");
    foreach (var cooking in cookings)
    {
        cookingCategory.AddNode($"{cooking.Name} | {cooking.Url}");
    }

    // Render the tree
    AnsiConsole.Write(root);
}