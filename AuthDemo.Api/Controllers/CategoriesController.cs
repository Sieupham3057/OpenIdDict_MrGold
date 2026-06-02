using AuthDemo.Api.Authorization;
using AuthDemo.Api.Data;
using AuthDemo.Api.Models.Dtos;
using AuthDemo.Api.Models.Entities;
using AuthDemo.Api.Models.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CategoriesController(ApplicationDbContext db) => _db = db;

    // GET /api/categories?page=1&pageSize=20&search=
    [HttpGet]
    [HasPermission("PRODUCT", "VIEW")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        var query = _db.Categories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CategoryDto
            {
                Id           = c.Id,
                Name         = c.Name,
                Description  = c.Description,
                IsActive     = c.IsActive,
                CreatedAt    = c.CreatedAt,
                ProductCount = c.Products.Count(p => p.IsActive),
            })
            .ToListAsync();

        return Ok(new { data = items, totalCount, page, pageSize });
    }

    // GET /api/categories/{id}
    [HttpGet("{id:int}")]
    [HasPermission("PRODUCT", "VIEW")]
    public async Task<IActionResult> GetById(int id)
    {
        var c = await _db.Categories
            .Include(x => x.Products.Where(p => p.IsActive))
            .FirstOrDefaultAsync(x => x.Id == id);

        if (c == null) return NotFound(new { message = "Không tìm thấy danh mục." });

        return Ok(new CategoryDto
        {
            Id           = c.Id,
            Name         = c.Name,
            Description  = c.Description,
            IsActive     = c.IsActive,
            CreatedAt    = c.CreatedAt,
            ProductCount = c.Products.Count,
        });
    }

    // POST /api/categories
    [HttpPost]
    [HasPermission("PRODUCT", "CREATE")]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        var category = new Category
        {
            Name        = request.Name,
            Description = request.Description,
            CreatedAt   = DateTime.UtcNow,
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = category.Id }, new CategoryDto
        {
            Id          = category.Id,
            Name        = category.Name,
            Description = category.Description,
            IsActive    = category.IsActive,
            CreatedAt   = category.CreatedAt,
        });
    }

    // PUT /api/categories/{id}
    [HttpPut("{id:int}")]
    [HasPermission("PRODUCT", "EDIT")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest request)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return NotFound(new { message = "Không tìm thấy danh mục." });

        category.Name        = request.Name;
        category.Description = request.Description;
        category.IsActive    = request.IsActive;
        category.UpdatedAt   = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Cập nhật thành công." });
    }

    // DELETE /api/categories/{id}
    [HttpDelete("{id:int}")]
    [HasPermission("PRODUCT", "DELETE")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return NotFound(new { message = "Không tìm thấy danh mục." });

        var hasProducts = await _db.Products.AnyAsync(p => p.CategoryId == id);
        if (hasProducts)
            return BadRequest(new { message = "Danh mục đang có sản phẩm, không thể xóa." });

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
