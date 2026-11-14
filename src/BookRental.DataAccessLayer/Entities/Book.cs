public class Book
{
    public Book(string title, string author, string isbn, int copiesAvailable)
    {
        Title = title;
        Author = author;
        Isbn = isbn;
        CopiesAvailable = copiesAvailable;
    }
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public string Isbn { get; set; }  
    public int CopiesAvailable { get; set; }
              
}