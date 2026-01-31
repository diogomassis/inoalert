using System;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using StockQuoteAlert.Services;
using Xunit;

namespace StockQuoteAlert.Tests;

public class MarketStatusServiceTests
{
    private readonly Mock<TimeProvider> _mockTimeProvider;
    private readonly Mock<ILogger<MarketStatusService>> _mockLogger;
    private readonly MarketStatusService _service;

    public MarketStatusServiceTests()
    {
        _mockTimeProvider = new Mock<TimeProvider>();
        _mockLogger = new Mock<ILogger<MarketStatusService>>();
        _service = new MarketStatusService(_mockTimeProvider.Object, _mockLogger.Object);
    }

    private void SetupTime(int year, int month, int day, int hour, int minute)
    {
        // Setup UTC time. BRT is UTC-3.
        // So if we want 10:00 BRT, we set 13:00 UTC.
        var utcTime = new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero);
        _mockTimeProvider.Setup(x => x.GetUtcNow()).Returns(utcTime);
        _mockTimeProvider.Setup(x => x.LocalTimeZone).Returns(TimeZoneInfo.Utc);
    }

    [Fact]
    public void IsMarketOpen_ShouldReturnFalse_OnSaturday()
    {
        // Arrange: Saturday 14:00 UTC (11:00 BRT) - Weekend
        SetupTime(2023, 10, 21, 14, 0); // Oct 21 2023 is Saturday
        // Act & Assert
        _service.IsMarketOpen().Should().BeFalse();
    }

    [Fact]
    public void IsMarketOpen_ShouldReturnFalse_OnSunday()
    {
        // Arrange: Sunday
        SetupTime(2023, 10, 22, 14, 0);
        // Act & Assert
        _service.IsMarketOpen().Should().BeFalse();
    }

    [Fact]
    public void IsMarketOpen_ShouldReturnTrue_OnWeekday_CreationHours()
    {
        // Arrange: Tuesday 10:00 BRT (13:00 UTC) - MARKET OPEN
        SetupTime(2023, 10, 24, 13, 0); // Oct 24 2023 is Tuesday
        // Act & Assert
        _service.IsMarketOpen().Should().BeTrue();
    }

    [Fact]
    public void IsMarketOpen_ShouldReturnTrue_OnWeekday_MiddleOfDay()
    {
        // Arrange: Tuesday 15:00 BRT (18:00 UTC) - OPEN
        SetupTime(2023, 10, 24, 18, 0);
        // Act & Assert
        _service.IsMarketOpen().Should().BeTrue();
    }

    [Fact]
    public void IsMarketOpen_ShouldReturnFalse_OnWeekday_TooEarly()
    {
        // Arrange: Tuesday 09:59 BRT (12:59 UTC) - CLOSED
        SetupTime(2023, 10, 24, 12, 59);
        // Act & Assert
        _service.IsMarketOpen().Should().BeFalse();
    }

    [Fact]
    public void IsMarketOpen_ShouldReturnFalse_OnWeekday_TooLate()
    {
        // Arrange: Tuesday 17:31 BRT (20:31 UTC) - CLOSED
        SetupTime(2023, 10, 24, 20, 31);
        // Act & Assert
        _service.IsMarketOpen().Should().BeFalse();
    }
}
