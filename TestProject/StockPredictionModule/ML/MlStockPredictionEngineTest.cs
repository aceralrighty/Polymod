using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Scaffolding.Shared;
using Moq;
using NUnit.Framework;
using TBD.MetricsModule.Services.Interfaces;
using TBD.StockPredictionModule.ML;
using TBD.StockPredictionModule.Models;

namespace TestProject.StockPredictionModule.ML;

[TestFixture]
[TestOf(typeof(MlStockPredictionEngine))]
public class MlStockPredictionEngineTest
{
    private Mock<IMetricsServiceFactory> _mockMetricsServiceFactory;
    private Mock<IMetricsService> _mockMetricsService;
    private MlStockPredictionEngine _mlStockPredictionEngine;

    [SetUp]
    public void SetUp()
    {
        _mockMetricsServiceFactory = new Mock<IMetricsServiceFactory>();
        _mockMetricsService = new Mock<IMetricsService>();

        _mockMetricsServiceFactory
            .Setup(f => f.CreateMetricsService(It.IsAny<string>()))
            .Returns(_mockMetricsService.Object);

        _mlStockPredictionEngine = new MlStockPredictionEngine(_mockMetricsServiceFactory.Object);
    }

    [Test]
    public async Task IsModelTrainedAsync_ModelNotTrained_ReturnsFalse()
    {
        // Act
        var isTrained = await _mlStockPredictionEngine.IsModelTrainedAsync();

        // Assert
        isTrained.Should().BeFalse();
    }

    [Test]
    public async Task IsModelTrainedAsync_ModelTrained_ReturnsTrue()
    {
        // Arrange - Provide sufficient data for feature engineering (minimum 15 records)
        var rawData = new List<RawData>();
        var baseDate = new DateTime(2025, 8, 1);
        const float basePrice = 150.0f;

        // Generate 15 days of realistic stock data with proper progression
        for (var i = 0; i < 15; i++)
        {
            var date = baseDate.AddDays(i);
            var priceVariation = (float)(new Random(i).NextDouble() * 10 - 5); // -5 to +5 variation
            var dailyPrice = basePrice + i + priceVariation;
            var high = dailyPrice + (float)(new Random(i + 100).NextDouble() * 3);
            var low = dailyPrice - (float)(new Random(i + 200).NextDouble() * 3);

            rawData.Add(new RawData
            {
                Symbol = "AAPL",
                Open = dailyPrice,
                High = Math.Max(high, dailyPrice),
                Low = Math.Min(low, dailyPrice),
                Close = dailyPrice,
                Volume = 1000 + (i * 100), // Varying volume
                Date = date.ToString("yyyy-MM-dd")
            });
        }

        // Act
        await _mlStockPredictionEngine.TrainModelAsync(rawData);
        var isTrained = await _mlStockPredictionEngine.IsModelTrainedAsync();

        // Assert
        isTrained.Should().BeTrue();
    }

    [Test]
    public void TrainModelAsync_InvalidData_ThrowsInvalidOperationException()
    {
        // Arrange
        var rawData = new List<RawData>();

        // Act
        var act = () => _mlStockPredictionEngine.TrainModelAsync(rawData);

        // Assert
        act.Should().NotBeNull();
        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No training data provided");
    }

    [Test]
    public async Task TrainModelAsync_ValidData_TrainsModel()
    {
        // Arrange - Provide sufficient data for feature engineering (minimum 15 records)
        var rawData = new List<RawData>();
        var baseDate = new DateTime(2025, 8, 1);
        const float basePrice = 150.0f;

        // Generate 15 days of realistic stock data with proper progression
        for (var i = 0; i < 15; i++)
        {
            var date = baseDate.AddDays(i);
            var priceVariation = (float)(new Random(i).NextDouble() * 10 - 5); // -5 to +5 variation
            var dailyPrice = basePrice + i + priceVariation;
            var high = dailyPrice + (float)(new Random(i + 100).NextDouble() * 3);
            var low = dailyPrice - (float)(new Random(i + 200).NextDouble() * 3);

            rawData.Add(new RawData
            {
                Symbol = "AAPL",
                Open = dailyPrice,
                High = Math.Max(high, dailyPrice),
                Low = Math.Min(low, dailyPrice),
                Close = dailyPrice,
                Volume = 1000 + (i * 100), // Varying volume
                Date = date.ToString("yyyy-MM-dd")
            });
        }

        // Act
        await _mlStockPredictionEngine.TrainModelAsync(rawData);

        // Assert
        var isTrained = await _mlStockPredictionEngine.IsModelTrainedAsync();
        isTrained.Should().BeTrue();
    }

