using System.ComponentModel.DataAnnotations;

namespace MAFStudio.Backend.Data
{
    public class User
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Avatar { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "user";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public virtual ICollection<Agent> Agents { get; set; } = new List<Agent>();
    }
}
