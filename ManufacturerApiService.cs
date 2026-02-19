using System.Text.Json;
using DistributionAPI.DTOs;

namespace DistributionAPI.Services
{
    public class ManufacturerApiService : IManufacturerApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ManufacturerApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<StockResponseDTO> CheckManufacturerStock(int blanketId, int quantity)
        {
            try
            {
                var manufacturerUrl = _configuration["ManufacturerApi:BaseUrl"];
                var response = await _httpClient.GetAsync(
                    $"{manufacturerUrl}/api/manufacturer/stock/{blanketId}?quantity={quantity}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var stockResponse = JsonSerializer.Deserialize<StockResponseDTO>(
                        content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return stockResponse ?? new StockResponseDTO
                    {
                        IsAvailable = false,
                        Message = "Invalid response from manufacturer"
                    };
                }
                else
                {
                    return new StockResponseDTO
                    {
                        IsAvailable = false,
                        Message = "Unable to check manufacturer stock"
                    };
                }
            }
            catch (Exception ex)
            {
                return new StockResponseDTO
                {
                    IsAvailable = false,
                    Message = $"Error checking manufacturer stock: {ex.Message}"
                };
            }
        }
    }
}