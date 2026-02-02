using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using StockQuoteAlert.Models;
using StockQuoteAlert.Services;
using Xunit;

namespace StockQuoteAlert.Tests;

public class StockMonitorServiceTests
{
    private readonly MonitorOptions _options;
    private readonly StockMonitorService _service;
    private readonly Mock<IStockService> _mockStockService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<IMarketStatusService> _mockMarketStatusService;
    private readonly Mock<ILogger<StockMonitorService>> _mockLogger;

    public StockMonitorServiceTests()
    {
        _mockStockService = new Mock<IStockService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockMarketStatusService = new Mock<IMarketStatusService>();
        _mockLogger = new Mock<ILogger<StockMonitorService>>();
        // Default behavior: Market is OPEN
        _mockMarketStatusService.Setup(m => m.IsMarketOpen()).Returns(true);
        _options = new MonitorOptions("PETR4", SellPrice: 30.00m, BuyPrice: 20.00m);
        // Simulating a list with ONE channel for testing efficiency
        var channels = new List<INotificationService> { _mockNotificationService.Object };
        _service = new StockMonitorService(
            _mockStockService.Object,
            channels,
            _mockMarketStatusService.Object,
            _mockLogger.Object
        );
    }


    [Fact]
    public async Task CheckAndNotify_ShouldSendSellNotification_WhenPriceIsAboveSellPrice()
    {
        // Arrange
        decimal currentPrice = 35.00m; // Acima de 30.00 (Venda)
        _mockStockService.Setup(s => s.GetPriceAsync(_options.Symbol))
            .ReturnsAsync(currentPrice);
        // Act
        await _service.CheckAndNotifyAsync(_options);
        // Assert
        _mockNotificationService.Verify(e => e.SendNotificationAsync(
            It.Is<string>(s => s.Contains("VENDA")),
            It.Is<string>(b => b.Contains("35.00"))),
            Times.Once);
    }

    [Fact]
    public async Task CheckAndNotify_ShouldSendBuyNotification_WhenPriceIsBelowBuyPrice()
    {
        // Arrange
        decimal currentPrice = 15.00m; // Abaixo de 20.00 (Compra)
        _mockStockService.Setup(s => s.GetPriceAsync(_options.Symbol))
            .ReturnsAsync(currentPrice);
        // Act
        await _service.CheckAndNotifyAsync(_options);
        // Assert
        _mockNotificationService.Verify(e => e.SendNotificationAsync(
            It.Is<string>(s => s.Contains("COMPRA")),
            It.Is<string>(b => b.Contains("15.00"))),
            Times.Once);
    }

    [Fact]
    public async Task CheckAndNotify_ShouldNotSendNotification_WhenPriceIsWithinNeutralRange()
    {
        // Arrange
        decimal currentPrice = 25.00m; // Entre 20 e 30 (Neutro)
        _mockStockService.Setup(s => s.GetPriceAsync(_options.Symbol))
            .ReturnsAsync(currentPrice);
        // Act
        await _service.CheckAndNotifyAsync(_options);
        // Assert
        _mockNotificationService.Verify(e => e.SendNotificationAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CheckAndNotify_ShouldHandleNullPrice_Gracefully()
    {
        // Arrange
        _mockStockService.Setup(s => s.GetPriceAsync(_options.Symbol))
            .ReturnsAsync((decimal?)null);
        // Act
        await _service.CheckAndNotifyAsync(_options);
        // Assert
        _mockNotificationService.Verify(e => e.SendNotificationAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CheckAndNotify_ShouldThrow_WhenServiceFails()
    {
        // Arrange
        _mockStockService.Setup(s => s.GetPriceAsync(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("API Down"));
        // Act
        Func<Task> act = async () => await _service.CheckAndNotifyAsync(_options);
        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task CheckAndNotify_ShouldSkip_WhenMarketIsClosed()
    {
        // Arrange
        _mockMarketStatusService.Setup(m => m.IsMarketOpen()).Returns(false); // Closed
        // Act
        await _service.CheckAndNotifyAsync(_options);
        // Assert
        _mockStockService.Verify(s => s.GetPriceAsync(It.IsAny<string>()), Times.Never);
        _mockNotificationService.Verify(e => e.SendNotificationAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
