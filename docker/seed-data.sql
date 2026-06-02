-- =============================================================================
-- Seed dữ liệu Categories và Products cho load testing
-- Chạy trên SQL Server sau khi API đã migrate DB lần đầu
-- =============================================================================

USE AuthDemo;   -- Đổi tên database nếu khác
GO

-- ── 1. Categories ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM Categories)
BEGIN
    INSERT INTO Categories (Name, Description, IsActive, CreatedAt) VALUES
    (N'Điện thoại',          N'Smartphone các loại',       1, GETUTCDATE()),
    (N'Laptop',              N'Máy tính xách tay',          1, GETUTCDATE()),
    (N'Phụ kiện',            N'Tai nghe, sạc, ốp lưng',    1, GETUTCDATE()),
    (N'Máy tính bảng',       N'iPad, Android tablet',       1, GETUTCDATE()),
    (N'Đồng hồ thông minh',  N'Smartwatch các hãng',        1, GETUTCDATE());

    PRINT 'Đã seed 5 categories';
END
ELSE
    PRINT 'Categories đã tồn tại, bỏ qua.';
GO

-- ── 2. Products (50 sản phẩm, 10 per category) ───────────────────────────────

IF NOT EXISTS (SELECT 1 FROM Products)
BEGIN
    DECLARE @i INT = 1;
    DECLARE @catCount INT = (SELECT COUNT(*) FROM Categories);

    WHILE @i <= 50
    BEGIN
        INSERT INTO Products (CategoryId, Name, Description, Price, Stock, IsActive, CreatedAt)
        VALUES (
            (@i % @catCount) + 1,
            N'Sản phẩm ' + CAST(@i AS NVARCHAR(10)),
            N'Mô tả chi tiết cho sản phẩm số ' + CAST(@i AS NVARCHAR(10)),
            CAST(100000 + (@i * 75000) AS DECIMAL(18,2)),
            500,    -- Tồn kho đủ cho stress test
            1,
            GETUTCDATE()
        );
        SET @i = @i + 1;
    END;

    PRINT 'Đã seed 50 products';
END
ELSE
    PRINT 'Products đã tồn tại, bỏ qua.';
GO

-- ── Kiểm tra kết quả ──────────────────────────────────────────────────────────

SELECT
    c.Name AS Category,
    COUNT(p.Id) AS ProductCount
FROM Categories c
LEFT JOIN Products p ON p.CategoryId = c.Id AND p.IsActive = 1
GROUP BY c.Name
ORDER BY c.Name;
GO
