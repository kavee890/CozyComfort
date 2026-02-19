namespace DistributionAPI.DTOs
{
    public class OrderRequestDTO
    {
        public int SellerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public List<OrderItemDTO> Items { get; set; } = new List<OrderItemDTO>();
    }

    public class OrderItemDTO
    {
        public int BlanketId { get; set; }
        public int Quantity { get; set; }
    }

    public class StockResponseDTO
    {
        public bool IsAvailable { get; set; }
        public int AvailableQuantity { get; set; }
        public int LeadTimeDays { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class OrderResponseDTO
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime? EstimatedDelivery { get; set; }
    }
}