using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StockQuoteAlert.Models;
using StockQuoteAlert.Services.Market;
using Xunit;

namespace StockQuoteAlert.Tests;

public class MarketStatusServiceTests
{
    private readonly Mock<TimeProvider> _mockTimeProvider;
    private readonly Mock<ILogger<MarketStatusService>> _mockLogger;
    private readonly Mock<IOptions<AppSettings>> _mockOptions;
    private readonly AppSettings _appSettings;
    private readonly MarketStatusService _service;

    public MarketStatusServiceTests()
    {
        _mockTimeProvider = new Mock<TimeProvider>();
        _mockLogger = new Mock<ILogger<MarketStatusService>>();
        _mockOptions = new Mock<IOptions<AppSettings>>();
        _appSettings = new AppSettings { IgnoreMarketHours = false };
        _mockOptions.Setup(o => o.Value).Returns(_appSettings);
        _service = new MarketStatusService(
            _mockTimeProvider.Object,
            _mockLogger.Object,
            _mockOptions.Object);
    }

    private void SetupTime(int year, int month, int day, int hour, int minute)
    {
        var utcTime = new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero);
        _mockTimeProvider.Setup(x => x.GetUtcNow()).Returns(utcTime);
        _mockTimeProvider.Setup(x => x.LocalTimeZone).Returns(TimeZoneInfo.Utc);
    }

    [Fact]
    public void IsMarketOpen_ShouldReturnFalse_OnSaturday()
    {
        SetupTime(2023, 10, 21, 14, 0);
        _service.IsMarketOpen().Should().BeFalse();
    }

    [Fact]
    public void IsMarketOpen_ShouldReturnFalse_OnSunday()
    {
        SetupTime(2023, 10, 22, 14, 0);
        _service.IsMarketOpen().Should().BeFalse();
    }

    [Fact]
    public void IsMarketOpen_ShouldReturnTrue_OnWeekday_CreationHours()
    {
        SetupTime(2023, 10, 24, 13, 0);
        _service.IsMarketOpen().Should().BeTrue();
    }

    [Fact]
    public void IsMarketOpen_ShouldReturnTrue_OnWeekday_MiddleOfDay()
    {
        SetupTime(2023, 10, 24, 18, 0);
        _service.IsMarketOpen().Should().BeTrue();
    }

    [Fact]
    public void IsMarketOpen_ShouldReturnFalse_OnWeekday_TooEarly()
    {
        SetupTime(2023, 10, 24, 12, 59);
        _service.IsMarketOpen().Should().BeFalse();
    }

    [Fact]
    public void IsMarketOpen_ShouldReturnFalse_OnWeekday_TooLate()
    {
        SetupTime(2023, 10, 24, 20, 31);
        _service.IsMarketOpen().Should().BeFalse();
    }

    [Fact]
    public void IsMarketOpen_ShouldReturnTrue_WhenIgnoreMarketHoursIsTrue()
    {
        SetupTime(2023, 10, 21, 14, 0);
        _appSettings.IgnoreMarketHours = true;
        _service.IsMarketOpen().Should().BeTrue();
    }
}
