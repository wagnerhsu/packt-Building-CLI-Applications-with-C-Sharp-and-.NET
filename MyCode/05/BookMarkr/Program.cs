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
urlOption.AddValidator(result=>
{
    foreach (var token in result.Tokens)
    {
        if (string.IsNullOrWhiteSpace(token.Value))
        {
            result.ErrorMessage = "URL cannot be empty";
            break;
        }

        if (!Uri.TryCreate(token.Value, UriKind.Absolute, out _))
        {
            result.ErrorMessage = $"Invalid URL: {token.Value}";
            break;
        }
    }
});
var categoryOption = new Option<string>(
    ["--category", "-c"],
    "The category to which the bookmark is associated"
);

nameOption.IsRequired = true;
urlOption.IsRequired = true;
categoryOption.IsRequired = false;
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

rootCommand.SetHandler(OnHandleRootCommand);
var parser = new CommandLineBuilder(rootCommand)
    .UseDefaults()
    .Build();

return await parser.InvokeAsync(args);

static void OnHandleRootCommand()
{
    Console.WriteLine("Hello from the root command!");
}
static void OnHandleAddLinkCommand(string name, string url, string category)
{
    Helper.BookmarkServiceInstance.AddLink(name, url,category);
}