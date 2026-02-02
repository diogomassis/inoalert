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
    private readonly Mock<INotificationStateManager> _mockStateManager;
    private readonly Mock<ILogger<StockMonitorService>> _mockLogger;

    public StockMonitorServiceTests()
    {
        _mockStockService = new Mock<IStockService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockMarketStatusService = new Mock<IMarketStatusService>();
        _mockStateManager = new Mock<INotificationStateManager>();
        _mockLogger = new Mock<ILogger<StockMonitorService>>();
        // Default behavior: Market is OPEN
        _mockMarketStatusService.Setup(m => m.IsMarketOpen()).Returns(true);
        // Default behavior: Always notify
        _mockStateManager.Setup(m => m.ShouldNotify(It.IsAny<string>(), It.IsAny<decimal>())).Returns(true);
        _options = new MonitorOptions("PETR4", SellPrice: 30.00m, BuyPrice: 20.00m);
        var channels = new List<INotificationService> { _mockNotificationService.Object };
        _service = new StockMonitorService(
            _mockStockService.Object,
            channels,
            _mockMarketStatusService.Object,
            _mockStateManager.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task CheckAndNotify_ShouldSendSellNotification_WhenPriceIsAboveSellPrice()
    {
        // Arrange
        decimal currentPrice = 35.00m;
        _mockStockService.Setup(s => s.GetPriceAsync(_options.Symbol))
            .ReturnsAsync(currentPrice);
        // Act
        await _service.CheckAndNotifyAsync(_options);
        // Assert
        _mockNotificationService.Verify(e => e.SendNotificationAsync(
            It.Is<string>(s => s.Contains("VENDA")),
            It.Is<string>(b => b.Contains("35.00"))),
            Times.Once);
        _mockStateManager.Verify(m => m.UpdateState(_options.Symbol, currentPrice), Times.Once);
    }

    [Fact]
    public async Task CheckAndNotify_ShouldSendBuyNotification_WhenPriceIsBelowBuyPrice()
    {
        // Arrange
        decimal currentPrice = 15.00m;
        _mockStockService.Setup(s => s.GetPriceAsync(_options.Symbol))
            .ReturnsAsync(currentPrice);
        // Act
        await _service.CheckAndNotifyAsync(_options);
        // Assert
        _mockNotificationService.Verify(e => e.SendNotificationAsync(
            It.Is<string>(s => s.Contains("COMPRA")),
            It.Is<string>(b => b.Contains("15.00"))),
            Times.Once);
        _mockStateManager.Verify(m => m.UpdateState(_options.Symbol, currentPrice), Times.Once);
    }

    [Fact]
    public async Task CheckAndNotify_ShouldNotSendNotification_WhenPriceIsWithinNeutralRange()
    {
        // Arrange
        decimal currentPrice = 25.00m;
        _mockStockService.Setup(s => s.GetPriceAsync(_options.Symbol))
            .ReturnsAsync(currentPrice);
        // Act
        await _service.CheckAndNotifyAsync(_options);
        // Assert
        _mockNotificationService.Verify(e => e.SendNotificationAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mockStateManager.Verify(m => m.UpdateState(It.IsAny<string>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public async Task CheckAndNotify_ShouldNotNotify_WhenStateManagerReturnsFalse()
    {
        // Arrange
        decimal currentPrice = 35.00m;
        _mockStockService.Setup(s => s.GetPriceAsync(_options.Symbol)).ReturnsAsync(currentPrice);
        _mockStateManager.Setup(m => m.ShouldNotify(_options.Symbol, currentPrice)).Returns(false); // BLOQUEADO
        // Act
        await _service.CheckAndNotifyAsync(_options);
        // Assert
        _mockNotificationService.Verify(e => e.SendNotificationAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mockStateManager.Verify(m => m.UpdateState(It.IsAny<string>(), It.IsAny<decimal>()), Times.Never);
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