    [Test]
    public void GeneratePredictAsync_GroupedData_NoDataForSymbol_ThrowsArgumentException()
    {
        // Arrange
        var groupedData = new Dictionary<string, List<RawData>>
        {
            { "MSFT", [new RawData { Symbol = "MSFT", Close = 300 }] }
        };

        // Act
        Func<Task> act = () => _mlStockPredictionEngine.GeneratePredictAsync(groupedData, "AAPL");

        // Assert
        act.Should().NotBeNull();
        act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("No data found for symbol: AAPL");
    }

    [Test]
    public async Task GeneratePredictAsync_GroupedData_ValidData_ReturnsPrediction()
    {
        // Arrange - Provide MORE data for robust feature engineering (minimum 20 records)
        var groupedData = new Dictionary<string, List<RawData>>
        {
            { "AAPL", GenerateTestData("AAPL", 20, new DateTime(2025, 8, 1), 150.0f) }
        };

        await _mlStockPredictionEngine.TrainModelAsync(groupedData["AAPL"]);

        // Act
        var prediction = await _mlStockPredictionEngine.GeneratePredictAsync(groupedData, "AAPL");

        // Assert
        prediction.Should().NotBeNull();
        prediction.Symbol.Should().Be("AAPL");
        prediction.PredictedPrice.Should().BeGreaterThan(0);
    }

    // Helper method to generate consistent test data
    private List<RawData> GenerateTestData(string symbol, int days, DateTime startDate, float basePrice)
    {
        var data = new List<RawData>();
        var random = new Random(42); // Fixed seed for reproducible tests

        for (var i = 0; i < days; i++)
        {
            var date = startDate.AddDays(i);
            var priceVariation = (float)(random.NextDouble() * 4 - 2); // -2 to +2 variation
            var dailyPrice = basePrice + (i * 0.5f) + priceVariation; // Gradual upward trend

            var highVariation = (float)(random.NextDouble() * 2); // 0 to 2
            var lowVariation = (float)(random.NextDouble() * 2); // 0 to 2

            var high = dailyPrice + highVariation;
            var low = dailyPrice - lowVariation;
            var volume = 1000 + (int)(random.NextDouble() * 500); // 1000-1500 volume

            data.Add(new RawData
            {
                Symbol = symbol,
                Open = dailyPrice,
                High = high,
                Low = low,
                Close = dailyPrice,
                Volume = volume,
                Date = date.ToString("yyyy-MM-dd")
            });
        }

        return data;
    }

    [Test]
    public void CleanTrainingData_ValidAndInvalidData_CleansDataCorrectly()
    {
        // Arrange
        var rawData = new List<RawData>
        {
            new()
            {
                Symbol = "AAPL",
                Close = 150,
                High = 155,
                Low = 145,
                Volume = 1000,
                Date = "2025-08-01"
            },
            new()
            {
                Symbol = "",
                Close = 0,
                High = 0,
                Low = 0,
                Volume = 0,
                Date = "InvalidDate"
            },
            new()
            {
                Symbol = "MSFT",
                Close = 300,
                High = 305,
                Low = 295,
                Volume = 2000,
                Date = "2025-08-02"
            }
        };

        // Act
        var cleanedData = MlStockPredictionEngine.CleanTrainingData(rawData);

        // Assert
        cleanedData.Should().HaveCount(2);
        cleanedData.All(d => !string.IsNullOrEmpty(d.Symbol) &&
                             d is { Close: > 0, High: > 0, Low: > 0 } &&
                             d.Volume > 0).Should().BeTrue();
    }
}
