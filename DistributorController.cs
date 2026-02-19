using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DistributionAPI.Data;
using DistributionAPI.Models;
using System.Text.Json;

namespace DistributionAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DistributorController : ControllerBase
    {
        private readonly DistributionDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly ILogger<DistributorController> _logger;
        private readonly IConfiguration _configuration;

        public DistributorController(
            DistributionDbContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<DistributorController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
            _configuration = configuration;
        }

        // GET: https://localhost:7013/api/distributor/blankets
        [HttpGet("blankets")]
        public async Task<IActionResult> GetBlankets()
        {
            try
            {
                var manufacturerApi = _configuration["ApiUrls:ManufacturerApi"];
                var response = await _httpClient.GetAsync($"{manufacturerApi}/api/manufacturer/blankets");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Ok(content);
                }
                else
                {
                    _logger.LogWarning("Manufacturer API returned {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to Manufacturer API");
            }

            var blankets = new[]
            {
                new { Id = 1, Name = "Winter Warm", ModelNumber = "CC-001", Price = 49.99m, Material = "Wool" },
                new { Id = 2, Name = "Summer Light", ModelNumber = "CC-002", Price = 39.99m, Material = "Cotton" },
                new { Id = 3, Name = "Luxury Silk", ModelNumber = "CC-003", Price = 89.99m, Material = "Silk" }
            };

            return Ok(blankets);
        }

        // GET: https://localhost:7013/api/distributor/inventory
        [HttpGet("inventory")]
        public async Task<IActionResult> GetInventory()
        {
            try
            {
                var inventory = await _context.Inventories.ToListAsync();
                return Ok(inventory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory");
                return StatusCode(500, new { message = "Error getting inventory", error = ex.Message });
            }
        }

        // POST: https://localhost:7013/api/distributor/order
        [HttpPost("order")]
        public async Task<IActionResult> PlaceOrder([FromBody] OrderRequest request)
        {
            try
            {
                var orderNumber = $"ORD-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";
                string status = "Processing";
                DateTime? estimatedDelivery = DateTime.Now.AddDays(3);
                decimal totalAmount = 0;
                var errors = new List<string>();

                foreach (var item in request.Items)
                {
                    var inventory = await _context.Inventories
                        .FirstOrDefaultAsync(i => i.BlanketId == item.BlanketId && i.DistributorId == 1);

                    if (inventory == null || inventory.Quantity < item.Quantity)
                    {
                        var manufacturerApi = _configuration["ApiUrls:ManufacturerApi"];
                        try
                        {
                            var manufacturerResponse = await _httpClient.GetAsync(
                                $"{manufacturerApi}/api/manufacturer/stock/{item.BlanketId}?quantity={item.Quantity}");

                            if (manufacturerResponse.IsSuccessStatusCode)
                            {
                                var content = await manufacturerResponse.Content.ReadAsStringAsync();
                                var stockInfo = JsonSerializer.Deserialize<StockInfo>(content,
                                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                                if (stockInfo?.IsAvailable == true)
                                {
                                    status = "Awaiting Manufacturer";
                                    estimatedDelivery = DateTime.Now.AddDays(stockInfo.LeadTimeDays + 2);

                                    // Send order to Manufacturer API
                                    var manufacturerOrder = new
                                    {
                                        DistributorId = 1,
                                        OrderNumber = orderNumber,
                                        Items = new[]
                                        {
                                            new { BlanketId = item.BlanketId, Quantity = item.Quantity }
                                        },
                                        LeadTimeDays = stockInfo.LeadTimeDays
                                    };

                                    var manufacturerJson = JsonSerializer.Serialize(manufacturerOrder);
                                    var manufacturerContent = new StringContent(manufacturerJson,
                                        System.Text.Encoding.UTF8, "application/json");

                                    await _httpClient.PostAsync($"{manufacturerApi}/api/manufacturer/order", manufacturerContent);
                                }
                                else
                                {
                                    errors.Add($"Insufficient stock for blanket ID: {item.BlanketId} - {stockInfo?.Message}");
                                }
                            }
                            else
                            {
                                errors.Add($"Cannot check manufacturer stock for blanket ID: {item.BlanketId}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error checking manufacturer stock");
                            errors.Add($"Error checking stock for blanket ID: {item.BlanketId}");
                        }
                    }
                    else
                    {
                        // Update inventory
                        inventory.Quantity -= item.Quantity;
                        _context.Inventories.Update(inventory);
                    }

                    totalAmount += item.Quantity * GetBlanketPrice(item.BlanketId);
                }

                if (errors.Count > 0)
                {
                    return BadRequest(new
                    {
                        message = "Order cannot be fulfilled",
                        errors = errors
                    });
                }

                var order = new SellerOrder
                {
                    SellerId = request.SellerId,
                    CustomerName = request.CustomerName,
                    OrderNumber = orderNumber,
                    TotalAmount = totalAmount,
                    Status = status,
                    OrderDate = DateTime.Now,
                    EstimatedDelivery = estimatedDelivery
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    OrderId = order.Id,
                    OrderNumber = orderNumber,
                    Status = status,
                    Message = "Order placed successfully",
                    EstimatedDelivery = estimatedDelivery?.ToString("yyyy-MM-dd"),
                    TotalAmount = totalAmount.ToString("0.00")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order");
                return StatusCode(500, new { message = "Error processing order", error = ex.Message });
            }
        }

        // GET: api/distributor/orders?sellerId=1
        [HttpGet("orders")]
        public async Task<IActionResult> GetSellerOrders([FromQuery] int sellerId)
        {
            try
            {
                var orders = await _context.Orders
                    .Where(o => o.SellerId == sellerId)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                if (!orders.Any())
                {
                    return NotFound(new { message = $"No orders found for seller ID: {sellerId}" });
                }

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders for seller {SellerId}", sellerId);
                return StatusCode(500, new { message = "Error getting orders", error = ex.Message });
            }
        }

        // GET: api/distributor/orders/5
        [HttpGet("orders/{orderId}")]
        public async Task<IActionResult> GetOrderDetails(int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    return NotFound(new { message = $"Order with ID {orderId} not found" });
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order {OrderId}", orderId);
                return StatusCode(500, new { message = "Error getting order details", error = ex.Message });
            }
        }

        // GET: api/distributor/order/by-number/ORD-20250211-ABC123
        [HttpGet("order/by-number/{orderNumber}")]
        public async Task<IActionResult> GetOrderByNumber(string orderNumber)
        {
            try
            {
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

                if (order == null)
                {
                    return NotFound(new { message = $"Order {orderNumber} not found" });
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order {OrderNumber}", orderNumber);
                return StatusCode(500, new { message = "Error getting order" });
            }
        }

        // GET: https://localhost:7013/api/distributor/test
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new
            {
                message = "Distribution API is working!",
                timestamp = DateTime.Now,
                manufacturerApi = _configuration["ApiUrls:ManufacturerApi"],
                database = "SQL Server LocalDB",
                apiUrl = "https://localhost:7013"
            });
        }

        private decimal GetBlanketPrice(int blanketId)
        {
            return blanketId switch
            {
                1 => 49.99m,
                2 => 39.99m,
                3 => 89.99m,
                _ => 50.00m
            };
        }

        private class StockInfo
        {
            public bool IsAvailable { get; set; }
            public int LeadTimeDays { get; set; }
            public string Message { get; set; } = string.Empty;
        }
    }

    public class OrderRequest
    {
        public int SellerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }

    public class OrderItem
    {
        public int BlanketId { get; set; }
        public int Quantity { get; set; }
    }
}