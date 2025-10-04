using BinanceService.Models;
using BinanceService.Services;
using Microsoft.AspNetCore.Mvc;

namespace BinanceService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BinanceController : ControllerBase
    {
        private readonly IBinanceService _binanceService;
        private readonly ILogger<BinanceController> _logger;

        public BinanceController(IBinanceService binanceService, ILogger<BinanceController> logger)
        {
            _binanceService = binanceService;
            _logger = logger;
        }

        [HttpGet("price/{symbol}")]
        public async Task<ActionResult<ApiResponse<decimal>>> GetPrice(string symbol = "BTCUSDT")
        {
            try
            {
                var price = await _binanceService.GetPriceAsync(symbol);
                return Ok(ApiResponse<decimal>.SuccessResponse(price));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "������ ��� ��������� ����");
                return StatusCode(500, ApiResponse<decimal>.ErrorResponse("���������� ������ �������"));
            }
        }

        [HttpGet("orderbook/{symbol}")]
        public async Task<ActionResult<ApiResponse<OrderBookData>>> GetOrderBook(
            string symbol = "BTCUSDT",
            [FromQuery] int limit = 30)
        {
            try
            {
                var orderBook = await _binanceService.GetOrderBookAsync(symbol, limit);
                return Ok(ApiResponse<OrderBookData>.SuccessResponse(orderBook));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "������ ��� ��������� ������� ������");
                return StatusCode(500, ApiResponse<OrderBookData>.ErrorResponse("���������� ������ �������"));
            }
        }

        [HttpGet("marketstats/{symbol}")]
        public async Task<ActionResult<ApiResponse<MarketStats>>> GetMarketStats(string symbol = "BTCUSDT")
        {
            try
            {
                var stats = await _binanceService.GetMarketStatsAsync(symbol);
                return Ok(ApiResponse<MarketStats>.SuccessResponse(stats));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "������ ��� ��������� �������� ����������");
                return StatusCode(500, ApiResponse<MarketStats>.ErrorResponse("���������� ������ �������"));
            }
        }

        [HttpGet("health")]
        public async Task<ActionResult<ApiResponse<string>>> HealthCheck()
        {
            try
            {
                // ��������, ��� ������ ��������
                var price = await _binanceService.GetPriceAsync("BTCUSDT");
                return Ok(ApiResponse<string>.SuccessResponse("������ �������� ���������"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�������� �������� �� �������");
                return StatusCode(503, ApiResponse<string>.ErrorResponse("������ ����������"));
            }
        }
    }
}