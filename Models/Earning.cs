using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReferralManagement.Models
{
    public class Earning
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("Referral")]
        public int ReferralId { get; set; }
    public Referral? Referral { get; set; }
    [Required]
    public decimal Amount { get; set; }
    [Required]
    public DateTime Date { get; set; }
    [ForeignKey("User")]
    public int EmployeeId { get; set; } // Link earning to employee
    }
}
