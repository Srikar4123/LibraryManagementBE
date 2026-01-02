using LibraryManagementBE.Data;
using LibraryManagementBE.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;

namespace MiniProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FinesController : ControllerBase
    {
        private readonly FinesDbContext _fines;
        private readonly BooksDbContext _books;
        private readonly AppDbContext _accounts;
        private const decimal DailyFineRate = 5m;

        public FinesController(FinesDbContext fines, BooksDbContext books, AppDbContext accounts)
        {
            _fines = fines;
            _books = books;
            _accounts = accounts;
        }

        // DTOs
        public class AdminIssueDto
        {
            [Required] public int adminId { get; set; }
            [Required] public int userId { get; set; }
            [Required] public int bookId { get; set; }
            [Required] public DateTime issueDate { get; set; }
            [Required] public DateTime dueDate { get; set; }
        }

        public class BorrowDto
        {
            [Required] public int userId { get; set; }
            [Required] public int bookId { get; set; }
            [Required] public DateTime issueDate { get; set; }
            [Required] public DateTime dueDate { get; set; }
        }

        public class ReturnDto
        {
            public int userId { get; set; }
            [Required] public int loanId { get; set; }
        }

        public class PayFineDto
        {
            [Required] public int loanId { get; set; }
            [Range(0, 100000)] public decimal amount { get; set; }
        }

        // Helpers
        private static decimal ComputeFine(DateTime dueDate, DateTime asOf)
        {
            if (asOf <= dueDate) return 0m;
            var daysLate = (asOf.Date - dueDate.Date).Days;
            return daysLate * DailyFineRate;
        }

        private async Task<bool> IsAdmin(int accountId) =>
            await _accounts.Accounts.AnyAsync(a => a.Id == accountId && a.role == AccountRole.Admin);

        private async Task<bool> IsUser(int accountId) =>
            await _accounts.Accounts.AnyAsync(a => a.Id == accountId && a.role == AccountRole.User);

        // ADMIN: Issue
        [Authorize(Roles = "Admin")]
        [HttpPost("admin/issue")]
        public async Task<IActionResult> AdminIssue([FromBody] AdminIssueDto dto)


        {
            //var adminId = int.Parse(User.FindFirst("id")!.Value);

            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            if (!await IsAdmin(dto.adminId))
                return NotFound(new { message = $"Admin {dto.adminId} not found." });

            if (!await IsUser(dto.userId))
                return NotFound(new { message = $"User {dto.userId} not found." });

            var book = await _books.Books.FindAsync(dto.bookId);
            if (book == null) return NotFound(new { message = $"Book {dto.bookId} not found." });
            if (book.availableCopies <= 0) return BadRequest(new { message = "No available copies to issue." });

            bool alreadyHasThisBook = await _fines.Fines.AnyAsync(
                f => f.UserId == dto.userId && f.BookId == dto.bookId && f.ReturnDate == null
            );

            if (alreadyHasThisBook)
                return BadRequest(new { message = "This user already has an active loan for this book." });

            var activeCount = await _fines.Fines.CountAsync(f => f.UserId == dto.userId && f.ReturnDate == null);
            if (activeCount >= 2) return BadRequest(new { message = "Borrow limit reached (max 2 active loans)." });

            var loan = new Fines
            {
                BookId = dto.bookId,
                UserId = dto.userId,
                IssueDate = dto.issueDate,
                DueDate = dto.dueDate,
                ReturnDate = null,
                fineAmount = 0m,
                paymentStatus = false
            };

            _fines.Fines.Add(loan);
            book.availableCopies -= 1;

            await _fines.SaveChangesAsync();
            await _books.SaveChangesAsync();

            return Ok(new { message = "Issued by admin", loanId = loan.Id, availableCopies = book.availableCopies });
        }

        // USER: Borrow
        [HttpPost("borrow")]
        public async Task<IActionResult> Borrow([FromBody] BorrowDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            if (!await IsUser(dto.userId))
                return NotFound(new { message = $"User {dto.userId} not found." });

            var book = await _books.Books.FindAsync(dto.bookId);
            if (book == null) return NotFound(new { message = $"Book {dto.bookId} not found." });
            if (book.availableCopies <= 0) return BadRequest(new { message = "No available copies to borrow." });

            bool alreadyHasThisBook = await _fines.Fines.AnyAsync(
                f => f.UserId == dto.userId && f.BookId == dto.bookId && f.ReturnDate == null
            );
            if (alreadyHasThisBook)
                return BadRequest(new { message = "You already have an active loan for this book." });

            var activeCount = await _fines.Fines.CountAsync(f => f.UserId == dto.userId && f.ReturnDate == null);
            if (activeCount >= 2) return BadRequest(new { message = "Borrow limit reached (max 2 active loans)." });

            var loan = new Fines
            {
                BookId = dto.bookId,
                UserId = dto.userId,
                IssueDate = dto.issueDate,
                DueDate = dto.dueDate,
                ReturnDate = null,
                fineAmount = 0m,
                paymentStatus = false
            };

            _fines.Fines.Add(loan);
            book.availableCopies -= 1;

            await _fines.SaveChangesAsync();
            await _books.SaveChangesAsync();

            return Ok(new { message = "Book borrowed", loanId = loan.Id, availableCopies = book.availableCopies });
        }

        // USER: Return
        [HttpPost("return")]
        public async Task<IActionResult> Return([FromBody] ReturnDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var loan = await _fines.Fines.FirstOrDefaultAsync(f => f.Id == dto.loanId);
            if (loan == null)
                return NotFound(new { message = $"Loan {dto.loanId} not found." });

            if (loan.UserId != dto.userId)
                return Forbid("You cannot return a book you did not borrow.");

            if (loan.ReturnDate != null)
                return BadRequest(new { message = "Loan already returned." });

            var book = await _books.Books.FindAsync(loan.BookId);
            if (book == null)
                return NotFound(new { message = $"Book {loan.BookId} not found." });

            loan.ReturnDate = DateTime.UtcNow;
            loan.fineAmount = ComputeFine(loan.DueDate, loan.ReturnDate.Value);
            book.availableCopies = Math.Min(
                book.availableCopies + 1,
                book.totalCopies
            );

            await _fines.SaveChangesAsync();
            await _books.SaveChangesAsync();

            return Ok(new
            {
                message = "Book returned",
                fineAmount = loan.fineAmount,
                availableCopies = book.availableCopies
            });
        }


        // USER: Pay Fine
        [HttpPost("pay")]
        public async Task<IActionResult> Pay([FromBody] PayFineDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var loan = await _fines.Fines.FirstOrDefaultAsync(f => f.Id == dto.loanId);
            if (loan == null) return NotFound(new { message = $"Loan {dto.loanId} not found." });

            var asOf = loan.ReturnDate ?? DateTime.UtcNow;
            loan.fineAmount = ComputeFine(loan.DueDate, asOf);

            if (dto.amount < loan.fineAmount)
                return BadRequest(new { message = $"Insufficient amount. Required: {loan.fineAmount}" });

            loan.paymentStatus = true;
            await _fines.SaveChangesAsync();

            return Ok(new { message = "Fine paid", loanId = loan.Id, paidAmount = dto.amount, paymentStatus = loan.paymentStatus });
        }

        // VIEW fines/loans with filters
        [HttpGet("loans")]
        public async Task<IActionResult> Loans([FromQuery] int? userId, [FromQuery] bool onlyActive = false, [FromQuery] bool onlyUnpaid = false, [FromQuery] bool onlyOverdue = false)
        {
            IQueryable<Fines> q = _fines.Fines.AsNoTracking();

            if (userId.HasValue) q = q.Where(f => f.UserId == userId.Value);
            if (onlyActive) q = q.Where(f => f.ReturnDate == null);
            if (onlyUnpaid) q = q.Where(f => !f.paymentStatus);
            if (onlyOverdue) q = q.Where(f => f.ReturnDate == null && DateTime.UtcNow > f.DueDate);

            var loans = await q.OrderByDescending(f => f.IssueDate).ToListAsync();

            // Recompute displayed fine for active overdue loans
            foreach (var l in loans.Where(x => x.ReturnDate == null))
                l.fineAmount = ComputeFine(l.DueDate, DateTime.UtcNow);

            // Enrich with names/titles
            var bookIds = loans.Select(l => l.BookId).Distinct().ToList();
            var userIds = loans.Select(l => l.UserId).Distinct().ToList();

            var booksMap = await _books.Books
                .Where(b => bookIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.title);

            var usersMap = await _accounts.Accounts
                .Where(a => userIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, a => a.userName);

            return Ok(loans.Select(l => new
            {
                l.Id,
                l.UserId,
                userName = usersMap.GetValueOrDefault(l.UserId),
                l.BookId,
                title = booksMap.GetValueOrDefault(l.BookId),
                l.IssueDate,
                l.DueDate,
                l.ReturnDate,
                l.fineAmount,
                l.paymentStatus
            }));
        }

        // Active count per user
        [HttpGet("user/{userId:int}/activeCount")]
        public async Task<IActionResult> ActiveCount(int userId)
        {
            var count = await _fines.Fines.CountAsync(f => f.UserId == userId && f.ReturnDate == null);
            return Ok(new { userId, activeLoans = count, canBorrow = count < 2 });
        }

        // Outstanding total per user
        [HttpGet("user/{userId:int}/outstanding")]
        public async Task<IActionResult> Outstanding(int userId)
        {
            var loans = await _fines.Fines
                .Where(f => f.UserId == userId && !f.paymentStatus)
                .ToListAsync();

            decimal total = 0m;
            foreach (var l in loans)
            {
                var asOf = l.ReturnDate ?? DateTime.UtcNow;
                total += ComputeFine(l.DueDate, asOf);
            }

            return Ok(new { userId, totalOutstanding = total });
        }
    }
}