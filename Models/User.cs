using System.ComponentModel.DataAnnotations;

namespace TestTask.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; }

        [MaxLength(50)]
        public string Role { get; set; }
    }
}
