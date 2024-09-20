namespace TestTask.ViewModels
{
    public class DepartmentViewModel
    {
        public int DepartmentID { get; set; }
        public string DepartmentName { get; set; }
        public List<PositionViewModel> Positions { get; set; } = new List<PositionViewModel>();
        public int? ParentDepartmentID { get; set; }
        public List<DepartmentViewModel> SubDepartments { get; set; } = new List<DepartmentViewModel>();
    }
}
