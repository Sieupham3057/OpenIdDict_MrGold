using AuthDemo.Api.Authorization;
using AuthDemo.Api.Models;
using AuthDemo.Api.Models.Dtos;
using AuthDemo.Api.Models.Requests;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;

namespace AuthDemo.Api.Controllers;

// CRUD quản lý user — mọi endpoint đều yêu cầu Bearer token
// và quyền cụ thể qua [HasPermission("USER", "...")]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    // GET /api/users?page=1&pageSize=20&search=abc
    // Phân trang + tìm kiếm theo username, email, hoặc họ tên
    [HttpGet]
    [HasPermission("USER", "VIEW")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u =>
                u.UserName!.Contains(search) ||
                u.Email!.Contains(search) ||
                (u.FullName != null && u.FullName.Contains(search)));

        var totalCount = await query.CountAsync();
        var users = await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // GetRolesAsync cần gọi riêng cho từng user (không thể JOIN trong 1 query)
        // → có N+1 query ở đây, chấp nhận được với pageSize nhỏ
        var dtos = new List<UserDto>();
        foreach (var u in users)
        {
            var dto = await MapToDto(u);
            dtos.Add(dto);
        }

        return Ok(new { data = dtos, totalCount, page, pageSize });
    }

    // GET /api/users/{id}
    [HttpGet("{id:guid}")]
    [HasPermission("USER", "VIEW")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound(new { message = "Không tìm thấy user." });

        return Ok(await MapToDto(user));
    }

    // POST /api/users
    // Tạo user mới với mật khẩu và gán role ngay khi tạo
    [HttpPost]
    [HasPermission("USER", "CREATE")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email,
            FullName = request.FullName,
            FirstName = request.FirstName,
            LastName = request.LastName,
            DateOfBirth = request.DateOfBirth,
            EmailConfirmed = true, // Admin tạo → coi như email đã xác nhận
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        if (request.Roles.Any())
        {
            var roleResult = await _userManager.AddToRolesAsync(user, request.Roles);
            if (!roleResult.Succeeded)
                return BadRequest(new { errors = roleResult.Errors.Select(e => e.Description) });
        }

        // 201 Created kèm Location header trỏ đến GET /api/users/{id}
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, await MapToDto(user));
    }

    // PUT /api/users/{id}
    // Chỉ cập nhật các trường được gửi lên (null = không thay đổi)
    [HttpPut("{id:guid}")]
    [HasPermission("USER", "EDIT")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound(new { message = "Không tìm thấy user." });

        if (request.FullName != null) user.FullName = request.FullName;
        if (request.FirstName != null) user.FirstName = request.FirstName;
        if (request.LastName != null) user.LastName = request.LastName;
        if (request.DateOfBirth.HasValue) user.DateOfBirth = request.DateOfBirth;
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;
        if (request.AvatarUrl != null) user.AvatarUrl = request.AvatarUrl;

        // ChangeEmailAsync yêu cầu token xác nhận — dùng GenerateChangeEmailTokenAsync
        // để bỏ qua bước gửi email (admin đổi trực tiếp)
        if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
        {
            var token = await _userManager.GenerateChangeEmailTokenAsync(user, request.Email);
            var emailResult = await _userManager.ChangeEmailAsync(user, request.Email, token);
            if (!emailResult.Succeeded)
                return BadRequest(new { errors = emailResult.Errors.Select(e => e.Description) });
        }

        user.UpdatedAt = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(await MapToDto(user));
    }

    // DELETE /api/users/{id}
    [HttpDelete("{id:guid}")]
    [HasPermission("USER", "DELETE")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound(new { message = "Không tìm thấy user." });

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return NoContent(); // 204 — xóa thành công, không có body
    }

    // POST /api/users/{id}/change-password
    // Admin đặt lại mật khẩu mà không cần biết mật khẩu cũ
    [HttpPost("{id:guid}/change-password")]
    [HasPermission("USER", "EDIT")]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound(new { message = "Không tìm thấy user." });

        // GeneratePasswordResetToken → tạo token nội bộ (không gửi email)
        // ResetPasswordAsync dùng token đó để đặt mật khẩu mới, bỏ qua kiểm tra mật khẩu cũ
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new { message = "Đổi mật khẩu thành công." });
    }

    // GET /api/users/{id}/roles
    [HttpGet("{id:guid}/roles")]
    [HasPermission("USER", "VIEW")]
    public async Task<IActionResult> GetRoles(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound(new { message = "Không tìm thấy user." });

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(roles);
    }

    // PUT /api/users/{id}/roles
    // Replace toàn bộ role của user (xóa cũ → thêm mới), không merge
    [HttpPut("{id:guid}/roles")]
    [HasPermission("USER", "EDIT")]
    public async Task<IActionResult> AssignRoles(Guid id, [FromBody] AssignRolesRequest request)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound(new { message = "Không tìm thấy user." });

        var currentRoles = await _userManager.GetRolesAsync(user);

        // Xóa toàn bộ role cũ trước
        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
            return BadRequest(new { errors = removeResult.Errors.Select(e => e.Description) });

        // Thêm role mới (nếu có)
        if (request.Roles.Any())
        {
            var addResult = await _userManager.AddToRolesAsync(user, request.Roles);
            if (!addResult.Succeeded)
                return BadRequest(new { errors = addResult.Errors.Select(e => e.Description) });
        }

        return Ok(new { message = "Gán role thành công.", roles = request.Roles });
    }

    // POST /api/users/{id}/toggle-lock
    // Khóa vĩnh viễn (DateTimeOffset.MaxValue) hoặc mở khóa (null)
    [HttpPost("{id:guid}/toggle-lock")]
    [HasPermission("USER", "EDIT")]
    public async Task<IActionResult> ToggleLock(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound(new { message = "Không tìm thấy user." });

        var isLockedOut = await _userManager.IsLockedOutAsync(user);

        if (isLockedOut)
        {
            // Mở khóa: đặt LockoutEnd = null
            await _userManager.SetLockoutEndDateAsync(user, null);
            return Ok(new { message = "Đã mở khóa tài khoản.", isLocked = false });
        }
        else
        {
            // Khóa vĩnh viễn: đặt LockoutEnd = DateTimeOffset.MaxValue (~năm 9999)
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            return Ok(new { message = "Đã khóa tài khoản.", isLocked = true });
        }
    }

    // ─── Helper ───────────────────────────────────────────────────────────────────

    // Map entity → DTO để không trả về PasswordHash và các trường nhạy cảm
    private async Task<UserDto> MapToDto(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            IsActive = user.IsActive,
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt,
            EmailConfirmed = user.EmailConfirmed,
            // IsLockedOut: LockoutEnd có giá trị VÀ còn trong tương lai
            IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
            Roles = roles,
        };
    }
}
