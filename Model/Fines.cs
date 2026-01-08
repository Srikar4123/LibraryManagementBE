using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementBE.Model
{
    public class Fines
    {
        // Properties of the Fines Model

        public int Id { get; set; }

        [ForeignKey(nameof(Book))]
        public int BookId { get; set; }

        [ForeignKey(nameof(UserAccount))]
        public int UserId { get; set; }


        public decimal fineAmount { get; set; }

        public bool paymentStatus { get; set; } 

        [Required]
        public DateTime IssueDate { get; set; }  

        [Required]
        public DateTime DueDate { get; set; }    

        public DateTime? ReturnDate { get; set; } 

        public Books Book { get; set; }
        public Account UserAccount { get; set; }
    }
}
