
    using ReferralManagement.Models;
    using Microsoft.AspNetCore.Mvc;
    using ReferralManagement.Data;
    using Microsoft.EntityFrameworkCore;
    using System.Net.Mail;
    using System.Net;

namespace ReferralManagement.Controllers
{
    [ApiController]
    [Route("api/hr")]
    public class HRController : ControllerBase
    {
        private readonly ReferralDbContext _context;
        public HRController(ReferralDbContext context)
        {
            _context = context;
        }

        // GET: api/hr/earnings
        [HttpGet("earnings")]
        public async Task<IActionResult> GetEarnings()
        {
            var earnings = await _context.Earnings.ToListAsync();
            return Ok(earnings);
        }

        // GET: api/hr/jobs
        [HttpGet("jobs")]
        public async Task<IActionResult> GetJobs()
        {
            var jobs = await _context.Jobs.ToListAsync();
            return Ok(jobs);
        }

        // GET: api/hr/referral-limits
        [HttpGet("referral-limits")]
        public async Task<IActionResult> GetReferralLimits()
        {
            var limits = await _context.ReferralLimits.ToListAsync();
            return Ok(limits);
        }

        // POST: api/hr/add-job
        [HttpPost("add-job")]
        public async Task<IActionResult> AddJob([FromBody] AddJobRequest request)
        {
            try
            {
                // Log incoming request payload
                Console.WriteLine("[AddJob] Incoming payload: " + System.Text.Json.JsonSerializer.Serialize(request));

                if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
                {
                    Console.WriteLine("[AddJob] Validation failed: Title or Description is missing.");
                    return BadRequest(new { error = "Title and Description are required." });
                }

                var job = new Job
                {
                    Title = request.Title ?? string.Empty,
                    Description = request.Description ?? string.Empty,
                    ReferralBonus = request.ReferralBonus,
                    CreatedBy = request.CreatedBy
                };

                // Log mapped Job entity
                Console.WriteLine("[AddJob] Mapped Job entity: " + System.Text.Json.JsonSerializer.Serialize(job));

                if (!ModelState.IsValid)
                {
                    Console.WriteLine("[AddJob] ModelState errors:");
                    foreach (var key in ModelState.Keys)
                    {
                        var errors = ModelState[key]?.Errors;
                        if (errors != null && errors.Count > 0)
                        {
                            foreach (var err in errors)
                            {
                                Console.WriteLine($"[AddJob] {key}: {err.ErrorMessage}");
                            }
                        }
                    }
                    return BadRequest(ModelState);
                }

                _context.Jobs.Add(job);
                await _context.SaveChangesAsync();
                Console.WriteLine("[AddJob] Job saved successfully.");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine("[AddJob] Exception: " + ex.ToString());
                if (ex.InnerException != null)
                {
                    Console.WriteLine("[AddJob] InnerException: " + ex.InnerException.ToString());
                }
                return StatusCode(500, new { error = "Internal server error", details = ex.ToString() });
            }
        }

        // DELETE: api/hr/delete-job/{id}
        [HttpDelete("delete-job/{id}")]
        public async Task<IActionResult> DeleteJob(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
                return NotFound(new { error = "Job not found" });
            _context.Jobs.Remove(job);
            await _context.SaveChangesAsync();
            var jobs = await _context.Jobs.ToListAsync();
            return Ok(new { success = true, jobs });
        }

        // GET: api/hr/referrals
        [HttpGet("referrals")]
        public async Task<IActionResult> GetReceivedReferrals()
        {
            var referrals = await _context.Referrals.Include(r => r.Job).Include(r => r.Employee).ToListAsync();
            return Ok(referrals);
        }

