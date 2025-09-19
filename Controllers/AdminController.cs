using Microsoft.AspNetCore.Mvc;
using ReferralManagement.Data;
using ReferralManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;
using System.Net;

namespace ReferralManagement.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly ReferralDbContext _context;
        public AdminController(ReferralDbContext context)
        {
            _context = context;
        }

        // DELETE: api/admin/delete-user/{id}
        [HttpDelete("delete-user/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { error = $"User with id {id} not found." });
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            var users = await _context.Users.ToListAsync();
            return Ok(new { success = true, users });
        }

        // GET: api/admin/users
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            return Ok(users);
        }

        // POST: api/admin/create-user
        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] ReferralManagement.Models.CreateUserRequest req)
        {
            Console.WriteLine("[CreateUser] Endpoint hit (CreateUserRequest)");
            try
            {
                if (req == null)
                    return BadRequest(new { error = "Request body is null." });
                if (string.IsNullOrWhiteSpace(req.Name))
                    return BadRequest(new { error = "Name is required." });
                if (string.IsNullOrWhiteSpace(req.Email))
                    return BadRequest(new { error = "Email is required." });
                if (string.IsNullOrWhiteSpace(req.Role))
                    return BadRequest(new { error = "Role is required." });
                if (req.Role != "HR" && req.Role != "Employee")
                    return BadRequest(new { error = "Role must be HR or Employee." });
                if (string.IsNullOrWhiteSpace(req.Project))
                    return BadRequest(new { error = "Project is required." });
                if (string.IsNullOrWhiteSpace(req.Workplace))
                    return BadRequest(new { error = "Workplace is required." });
                if (string.IsNullOrWhiteSpace(req.Designation))
                    return BadRequest(new { error = "Designation is required." });
                if (await _context.Users.AnyAsync(u => u.Email == req.Email))
                    return BadRequest(new { error = "Email already exists." });
                string password = GeneratePassword();
                var user = new User
                {
                    Name = req.Name,
                    Email = req.Email,
                    Role = req.Role,
                    Project = req.Project,
                    Workplace = req.Workplace,
                    Designation = req.Designation,
                    Password = password,
                    FirstLogin = true
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                await SendEmailAsync(user.Email, "Your Account Credentials",
                    $"Hello {user.Name},\nRole: {user.Role}\nUsername: {user.Email}\nPassword: {password}");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine("[CreateUser] Exception: " + ex);
                return StatusCode(500, new { error = "Internal server error", exception = ex.ToString() });
            }
        }

        private string GeneratePassword()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[8];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
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

            Console.WriteLine($"SMTP Config: Host={smtpHost}, Port={smtpPortRaw}, User={smtpUser}, Pass={(string.IsNullOrEmpty(smtpPass) ? "<empty>" : "<hidden>")}");

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpPortRaw) || string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
            {
                Console.WriteLine("SMTP config missing or invalid.");
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
