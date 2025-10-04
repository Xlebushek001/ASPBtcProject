using ComparisonService.Models;
using ComparisonService.Services;
using Microsoft.AspNetCore.Mvc;

namespace ComparisonService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComparisonController : ControllerBase
    {
        private readonly IComparisonService _comparisonService;
        private readonly ILogger<ComparisonController> _logger;

        public ComparisonController(IComparisonService comparisonService, ILogger<ComparisonController> logger)
        {
            _comparisonService = comparisonService;
            _logger = logger;
        }

        [HttpGet("prices/{symbol}")]
        public async Task<ActionResult<ApiResponse<ComparisonResult>>> ComparePrices(string symbol = "BTCUSDT")
        {
            try
            {
                var result = await _comparisonService.ComparePricesAsync(symbol);
                return Ok(ApiResponse<ComparisonResult>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сравнении цен");
                return StatusCode(500, ApiResponse<ComparisonResult>.ErrorResponse("Внутренняя ошибка сервера"));
            }
        }

        [HttpGet("marketstats/{symbol}")]
        public async Task<ActionResult<ApiResponse<ComparisonResult>>> CompareMarketStats(string symbol = "BTCUSDT")
        {
            try
            {
                var result = await _comparisonService.CompareMarketStatsAsync(symbol);
                return Ok(ApiResponse<ComparisonResult>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сравнении рыночной статистики");
                return StatusCode(500, ApiResponse<ComparisonResult>.ErrorResponse("Внутренняя ошибка сервера"));
            }
        }

        [HttpGet("health")]
        public async Task<ActionResult<ApiResponse<string>>> HealthCheck()
        {
            try
            {
                // Проверяем доступность обоих сервисов
                var result = await _comparisonService.ComparePricesAsync();
                return Ok(ApiResponse<string>.SuccessResponse("Сервис работает нормально"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Проверка здоровья не удалась");
                return StatusCode(503, ApiResponse<string>.ErrorResponse("Сервис недоступен"));
            }
        }
    }
}