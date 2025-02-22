using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using BookMarkr;


var rootCommand = new RootCommand("Bookmarkr is a bookmark manager provided as a CLI application.")
{
};
var linkCommand = new Command("link", "Manage bookmarks links")
{
};
rootCommand.AddCommand(linkCommand);
var nameOption = new Option<string>(
    ["--name", "-n"], // equivalent to new string[] { "--name", "-n" }
    "The name of the bookmark"
);

var urlOption = new Option<string>(
    ["--url", "-u"],
    "The URL of the bookmark"
);

var addLinkCommand = new Command("add", "Add a new bookmark link")
{
    nameOption,
    urlOption
};

linkCommand.AddCommand(addLinkCommand);

addLinkCommand.SetHandler(OnHandleAddLinkCommand, nameOption, urlOption);

rootCommand.SetHandler(OnHandleRootCommand);
var parser = new CommandLineBuilder(rootCommand)
    .UseDefaults()
    .Build();

return await parser.InvokeAsync(args);

static void OnHandleRootCommand()
{
    Console.WriteLine("Hello from the root command!");
}
static void OnHandleAddLinkCommand(string name, string url)
{
    Helper.BookmarkServiceInstance.AddLink(name, url);
}