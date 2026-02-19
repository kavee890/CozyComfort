using DistributionAPI.Models;

namespace DistributionAPI.Models
{

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int BlanketId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;

        // Navigation property
        public SellerOrder? Order { get; set; }
    }
}