using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestTask.Models;
using TestTask.Data;
using TestTask.ViewModels;
using Microsoft.AspNetCore.Authorization;

[Authorize]
public class DepartmentsController : Controller
{
    private readonly AppDbContext _context;

    public DepartmentsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string searchTerm)
    {
        var departments = await _context.Departments
            .Include(d => d.Positions)
            .ThenInclude(p => p.Employees)
            .Include(d => d.SubDepartments)
            .AsNoTracking()
            .ToListAsync();

        var filteredDepartments = FilterDepartments(departments, searchTerm);
        var departmentViewModels = filteredDepartments.Select(d => CreateDepartmentViewModel(d, departments)).ToList();

        return View(departmentViewModels);
    }

    private List<Department> FilterDepartments(List<Department> departments, string searchTerm)
    {
        if (!string.IsNullOrEmpty(searchTerm))
        {
            return departments.Where(d =>
                d.DepartmentName.Contains(searchTerm) ||
                d.Positions.Any(p => p.PositionName.Contains(searchTerm))
            ).ToList();
        }

        return departments.Where(d => d.ParentDepartmentID == null).ToList();
    }

    private DepartmentViewModel CreateDepartmentViewModel(Department department, List<Department> allDepartments)
    {
        return new DepartmentViewModel
        {
            DepartmentID = department.DepartmentID,
            DepartmentName = department.DepartmentName,
            Positions = department.Positions?.Select(CreatePositionViewModel).ToList(),
            SubDepartments = GetSubDepartments(department.DepartmentID, allDepartments)
        };
    }

    private PositionViewModel CreatePositionViewModel(Position position)
    {
        return new PositionViewModel
        {
            PositionName = position.PositionName,
            MinSalary = position.MinSalary,
            MaxSalary = position.MaxSalary,
            EmployeeCount = position.Employees?.Count ?? 0,
            MaxEmployees = position.MaxEmployees,
            Employees = position.Employees?.Select(e => new EmployeeViewModel
            {
                FullName = e.FullName,
                EmployeeID = e.EmployeeID
            }).ToList()
        };
    }

    private List<DepartmentViewModel> GetSubDepartments(int parentDepartmentId, List<Department> allDepartments)
    {
        var subDepartments = allDepartments
            .Where(d => d.ParentDepartmentID == parentDepartmentId)
            .ToList();

        return subDepartments.Select(d => CreateDepartmentViewModel(d, allDepartments)).ToList();
    }

    public async Task<IActionResult> Details(int id)
    {
        var department = await _context.Departments
            .Include(d => d.SubDepartments)
            .Include(d => d.Positions)
            .FirstOrDefaultAsync(d => d.DepartmentID == id);

        if (department == null) return NotFound();

        var departmentViewModel = CreateDepartmentViewModel(department, new List<Department>());
        return View(departmentViewModel);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Departments = await _context.Departments.ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DepartmentViewModel model)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (user.Role != "Admin")
        {
            return Forbid();
        }

        if (ModelState.IsValid)
        {
            var department = new Department
            {
                DepartmentName = model.DepartmentName,
                ParentDepartmentID = model.ParentDepartmentID
            };

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
            AddPositionsToDepartment(model.Positions, department.DepartmentID);
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Departments = await _context.Departments.ToListAsync();
        return View(model);
    }

    private async void AddPositionsToDepartment(List<PositionViewModel> positions, int departmentId)
    {
        if (positions != null && positions.Any())
        {
            var newPositions = positions.Select(p => new Position
            {
                PositionName = p.PositionName,
                DepartmentID = departmentId,
                MinSalary = p.MinSalary,
                MaxSalary = p.MaxSalary,
                MaxEmployees = p.MaxEmployees
            }).ToList();

            _context.Positions.AddRange(newPositions);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var department = await _context.Departments
            .Include(d => d.Positions)
            .FirstOrDefaultAsync(d => d.DepartmentID == id);

        if (department == null) return NotFound();

        ViewBag.Departments = GetEditableDepartments(department.DepartmentID);
        var departmentViewModel = CreateDepartmentViewModel(department, new List<Department>());
        return View(departmentViewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DepartmentViewModel model)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (user.Role != "Admin")
        {
            return Forbid();
        }

        if (!ModelState.IsValid) return View(model);

        var department = await _context.Departments
            .Include(d => d.Positions)
            .FirstOrDefaultAsync(d => d.DepartmentID == id);

        if (department == null) return NotFound();

        department.DepartmentName = model.DepartmentName;
        department.ParentDepartmentID = model.ParentDepartmentID;

        UpdatePositions(department, model.Positions);
        _context.Update(department);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private void UpdatePositions(Department department, List<PositionViewModel> positionViewModels)
    {
        var existingPositions = department.Positions.ToList();

        foreach (var position in existingPositions)
        {
            var viewModel = positionViewModels.FirstOrDefault(p => p.PositionName == position.PositionName);
            if (viewModel != null)
            {
                position.MinSalary = viewModel.MinSalary;
                position.MaxSalary = viewModel.MaxSalary;
                position.MaxEmployees = viewModel.MaxEmployees;
            }
            else
            {
                _context.Employees
                    .Where(e => e.PositionID == position.PositionID)
                    .ToList()
                    .ForEach(e => e.PositionID = null);

                _context.Positions.Remove(position);
            }
        }

        foreach (var viewModel in positionViewModels.Where(p => !existingPositions.Any(ep => ep.PositionName == p.PositionName)))
        {
            department.Positions.Add(new Position
            {
                PositionName = viewModel.PositionName,
                MinSalary = viewModel.MinSalary,
                MaxSalary = viewModel.MaxSalary,
                MaxEmployees = viewModel.MaxEmployees,
                DepartmentID = department.DepartmentID
            });
        }
    }

    public async Task<IActionResult> Delete(int id)
    {
        var department = await _context.Departments
            .Include(d => d.SubDepartments)
            .Include(d => d.Positions)
            .FirstOrDefaultAsync(d => d.DepartmentID == id);

        if (department == null) return NotFound();

        return View(department);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (user.Role != "Admin")
        {
            return Forbid();
        }

        var department = await _context.Departments
            .Include(d => d.SubDepartments)
            .Include(d => d.Positions)
            .FirstOrDefaultAsync(d => d.DepartmentID == id);

        if (department != null)
        {
            foreach (var subDepartment in department.SubDepartments)
            {
                subDepartment.ParentDepartmentID = null;
            }

            _context.Employees
                .Where(e => department.Positions.Select(p => p.PositionID).Contains(e.PositionID.GetValueOrDefault()))
                .ToList()
                .ForEach(e => e.PositionID = null);

            _context.Positions.RemoveRange(department.Positions);
            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private IEnumerable<Department> GetEditableDepartments(int currentDepartmentId)
    {
        return _context.Departments
            .Where(d => d.DepartmentID != currentDepartmentId)
            .ToList();
    }

    private bool DepartmentExists(int id)
    {
        return _context.Departments.Any(e => e.DepartmentID == id);
    }

    protected async Task<User> GetCurrentUserAsync()
    {
        var username = User.Identity.Name;
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }
}