        // POST: api/hr/update-referral-status
        [HttpPost("update-referral-status")]
        public async Task<IActionResult> UpdateReferralStatus([FromBody] UpdateReferralStatusRequest request)
        {
            var referral = await _context.Referrals.Include(r => r.Job).Include(r => r.Employee).FirstOrDefaultAsync(r => r.Id == request.ReferralId);
            Console.WriteLine($"[UpdateReferralStatus] Received: ReferralId={request.ReferralId}, NewStatus={request.NewStatus}, InterviewDateTime={request.InterviewDateTime}");
            if (referral == null)
                return NotFound(new { error = "Referral not found" });
            // Sequential status logic
            var allowed = new Dictionary<string, string> {
                { "Pending", "Verified" },
                { "Verified", "Interview Scheduled" },
                { "Interview Scheduled", "Confirmed" }
            };
            if (request.NewStatus == "Rejected")
            {
                referral.Status = "Rejected";
                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            // Prevent cancelling if already confirmed
            if (request.NewStatus == "Cancelled" && referral.Status == "Confirmed")
            {
                return BadRequest(new { error = "Cannot cancel a confirmed referral." });
            }
            var currentStatus = referral.Status ?? string.Empty;
            if (!allowed.TryGetValue(currentStatus, out var expected) || expected != (request.NewStatus ?? string.Empty))
                return BadRequest(new { error = "Invalid status transition" });
            referral.Status = request.NewStatus;
            referral.InterviewDateTime = request.InterviewDateTime;
            await _context.SaveChangesAsync();
            // Send professional emails for status transitions
            int emailsSent = 0;
            try
            {
                if (request.NewStatus == "Verified")
                {
                    var subject = "Your referral has been verified";
                    var body = $"Dear {referral.CandidateName},\n\nWe are pleased to inform you that your referral for the position '{referral.Job?.Title ?? "the advertised role"}' has been verified by our team.\n\nThank you for your interest. We will contact you with next steps if applicable.\n\nBest regards,\nHR Team";
                    if (!string.IsNullOrEmpty(referral.CandidateEmail))
                    {
                        await SendEmailAsync(referral.CandidateEmail, subject, body);
                        emailsSent++;
                    }
                }

                if (request.NewStatus == "Interview Scheduled" && referral.InterviewDateTime.HasValue)
                {
                    var subject = "Interview Scheduled";
                    var when = referral.InterviewDateTime.Value.ToLocalTime().ToString("f");
                    var body = $"Dear {referral.CandidateName},\n\nYour interview has been scheduled for {when}. Please arrive 10 minutes early and bring any requested documents.\n\nIf you need to reschedule, please contact us.\n\nBest regards,\nHR Team";
                    if (!string.IsNullOrEmpty(referral.CandidateEmail))
                    {
                        await SendEmailAsync(referral.CandidateEmail, subject, body);
                        emailsSent++;
                        Console.WriteLine($"[UpdateReferralStatus] Sent interview email to candidate {referral.CandidateEmail}");
                    }

                    // Also notify the employee who referred the candidate
                    var employeeEmail = referral.Employee?.Email;
                    var employeeName = referral.Employee?.Name ?? "";
                    if (!string.IsNullOrEmpty(employeeEmail))
                    {
                        var empSubject = $"Interview Scheduled for {referral.CandidateName}";
                        var empBody = $"Dear {employeeName},\n\nAn interview for your referred candidate {referral.CandidateName} has been scheduled for {when}. We wanted to keep you informed for your convenience.\n\nBest regards,\nHR Team";
                        await SendEmailAsync(employeeEmail, empSubject, empBody);
                        emailsSent++;
                        Console.WriteLine($"[UpdateReferralStatus] Sent interview email to employee {employeeEmail}");
                    }
                }

                if (request.NewStatus == "Confirmed")
                {
                    var subject = "Congratulations — Your Referral Is Confirmed";
                    var body = $"Dear {referral.CandidateName},\n\nCongratulations! We are delighted to confirm your referral for the position '{referral.Job?.Title ?? "the role"}'.\n\nStart Date: {DateTime.Now.ToString("D")}\nDetails: Our HR representative will reach out with onboarding next steps and documentation.\n\nWelcome aboard and best wishes,\nHR Team";
                    if (!string.IsNullOrEmpty(referral.CandidateEmail))
                    {
                        await SendEmailAsync(referral.CandidateEmail, subject, body);
                        emailsSent++;
                    }

                    // Add earning
                    decimal bonus = 0;
                    if (referral.Job != null && referral.Job.ReferralBonus > 0)
                    {
                        bonus = referral.Job.ReferralBonus;
                    }
                    var earning = new Earning {
                        ReferralId = referral.Id,
                        Amount = bonus,
                        Date = DateTime.Now,
                        EmployeeId = referral.EmployeeId
                    };
                    _context.Earnings.Add(earning);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdateReferralStatus] Email sending error: {ex}");
            }

            return Ok(new { success = true, emailsSent = emailsSent, scheduledAt = referral.InterviewDateTime?.ToString("o") });
        }

        // POST: api/hr/set-referral-limit
        [HttpPost("set-referral-limit")]
    public async Task<IActionResult> SetReferralLimit([FromBody] ReferralLimitDto dto)
        {
            try
            {
                // Defensive logging for incoming payload
                Console.WriteLine($"[SetReferralLimit] Incoming payload: EmployeeId={dto.EmployeeId}, EmployeeId1={dto.EmployeeId1}, LimitCount={dto.LimitCount}");
                var employeeId1 = dto.EmployeeId;
                var limitCount = dto.LimitCount;
                var existing = await _context.ReferralLimits.FirstOrDefaultAsync(x => x.EmployeeId1 == employeeId1);
                if (existing != null && existing.UsedCount > limitCount)
                {
                    Console.WriteLine($"[SetReferralLimit] Cannot reduce: UsedCount={existing.UsedCount}, LimitCount={limitCount}");
                    return BadRequest("Referral limit already reached by this employee. Cannot reduce further.");
                }
                if (existing == null)
                {
                    var newLimit = new ReferralLimit {
                        EmployeeId1 = employeeId1,
                        LimitCount = limitCount,
                        UsedCount = 0
                    };
                    _context.ReferralLimits.Add(newLimit);
                    Console.WriteLine($"[SetReferralLimit] Creating new limit for EmployeeId1={employeeId1}, LimitCount={limitCount}");
                }
                else
                {
                    existing.LimitCount = limitCount;
                    Console.WriteLine($"[SetReferralLimit] Updating limit for EmployeeId1={employeeId1}, NewLimitCount={limitCount}");
                }
                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SetReferralLimit] Exception: {ex.Message}\n{ex.StackTrace}");
                return BadRequest(new { error = "Internal error: " + ex.Message });
            }
        }

