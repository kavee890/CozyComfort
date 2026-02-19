namespace DistributionAPI.Models
{
    public class DistributorInventory
    {
        public int Id { get; set; }
        public int DistributorId { get; set; }
        public int BlanketId { get; set; }
        public int Quantity { get; set; }
        public int ReorderLevel { get; set; } = 10;
        public DateTime LastRestocked { get; set; } = DateTime.Now;
    }
}