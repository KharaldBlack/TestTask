namespace TestTask.ViewModels
{
    public class PositionViewModel
    {
        public string PositionName { get; set; }
        public decimal MinSalary { get; set; }
        public decimal MaxSalary { get; set; }
        public int EmployeeCount { get; set; }
        public int MaxEmployees { get; set; }
        public List<EmployeeViewModel> Employees { get; set; } = new List<EmployeeViewModel>();
    }
}
