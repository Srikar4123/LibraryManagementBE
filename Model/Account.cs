using System.ComponentModel.DataAnnotations;

namespace LibraryManagementBE.Model
{
    public enum AccountRole
    {
        User = 0,
        Admin = 1
    }

    public class Account
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string userName { get; set; }

        [Required, EmailAddress, MaxLength(200)]
        public string email { get; set; }

        [Required, MaxLength(100)]
        public string password { get; set; }

        [Required, Phone, MaxLength(20)]
        public string phoneNumber { get; set; }

        [Required]
        public AccountRole role { get; set; }  

        public bool isActive { get; set; } = true;

        public DateTime createdAt { get; set; } = DateTime.UtcNow;
    }
}
