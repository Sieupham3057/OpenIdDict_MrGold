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
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ProductsController(ApplicationDbContext db) => _db = db;

    // GET /api/products?page=1&pageSize=20&search=&categoryId=
    [HttpGet]
    [HasPermission("PRODUCT", "VIEW")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] int? categoryId = null)
    {
        var query = _db.Products.Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || (p.Description != null && p.Description.Contains(search)));

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductDto
            {
                Id           = p.Id,
                CategoryId   = p.CategoryId,
                CategoryName = p.Category.Name,
                Name         = p.Name,
                Description  = p.Description,
                Price        = p.Price,
                Stock        = p.Stock,
                ImageUrl     = p.ImageUrl,
                IsActive     = p.IsActive,
                CreatedAt    = p.CreatedAt,
            })
            .ToListAsync();

        return Ok(new { data = items, totalCount, page, pageSize });
    }

    // GET /api/products/{id}
    [HttpGet("{id:int}")]
    [HasPermission("PRODUCT", "VIEW")]
    public async Task<IActionResult> GetById(int id)
    {
        var p = await _db.Products
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (p == null) return NotFound(new { message = "Không tìm thấy sản phẩm." });

        return Ok(new ProductDto
        {
            Id           = p.Id,
            CategoryId   = p.CategoryId,
            CategoryName = p.Category.Name,
            Name         = p.Name,
            Description  = p.Description,
            Price        = p.Price,
            Stock        = p.Stock,
            ImageUrl     = p.ImageUrl,
            IsActive     = p.IsActive,
            CreatedAt    = p.CreatedAt,
        });
    }

    // POST /api/products
    [HttpPost]
    [HasPermission("PRODUCT", "CREATE")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == request.CategoryId);
        if (!categoryExists)
            return BadRequest(new { message = "Danh mục không tồn tại." });

        var product = new Product
        {
            CategoryId  = request.CategoryId,
            Name        = request.Name,
            Description = request.Description,
            Price       = request.Price,
            Stock       = request.Stock,
            ImageUrl    = request.ImageUrl,
            CreatedAt   = DateTime.UtcNow,
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, new { product.Id, product.Name });
    }

    // PUT /api/products/{id}
    [HttpPut("{id:int}")]
    [HasPermission("PRODUCT", "EDIT")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound(new { message = "Không tìm thấy sản phẩm." });

        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == request.CategoryId);
        if (!categoryExists)
            return BadRequest(new { message = "Danh mục không tồn tại." });

        product.CategoryId  = request.CategoryId;
        product.Name        = request.Name;
        product.Description = request.Description;
        product.Price       = request.Price;
        product.Stock       = request.Stock;
        product.ImageUrl    = request.ImageUrl;
        product.IsActive    = request.IsActive;
        product.UpdatedAt   = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Cập nhật thành công." });
    }

    // DELETE /api/products/{id}
    [HttpDelete("{id:int}")]
    [HasPermission("PRODUCT", "DELETE")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound(new { message = "Không tìm thấy sản phẩm." });

        // Soft delete — giữ lại record để OrderItem còn tham chiếu được
        product.IsActive  = false;
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
