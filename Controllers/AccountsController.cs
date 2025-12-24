using LibraryManagementBE.Model;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagementBE.Data;

namespace LibraryManagementBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AccountsController(AppDbContext context)
        {
            _context = context;
        }

        // ===== DTOs (kept inside controller) =====
        public class AccountCreateDto
        {
            [Required, MaxLength(150)] public string userName { get; set; }
            [Required, EmailAddress, MaxLength(200)] public string email { get; set; }
            [Required, MaxLength(100)] public string password { get; set; }
            [Required, Phone, MaxLength(20)] public string phoneNumber { get; set; }
            [Required] public AccountRole role { get; set; }
        }

        public class AccountUpdateDto
        {
            [Required, MaxLength(150)] public string userName { get; set; }
            [Required, EmailAddress, MaxLength(200)] public string email { get; set; }
            [Required, MaxLength(100)] public string password { get; set; }
            [Required, Phone, MaxLength(20)] public string phoneNumber { get; set; }
            [Required] public AccountRole role { get; set; }
            public bool isActive { get; set; } = true;
        }

        // ===== List & filter =====
        // GET /api/accounts?role=Admin&search=kapya
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? role, [FromQuery] string? search)
        {
            var q = _context.Accounts.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<AccountRole>(role, true, out var r))
                q = q.Where(a => a.role == r);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(a => a.userName.Contains(s) || a.email.Contains(s));
            }

            var list = await q.OrderBy(a => a.userName).ToListAsync();
            return Ok(list);
        }

        // GET /api/accounts/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var acc = await _context.Accounts.FindAsync(id);
            return acc == null ? NotFound(new { message = $"Account {id} not found." }) : Ok(acc);
        }

        // POST /api/accounts
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AccountCreateDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            // Pre-check duplicates for more friendly error
            var exists = await _context.Accounts.AnyAsync(a => a.email == dto.email || a.phoneNumber == dto.phoneNumber);
            if (exists) return Conflict(new { message = "Email or phone number already exists." });

            var acc = new Account
            {
                userName = dto.userName,
                email = dto.email,
                password = dto.password,     // NOTE: store hashed password in real apps
                phoneNumber = dto.phoneNumber,
                role = dto.role,
                isActive = true,
                createdAt = DateTime.UtcNow
            };

            _context.Accounts.Add(acc);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = acc.Id }, acc);
        }

        // PUT /api/accounts/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] AccountUpdateDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var acc = await _context.Accounts.FindAsync(id);
            if (acc == null) return NotFound(new { message = $"Account {id} not found." });

            // Duplicate check excluding current account
            var dup = await _context.Accounts.AnyAsync(a =>
                a.Id != id && (a.email == dto.email || a.phoneNumber == dto.phoneNumber));
            if (dup) return Conflict(new { message = "Email or phone number already exists for another account." });

            acc.userName = dto.userName;
            acc.email = dto.email;
            acc.password = dto.password;  // NOTE: hash in real apps
            acc.phoneNumber = dto.phoneNumber;
            acc.role = dto.role;
            acc.isActive = dto.isActive;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE /api/accounts/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var acc = await _context.Accounts.FindAsync(id);
            if (acc == null) return NotFound(new { message = $"Account {id} not found." });

            _context.Accounts.Remove(acc);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Soft deactivate instead of hard delete (optional)
        // POST /api/accounts/{id}/deactivate
        [HttpPost("{id:int}/deactivate")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var acc = await _context.Accounts.FindAsync(id);
            if (acc == null) return NotFound(new { message = $"Account {id} not found." });

            acc.isActive = false;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Account deactivated." });
        }
    }
}
