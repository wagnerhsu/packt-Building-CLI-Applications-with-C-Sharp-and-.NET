namespace BookMarkr;

public class BookmarkService
{
    private readonly List<Bookmark> _bookmarks = new();


    public void AddLink(string name, string url, string category)
    {
        
        if(_bookmarks.Any(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            Helper.ShowWarningMessage([$"A link with the name '{name}' already exists. It will thus not be added", 
                $"To update the existing link, use the command: bookmarkr link update '{name}' '{url}'"]);
            return;
        }

        _bookmarks.Add(new Bookmark { Name = name, Url = url, Category = category});
        Helper.ShowSuccessMessage(["Bookmark successfully added!"]);
        Console.WriteLine(_bookmarks.Count);
        ListAll();
    }
    public void ListAll()
    {
        foreach(var bookmark in _bookmarks)
        {
            Console.WriteLine($"Name: '{bookmark.Name}' | URL: '{bookmark.Url}' | Category: '{bookmark.Category}'");            
        }
    }
}
