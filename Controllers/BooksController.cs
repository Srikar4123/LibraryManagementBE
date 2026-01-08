using LibraryManagementBE.Data;
using LibraryManagementBE.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
 
namespace MiniProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly BooksDbContext _books;
        private readonly FinesDbContext _fines;

        public BooksController(BooksDbContext books, FinesDbContext fines)
        {
            _books = books;
            _fines = fines;
        }

        // Data Transfer Objects (DTOs)
        public class BookDto
        {
            [Required] public string title { get; set; }
            public string description { get; set; }
            [Required] public string author { get; set; }
            public string? imageUrl { get; set; }
            [Range(0, 100000)] public decimal price { get; set; }
            [Required] public string genre { get; set; }
            [Range(0, int.MaxValue)] public int totalCopies { get; set; }
            [Range(0, int.MaxValue)] public int availableCopies { get; set; }
            public string publishedYear {  get; set; }
        }

        public class AvailabilityAdjustDto
        {
            [Required] public int bookId { get; set; }
            [Required] public int delta { get; set; }
        }

        // API Endpoints

        //GetAll
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? genre)
        {
            IQueryable<Books> q = _books.Books.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(genre))
                q = q.Where(b => b.genre == genre);

            var list = await q.OrderBy(b => b.title).ToListAsync();
            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var book = await _books.Books.FindAsync(id);
            return book == null ? NotFound(new { message = $"Book {id} not found." }) : Ok(book);
        }

        // Create
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BookDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var book = new Books
            {
                title = dto.title,
                description = dto.description,
                author = dto.author,
                imageUrl = dto.imageUrl,
                price = dto.price,
                genre = dto.genre,
                totalCopies = dto.totalCopies,
                availableCopies = dto.availableCopies,
                publishedYear = dto.publishedYear,
            };

            if (book.availableCopies > book.totalCopies)
                return BadRequest(new { message = "availableCopies cannot exceed totalCopies." });

            _books.Books.Add(book);
            await _books.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = book.Id }, book);
        }

        // Update
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] BookDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var book = await _books.Books.FindAsync(id);
            if (book == null) return NotFound(new { message = $"Book {id} not found." });

            book.title = dto.title;
            book.description = dto.description;
            book.author = dto.author;
            book.imageUrl = dto.imageUrl;
            book.price = dto.price;
            book.genre = dto.genre;
            book.totalCopies = dto.totalCopies;
            book.publishedYear = dto.publishedYear;
            book.availableCopies = Math.Max(0, Math.Min(dto.availableCopies, dto.totalCopies));

            await _books.SaveChangesAsync();
            return NoContent();
        }

        // DELETE (blocked if active loans)
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var book = await _books.Books.FindAsync(id);
            if (book == null) return NotFound(new { message = $"Book {id} not found." });

            // Prevent deletion when there are active, unreturned loans in Fines
            bool hasActiveLoans = await _fines.Fines.AnyAsync(f => f.BookId == id && f.ReturnDate == null);
            if (hasActiveLoans)
                return Conflict(new { message = "Cannot delete book with active loans." });

            _books.Books.Remove(book);
            await _books.SaveChangesAsync();
            return NoContent();
        }

        // Adjusting the Availability
        [HttpPost("availability/adjust")]
        public async Task<IActionResult> AdjustAvailability([FromBody] AvailabilityAdjustDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var book = await _books.Books.FindAsync(dto.bookId);
            if (book == null) return NotFound(new { message = $"Book {dto.bookId} not found." });

            if (dto.delta < 0 && book.availableCopies <= 0)
                return BadRequest(new { message = "No available copies to borrow/issue." });

            book.availableCopies = Math.Max(0, Math.Min(book.availableCopies + dto.delta, book.totalCopies));
            await _books.SaveChangesAsync();

            return Ok(new { message = "Availability updated.", bookId = book.Id, availableCopies = book.availableCopies });
        }

        [HttpPost("availability/decrement/{bookId:int}")]
        public async Task<IActionResult> DecrementAvailability(int bookId)
        {
            var book = await _books.Books.FindAsync(bookId);
            if (book == null) return NotFound(new { message = $"Book {bookId} not found." });
            if (book.availableCopies <= 0) return BadRequest(new { message = "No available copies to borrow/issue." });

            book.availableCopies -= 1;
            await _books.SaveChangesAsync();
            return Ok(new { message = "Availability decremented.", availableCopies = book.availableCopies });
        }

        [HttpPost("availability/increment/{bookId:int}")]
        public async Task<IActionResult> IncrementAvailability(int bookId)
        {
            var book = await _books.Books.FindAsync(bookId);
            if (book == null) return NotFound(new { message = $"Book {bookId} not found." });

            book.availableCopies = Math.Min(book.availableCopies + 1, book.totalCopies);
            await _books.SaveChangesAsync();
            return Ok(new { message = "Availability incremented.", availableCopies = book.availableCopies });
        }
    }
}
