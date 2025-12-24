namespace LibraryManagementBE.Model
{
    public class Books
    {
        public int Id { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string author { get; set; }
        public string? imageUrl { get; set; }
        public decimal price { get; set; }
        public string genre { get; set; }
        public int totalCopies { get; set; }
        public int availableCopies { get; set; }
    }
}
