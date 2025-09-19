using Microsoft.AspNetCore.Mvc;
using ReferralManagement.Data;
using ReferralManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using ReferralManagement.Services;

namespace ReferralManagement.Controllers
{
    [ApiController]
    [Route("api/referral-management")]
    public class ReferralManagementController : ControllerBase
    {
        private readonly ReferralDbContext _context;
        private readonly EmailService _emailService;
        public ReferralManagementController(ReferralDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: api/referral-management/referrals
        [HttpGet("referrals")]
        public async Task<IActionResult> GetReferrals()
        {
            var referrals = await _context.Referrals.Include(r => r.Job).Include(r => r.Employee).ToListAsync();
            return Ok(referrals);
        }

        // POST: api/referral-management/update-status
        [HttpPost("update-status")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusRequest req)
        {
            Console.WriteLine($"[UpdateStatus] Received: ReferralId={req.ReferralId}, NewStatus={req.NewStatus}, InterviewDateTime={req.InterviewDateTime}");
            var referral = await _context.Referrals.Include(r => r.Job).Include(r => r.Employee).FirstOrDefaultAsync(r => r.Id == req.ReferralId);
            if (referral == null) return NotFound("Referral not found");
            // Sequential status logic
            var allowed = new Dictionary<string, string> {
                { "Pending", "Verified" },
                { "Verified", "Interview Scheduled" },
                { "Interview Scheduled", "Confirmed" }
            };
            var currentStatus = referral.Status ?? string.Empty;
            if (!allowed.TryGetValue(currentStatus, out var expected) || expected != (req.NewStatus ?? string.Empty))
                return BadRequest("Invalid status transition");
            referral.Status = req.NewStatus;
            referral.InterviewDateTime = req.InterviewDateTime;
            await _context.SaveChangesAsync();
            int emailsSent = 0;
            try
            {
                if (req.NewStatus == "Verified")
                {
                    var subject = "Your referral has been verified";
                    var body = $"Dear {referral.CandidateName},\n\nWe have verified your referral for the position '{referral.Job?.Title ?? "the advertised role"}'. We will contact you with further updates.\n\nBest regards,\nRecruitment Team";
                    if (!string.IsNullOrEmpty(referral.CandidateEmail))
                    {
                        await _emailService.SendEmailAsync(referral.CandidateEmail, subject, body);
                        emailsSent++;
                        Console.WriteLine($"[UpdateStatus] Sent interview email to candidate {referral.CandidateEmail}");
                    }
                }

                if (req.NewStatus == "Interview Scheduled" && req.InterviewDateTime.HasValue)
                {
                    var when = req.InterviewDateTime.Value.ToLocalTime().ToString("f");
                    var subject = "Interview Scheduled";
                    var body = $"Dear {referral.CandidateName},\n\nYour interview has been scheduled for {when}. Please be prepared and reach out if you require any accommodations.\n\nBest regards,\nRecruitment Team";
                    if (!string.IsNullOrEmpty(referral.CandidateEmail))
                    {
                        await _emailService.SendEmailAsync(referral.CandidateEmail, subject, body);
                        emailsSent++;
                    }

                    // Notify the employee who referred the candidate
                    var employeeEmail = referral.Employee?.Email;
                    var employeeName = referral.Employee?.Name ?? "";
                    if (!string.IsNullOrEmpty(employeeEmail))
                    {
                        var empSubject = $"Interview Scheduled for {referral.CandidateName}";
                        var empBody = $"Dear {employeeName},\n\nAn interview for your referred candidate {referral.CandidateName} has been scheduled for {when}. We wanted to keep you informed.\n\nBest regards,\nRecruitment Team";
                        await _emailService.SendEmailAsync(employeeEmail, empSubject, empBody);
                        emailsSent++;
                        Console.WriteLine($"[UpdateStatus] Sent interview email to employee {employeeEmail}");
                    }
                }

                if (req.NewStatus == "Confirmed")
                {
                    var subject = "Congratulations — Your Referral Is Confirmed";
                    var body = $"Dear {referral.CandidateName},\n\nCongratulations! Your referral for '{referral.Job?.Title ?? "the role"}' has been confirmed. Our team will contact you with onboarding details.\n\nWarm regards,\nRecruitment Team";
                    if (!string.IsNullOrEmpty(referral.CandidateEmail))
                    {
                        await _emailService.SendEmailAsync(referral.CandidateEmail, subject, body);
                        emailsSent++;
                    }

                    var earning = new Earning {
                        ReferralId = referral.Id,
                        Amount = referral.Job?.ReferralBonus ?? 0,
                        Date = DateTime.Now
                    };
                    _context.Earnings.Add(earning);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdateStatus] Email sending error: {ex}");
            }

            return Ok(new { success = true, emailsSent = emailsSent, scheduledAt = referral.InterviewDateTime?.ToString("o") });
        }

        // GET: api/referral-management/search-referrals
        [HttpGet("search-referrals")]
        public async Task<IActionResult> SearchReferrals(string employeeName)
        {
            var searchName = employeeName ?? string.Empty;
            var referrals = await _context.Referrals
                .Include(r => r.Employee)
                .Where(r => r.Employee != null && r.Employee.Name != null && r.Employee.Name.Contains(searchName))
                .ToListAsync();
            return Ok(referrals);
        }

        // GET: api/referral-management/send-test-email
        [HttpGet("send-test-email")]
        public async Task<IActionResult> SendTestEmail(string to, string? subject = "Test Email", string? body = "This is a test email from ReferralManagement API")
        {
            try
            {
                await _emailService.SendEmailAsync(to, subject ?? "Test Email", body ?? "Test body");
                return Ok(new { success = true, to, subject });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SendTestEmail] Error: {ex}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        private async Task SendEmailAsync(string to, string subject, string body)
        {
            var smtp = new SmtpClient("smtp.example.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("your@email.com", "your_email_password"),
                EnableSsl = true
            };
            var mail = new MailMessage("your@email.com", to, subject, body);
            await smtp.SendMailAsync(mail);
        }
    }

    public class UpdateStatusRequest
    {
        public int ReferralId { get; set; }
        public string? NewStatus { get; set; }
        public DateTime? InterviewDateTime { get; set; }
    }
}
