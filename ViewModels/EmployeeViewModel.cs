namespace TestTask.ViewModels {
    public class EmployeeViewModel
    {
        public int EmployeeID { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public int? PositionID { get; set; }
        public IFormFile? Photo { get; set; }
    }
}