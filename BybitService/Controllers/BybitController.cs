using BybitService.Models;
using BybitService.Services;
using Microsoft.AspNetCore.Mvc;

namespace BybitService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BybitController : ControllerBase
    {
        private readonly IBybitService _bybitService;
        private readonly ILogger<BybitController> _logger;

        public BybitController(IBybitService bybitService, ILogger<BybitController> logger)
        {
            _bybitService = bybitService;
            _logger = logger;
        }

        [HttpGet("price/{symbol}")]
        public async Task<ActionResult<ApiResponse<decimal>>> GetPrice(string symbol = "BTCUSDT")
        {
            try
            {
                var price = await _bybitService.GetPriceAsync(symbol);
                return Ok(ApiResponse<decimal>.SuccessResponse(price));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении цены с Bybit");
                return StatusCode(500, ApiResponse<decimal>.ErrorResponse("Внутренняя ошибка сервера"));
            }
        }

        [HttpGet("orderbook/{symbol}")]
        public async Task<ActionResult<ApiResponse<OrderBookData>>> GetOrderBook(
            string symbol = "BTCUSDT",
            [FromQuery] int limit = 30)
        {
            try
            {
                var orderBook = await _bybitService.GetOrderBookAsync(symbol, limit);
                return Ok(ApiResponse<OrderBookData>.SuccessResponse(orderBook));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении стакана заявок с Bybit");
                return StatusCode(500, ApiResponse<OrderBookData>.ErrorResponse("Внутренняя ошибка сервера"));
            }
        }

        [HttpGet("marketstats/{symbol}")]
        public async Task<ActionResult<ApiResponse<MarketStats>>> GetMarketStats(string symbol = "BTCUSDT")
        {
            try
            {
                var stats = await _bybitService.GetMarketStatsAsync(symbol);
                return Ok(ApiResponse<MarketStats>.SuccessResponse(stats));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении рыночной статистики с Bybit");
                return StatusCode(500, ApiResponse<MarketStats>.ErrorResponse("Внутренняя ошибка сервера"));
            }
        }

        [HttpGet("health")]
        public async Task<ActionResult<ApiResponse<string>>> HealthCheck()
        {
            try
            {
                var price = await _bybitService.GetPriceAsync("BTCUSDT");
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