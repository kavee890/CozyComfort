namespace DistributionAPI.Models
{
    public class SellerOrder
    {
        public int Id { get; set; }
        public int SellerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public DateTime? EstimatedDelivery { get; set; }
    }
}