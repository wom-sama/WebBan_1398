using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WebBan_1398.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string FullName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Age { get; set; }
    }
}
