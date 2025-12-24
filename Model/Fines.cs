using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementBE.Model
{
    public class Fines
    {

        public int Id { get; set; }

        [ForeignKey(nameof(Book))]
        public int BookId { get; set; }

        [ForeignKey(nameof(UserAccount))]
        public int UserId { get; set; }


        public decimal fineAmount { get; set; }

        public bool paymentStatus { get; set; }  // true when fine is paid

        [Required]
        public DateTime IssueDate { get; set; }  // when issued

        [Required]
        public DateTime DueDate { get; set; }    // when it should be returned

        public DateTime? ReturnDate { get; set; } // actual return date (null = still borrowed)

        // Navigation
        public Books Book { get; set; }
        public Account UserAccount { get; set; }
    }
}
