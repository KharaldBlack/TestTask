using System.ComponentModel.DataAnnotations;

namespace TestTask.Models
{
    public class Department
    {
        [Key]
        public int DepartmentID { get; set; }

        [Required]
        [MaxLength(100)]
        public string DepartmentName { get; set; }

        public int? ParentDepartmentID { get; set; }

        public Department ParentDepartment { get; set; }

        public ICollection<Department> SubDepartments { get; set; } = new List<Department>();

        public ICollection<Position> Positions { get; set; } = new List<Position>();
    }
}
