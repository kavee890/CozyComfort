namespace DistributionAPI.Services
{
    public interface IManufacturerApiService
    {
        Task<DTOs.StockResponseDTO> CheckManufacturerStock(int blanketId, int quantity);
    }
}