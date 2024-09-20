using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using TestTask.Models;
using TestTask.Data;
using TestTask.ViewModels;
using Microsoft.AspNetCore.Authorization;

[Authorize]
public class EmployeesController : Controller
{
    private readonly AppDbContext _context;

    public EmployeesController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string searchQuery = null)
    {
        var query = _context.Employees
            .Include(e => e.Position)
            .ThenInclude(p => p.Department)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(e => e.FullName.Contains(searchQuery) || 
                                     e.PhoneNumber.Contains(searchQuery) || 
                                     e.Position.Department.DepartmentName.Contains(searchQuery));
        }

        var totalEmployees = await query.CountAsync();
        var employees = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return View(employees);
    }

    public async Task<IActionResult> Details(int id)
    {
        var employee = await _context.Employees
            .Include(e => e.Position)
            .ThenInclude(p => p.Department)
            .FirstOrDefaultAsync(e => e.EmployeeID == id);

        if (employee == null)
        {
            return NotFound();
        }

        return View(employee);
    }

    public async Task<IActionResult> Create()
    {
        await LoadAvailablePositionsAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeViewModel model)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Account");
        if (user.Role != "Admin") return Forbid();

        if (ModelState.IsValid)
        {
            var employee = new Employee
            {
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                PositionID = model.PositionID
            };

            await ProcessPhotoAsync(model.Photo, employee);
            _context.Add(employee);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        await LoadAvailablePositionsAsync(model.PositionID);
        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var employee = await _context.Employees
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.EmployeeID == id);

        if (employee == null)
        {
            return NotFound();
        }

        var employeeViewModel = new EmployeeViewModel
        {
            EmployeeID = employee.EmployeeID,
            FullName = employee.FullName,
            PhoneNumber = employee.PhoneNumber,
            PositionID = employee.PositionID
        };

        await LoadAvailablePositionsAsync(employee.PositionID);
        return View(employeeViewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EmployeeViewModel model)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Account");
        if (user.Role != "Admin") return Forbid();

        if (id != model.EmployeeID) return BadRequest();

        if (ModelState.IsValid)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            employee.FullName = model.FullName;
            employee.PhoneNumber = model.PhoneNumber;
            employee.PositionID = model.PositionID;

            try
            {
                _context.Update(employee);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmployeeExists(employee.EmployeeID)) return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        await LoadAvailablePositionsAsync(model.PositionID);
        return View(model);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var employee = await _context.Employees
            .Include(e => e.Position)
            .ThenInclude(p => p.Department)
            .FirstOrDefaultAsync(e => e.EmployeeID == id);

        if (employee == null)
        {
            return NotFound();
        }

        return View(employee);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Account");
        if (user.Role != "Admin") return Forbid();

        var employee = await _context.Employees.FindAsync(id);
        if (employee != null)
        {
            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private bool EmployeeExists(int id)
    {
        return _context.Employees.Any(e => e.EmployeeID == id);
    }

    public async Task<IActionResult> GetPhoto(int id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null || employee.Photo == null)
        {
            return NotFound();
        }

        return File(employee.Photo, "image/jpeg");
    }

    public async Task<IActionResult> Search(string searchQuery)
    {
        var employees = _context.Employees
            .Include(e => e.Position)
            .ThenInclude(p => p.Department)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            employees = employees.Where(e =>
                e.FullName.Contains(searchQuery) ||
                e.PhoneNumber.Contains(searchQuery) ||
                e.Position.Department.DepartmentName.Contains(searchQuery));
        }

        var employeeList = await employees.ToListAsync();
        return PartialView("_EmployeeTablePartial", employeeList);
    }

    private async Task LoadAvailablePositionsAsync(int? selectedPositionId = null)
    {
        var availablePositions = await _context.Positions
            .Include(p => p.Employees)
            .Include(p => p.Department)
            .Where(p => p.Employees.Count() < p.MaxEmployees)
            .Select(p => new
            {
                p.PositionID,
                PositionName = p.PositionName + " | " + (p.Department != null ? p.Department.DepartmentName : "Без отдела")
            })
            .ToListAsync();

        var positionsList = new List<object>
        {
            new { PositionID = (int?)null, PositionName = "(Без должности)" }
        };
        
        positionsList.AddRange(availablePositions);
        ViewBag.PositionID = new SelectList(positionsList, "PositionID", "PositionName", selectedPositionId);
    }

    private async Task ProcessPhotoAsync(IFormFile photo, Employee employee)
    {
        if (photo != null && photo.Length > 0)
        {
            using (var memoryStream = new MemoryStream())
            {
                await photo.CopyToAsync(memoryStream);
                employee.Photo = memoryStream.ToArray();
            }
        }
    }

    protected async Task<User> GetCurrentUserAsync()
    {
        var username = User.Identity.Name;
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }
}
