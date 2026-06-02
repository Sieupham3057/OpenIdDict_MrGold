using AuthDemo.Api.Authorization;
using AuthDemo.Api.Data;
using AuthDemo.Api.Models.Dtos;
using AuthDemo.Api.Models.Entities;
using AuthDemo.Api.Models.Requests;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly ILogger<RolesController> _logger;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ApplicationDbContext _dbContext;

    public RolesController(
        ILogger<RolesController> logger,
        RoleManager<IdentityRole<Guid>> roleManager,
        ApplicationDbContext dbContext)
    {
        _roleManager = roleManager;
        _dbContext = dbContext;
    }

    // GET /api/roles
    [HttpGet]
    [HasPermission("ROLE", "VIEW")]
    public async Task<IActionResult> GetAll()
    {
        //_logger.LogInformation("Fetching all roles.");
        var roles = await _roleManager.Roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto { Id = r.Id, Name = r.Name! })
            .ToListAsync();

        return Ok(roles);
    }

    // GET /api/roles/{id}
    [HttpGet("{id:guid}")]
    [HasPermission("ROLE", "VIEW")]
    public async Task<IActionResult> GetById(Guid id)
    {
        //_logger.LogInformation("Fetching role with ID: {RoleId}", id);
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null) return NotFound(new { message = "Không tìm thấy role." });

        return Ok(new RoleDto { Id = role.Id, Name = role.Name! });
    }

    // POST /api/roles
    [HttpPost]
    [HasPermission("ROLE", "CREATE")]
    public async Task<IActionResult> Create([FromBody] string roleName)
    {
        //_logger.LogInformation("Creating role with name: {RoleName}", roleName);
        if (string.IsNullOrWhiteSpace(roleName))
            return BadRequest(new { message = "Tên role không được để trống." });

        var result = await _roleManager.CreateAsync(new IdentityRole<Guid> { Name = roleName });
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        var created = await _roleManager.FindByNameAsync(roleName);
        return CreatedAtAction(nameof(GetById), new { id = created!.Id },
            new RoleDto { Id = created.Id, Name = created.Name! });
    }

    // PUT /api/roles/{id}
    [HttpPut("{id:guid}")]
    [HasPermission("ROLE", "EDIT")]
    public async Task<IActionResult> Update(Guid id, [FromBody] string roleName)
    {
        //_logger.LogInformation("Updating role with ID: {RoleId} and new name: {RoleName}", id, roleName);
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null) return NotFound(new { message = "Không tìm thấy role." });

        role.Name = roleName;
        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new RoleDto { Id = role.Id, Name = role.Name! });
    }

    // DELETE /api/roles/{id}
    [HttpDelete("{id:guid}")]
    [HasPermission("ROLE", "DELETE")]
    public async Task<IActionResult> Delete(Guid id)
    {
        //_logger.LogInformation("Deleting role with ID: {RoleId}", id);
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null) return NotFound(new { message = "Không tìm thấy role." });

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return NoContent();
    }

    // GET /api/roles/{id}/permissions
    // Trả về toàn bộ Function+Action matrix với trạng thái checked/unchecked cho role này
    [HttpGet("{id:guid}/permissions")]
    [HasPermission("ROLE", "VIEW")]
    public async Task<IActionResult> GetPermissions(Guid id)
    {
        //_logger.LogInformation("Fetching permissions for role with ID: {RoleId}", id);
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null) return NotFound(new { message = "Không tìm thấy role." });

        // Lấy tất cả Functions kèm Actions được phép trong function đó
        var functions = await _dbContext.Functions
            .Where(f => f.IsActive)
            .Include(f => f.ActionInFunctions)
                .ThenInclude(aif => aif.Action)
            .OrderBy(f => f.SortOrder)
            .ToListAsync();

        // Lấy permissions hiện tại của role
        var currentPermissions = await _dbContext.Permissions
            .Where(p => p.RoleId == id)
            .Select(p => new { p.FunctionId, p.ActionId })
            .ToListAsync();

        var permissionSet = currentPermissions
            .Select(p => $"{p.FunctionId}:{p.ActionId}")
            .ToHashSet();

        var result = new RolePermissionDto
        {
            Id = role.Id,
            Name = role.Name!,
            Permissions = functions.Select(f => new FunctionPermissionDto
            {
                FunctionId = f.Id,
                FunctionName = f.Name,
                Actions = f.ActionInFunctions
                    .Where(aif => aif.Action.IsActive)
                    .OrderBy(aif => aif.Action.SortOrder)
                    .Select(aif => new ActionPermissionDto
                    {
                        ActionId = aif.ActionId,
                        ActionName = aif.Action.Name,
                        HasPermission = permissionSet.Contains($"{f.Id}:{aif.ActionId}")
                    })
                    .ToList()
            }).ToList()
        };

        return Ok(result);
    }

    // PUT /api/roles/{id}/permissions
    // Ghi đè toàn bộ permissions của role (replace, không merge)
    [HttpPut("{id:guid}/permissions")]
    [HasPermission("ROLE", "EDIT")]
    public async Task<IActionResult> SavePermissions(
        Guid id, [FromBody] SavePermissionsRequest request)
    {
        //_logger.LogInformation("Saving permissions for role with ID: {RoleId}", id);
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null) return NotFound(new { message = "Không tìm thấy role." });

        // Xóa toàn bộ permissions cũ của role
        var oldPermissions = await _dbContext.Permissions
            .Where(p => p.RoleId == id)
            .ToListAsync();
        _dbContext.Permissions.RemoveRange(oldPermissions);

        // Thêm permissions mới
        var newPermissions = request.Permissions
            .Select(p => new Permission
            {
                RoleId = id,
                FunctionId = p.FunctionId,
                ActionId = p.ActionId
            })
            .ToList();

        await _dbContext.Permissions.AddRangeAsync(newPermissions);
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Lưu phân quyền thành công.", count = newPermissions.Count });
    }
}
