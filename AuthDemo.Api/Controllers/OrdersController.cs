using AuthDemo.Api.Authorization;
using AuthDemo.Api.Data;
using AuthDemo.Api.Models.Dtos;
using AuthDemo.Api.Models.Entities;
using AuthDemo.Api.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;

namespace AuthDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public OrdersController(ApplicationDbContext db) => _db = db;

    // GET /api/orders — admin/manager xem tất cả đơn hàng, có phân trang
    [HttpGet]
    [HasPermission("ORDER", "VIEW")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] OrderStatus? status = null)
    {
        var query = _db.Orders.Include(o => o.User).Include(o => o.Items).ThenInclude(i => i.Product).AsQueryable();

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => MapToDto(o))
            .ToListAsync();

        return Ok(new { data = items, totalCount, page, pageSize });
    }

    // GET /api/orders/my — user hiện tại xem đơn hàng của mình
    [HttpGet("my")]
    public async Task<IActionResult> GetMy([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var query = _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Where(o => o.UserId == userId.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => MapToDto(o))
            .ToListAsync();

        return Ok(new { data = items, totalCount, page, pageSize });
    }

    // GET /api/orders/{id}
    [HttpGet("{id:long}")]
    [HasPermission("ORDER", "VIEW")]
    public async Task<IActionResult> GetById(long id)
    {
        var order = await _db.Orders
            .Include(o => o.User)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound(new { message = "Không tìm thấy đơn hàng." });

        return Ok(MapToDto(order));
    }

    // POST /api/orders — user tạo đơn hàng cho mình
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        // Lấy thông tin sản phẩm trong 1 query
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToDictionaryAsync(p => p.Id);

        // Kiểm tra sản phẩm có tồn tại và đủ tồn kho không
        foreach (var item in request.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                return BadRequest(new { message = $"Sản phẩm ID {item.ProductId} không tồn tại." });

            if (product.Stock < item.Quantity)
                return BadRequest(new { message = $"Sản phẩm '{product.Name}' không đủ tồn kho (còn {product.Stock})." });
        }

        var order = new Order
        {
            UserId    = userId.Value,
            Note      = request.Note,
            CreatedAt = DateTime.UtcNow,
            Items     = request.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                Quantity  = i.Quantity,
                UnitPrice = products[i.ProductId].Price,
            }).ToList(),
        };

        order.TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice);

        // Trừ tồn kho
        foreach (var item in request.Items)
            products[item.ProductId].Stock -= item.Quantity;

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, new { order.Id, order.TotalAmount, order.Status });
    }

    // PUT /api/orders/{id}/status — admin/manager cập nhật trạng thái
    [HttpPut("{id:long}/status")]
    [HasPermission("ORDER", "EDIT")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateOrderStatusRequest request)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound(new { message = "Không tìm thấy đơn hàng." });

        order.Status    = request.Status;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Cập nhật trạng thái thành công.", status = order.Status.ToString() });
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────────

    private Guid? GetCurrentUserId()
    {
        var sub = User.GetClaim(OpenIddictConstants.Claims.Subject);
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private static OrderDto MapToDto(Order o) => new()
    {
        Id          = o.Id,
        UserId      = o.UserId,
        UserName    = o.User?.UserName ?? string.Empty,
        Status      = o.Status,
        TotalAmount = o.TotalAmount,
        Note        = o.Note,
        CreatedAt   = o.CreatedAt,
        Items       = o.Items.Select(i => new OrderItemDto
        {
            Id          = i.Id,
            ProductId   = i.ProductId,
            ProductName = i.Product?.Name ?? string.Empty,
            Quantity    = i.Quantity,
            UnitPrice   = i.UnitPrice,
        }).ToList(),
    };
}
