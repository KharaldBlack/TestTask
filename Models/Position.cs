using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TestTask.Models
{
    public class Position
    {
        [Key]
        public int PositionID { get; set; }

        [Required]
        [MaxLength(100)]
        public string PositionName { get; set; }

        [Required]
        public decimal MinSalary { get; set; }

        [Required]
        public decimal MaxSalary { get; set; }

        [ForeignKey("Department")]
        public int DepartmentID { get; set; }
        public Department Department { get; set; }

        public int MaxEmployees { get; set; }

        public ICollection<Employee> Employees { get; set; }
    }
}
