using Spectre.Console;

namespace BookMarkr;

static class Helper
{
    public static BookmarkService BookmarkService { get; } = new BookmarkService();
    public static void ShowErrorMessage(string[] errorMessages)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        AnsiConsole.MarkupLine(Emoji.Known.CrossMark + " [bold red]ERROR[/] :cross_mark:");
        foreach (var message in errorMessages)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]{message}[/]");
        }
    }


    public static void ShowWarningMessage(string[] errorMessages)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        var m = new Markup(Emoji.Known.Warning + " [bold yellow]Warning[/] :warning:");
        m.Centered();
        AnsiConsole.Write(m);
        AnsiConsole.WriteLine();
        foreach (var message in errorMessages)
        {
            AnsiConsole.MarkupLineInterpolated($"[yellow]{message}[/]");
        }
    }


    public static void ShowSuccessMessage(string[] errorMessages)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        AnsiConsole.MarkupLine(Emoji.Known.BeatingHeart + " [bold green]SUCCESS[/] :beating_heart:");
        foreach (var message in errorMessages)
        {
            AnsiConsole.MarkupLineInterpolated($"[green]{message}[/]");
        }
    }
}