        // POST: api/hr/change-password
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
            if (user == null)
                return NotFound("User not found");
            if (string.IsNullOrEmpty(request.OldPassword) || string.IsNullOrEmpty(request.NewPassword) || string.IsNullOrEmpty(request.ConfirmNewPassword))
                return BadRequest("All password fields are required");
            if (user.Password != request.OldPassword)
                return BadRequest("Current password is incorrect");
            if (request.NewPassword.Length < 6)
                return BadRequest("New password must be at least 6 characters");
            if (request.NewPassword != request.ConfirmNewPassword)
                return BadRequest("New password and confirm password do not match");
            user.Password = request.NewPassword;
            user.FirstLogin = false;
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        private async Task SendEmailAsync(string to, string subject, string body)
        {
            // Read SMTP settings from configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            var smtpHost = config["Smtp:Host"];
            var smtpPortRaw = config["Smtp:Port"];
            var smtpUser = config["Smtp:User"];
            var smtpPass = config["Smtp:Pass"];

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpPortRaw) || string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
            {
                throw new Exception("SMTP config missing or invalid.");
            }

            var smtpPort = int.Parse(smtpPortRaw);
            var smtp = new SmtpClient(smtpHost)
            {
                Port = smtpPort,
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };
            var mail = new MailMessage(smtpUser, to, subject, body);
            await smtp.SendMailAsync(mail);
        }
    }
}
