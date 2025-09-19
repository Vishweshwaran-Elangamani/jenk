using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReferralManagement.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Role { get; set; } // Admin, HR, Employee
        public string Project { get; set; }
        public string Workplace { get; set; }
        public string Designation { get; set; }
        [Required]
        public string Password { get; set; }
        public bool FirstLogin { get; set; } = true;
    }
}
