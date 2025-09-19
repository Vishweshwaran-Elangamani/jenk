using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReferralManagement.Models
{
    public class Referral
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string? CandidateName { get; set; }
        public string? CurrentCompany { get; set; }
        [Required]
        [EmailAddress]
        public string? CandidateEmail { get; set; }
        public string? ResumePdf { get; set; } // filename reference
        public byte[]? ResumeBlob { get; set; } // PDF file as BLOB
        [ForeignKey("Job")]
        public int JobId { get; set; }
    public Job? Job { get; set; }
        [ForeignKey("User")]
        public int EmployeeId { get; set; }
    public User? Employee { get; set; }
        [Required]
    public string? Status { get; set; } // Pending, Verified, Interview Scheduled, Confirmed
    public DateTime? InterviewDateTime { get; set; }
    public DateTime? SubmittedAt { get; set; } // UTC submission date
    }
}
