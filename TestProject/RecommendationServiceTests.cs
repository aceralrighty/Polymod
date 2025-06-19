using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using TBD.MetricsModule.Services;
using TBD.RecommendationModule.ML.Interface;
using TBD.RecommendationModule.Models;
using TBD.RecommendationModule.Models.Recommendations;
using TBD.RecommendationModule.Repositories.Interfaces;
using TBD.RecommendationModule.Services;
using TBD.ServiceModule.Models;
using TBD.ServiceModule.Repositories;

namespace TestProject;

[TestFixture]
public class RecommendationServiceTests
{
    private Mock<IRecommendationRepository> _mockRecommendationRepository;
    private Mock<IMetricsServiceFactory> _mockMetricsServiceFactory;
    private Mock<IServiceRepository> _mockServiceRepository;
    private Mock<IMlRecommendationEngine> _mockMlEngine;
    private Mock<IMetricsService> _mockMetricsService;
    private RecommendationService _service;

    [SetUp]
    public void SetUp()
    {
        _mockRecommendationRepository = new Mock<IRecommendationRepository>();
        _mockMetricsServiceFactory = new Mock<IMetricsServiceFactory>();
        _mockServiceRepository = new Mock<IServiceRepository>();
        _mockMlEngine = new Mock<IMlRecommendationEngine>();
        _mockMetricsService = new Mock<IMetricsService>();

        _mockMetricsServiceFactory
            .Setup(x => x.CreateMetricsService("Recommendation"))
            .Returns(_mockMetricsService.Object);

        // Fixed constructor call - match the primary constructor parameter order
        _service = new RecommendationService(
            _mockRecommendationRepository.Object,    // IRecommendationRepository recommendationRepository
            _mockMetricsServiceFactory.Object,       // IMetricsServiceFactory serviceFactory
            _mockServiceRepository.Object,           // IServiceRepository service
            _mockMlEngine.Object);                   // IMlRecommendationEngine mlEngine
    }

    #region GetRecommendationsForUserAsync Tests

    [Test]
    public async Task GetRecommendationsForUserAsync_ShouldReturnServices_WhenRecommendationsExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var serviceId1 = Guid.NewGuid();
        var serviceId2 = Guid.NewGuid();

        var recommendations = new List<UserRecommendation>
        {
            new() { UserId = userId, ServiceId = serviceId1 },
            new() { UserId = userId, ServiceId = serviceId2 },
            new() { UserId = userId, ServiceId = serviceId1 } // Duplicate - should be filtered
        };

        var services = new List<Service>
        {
            new() { Id = serviceId1, Title = "Service 1" },
            new() { Id = serviceId2, Title = "Service 2" }
        };

        _mockRecommendationRepository
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(recommendations);

        _mockServiceRepository
            .Setup(x => x.GetByIdsAsync(It.Is<IEnumerable<Guid>>(ids =>
                ids.Count() == 2 && ids.Contains(serviceId1) && ids.Contains(serviceId2))))
            .ReturnsAsync(services);

        // Act
        var result = await _service.GetRecommendationsForUserAsync(userId);

        // Assert
        var enumerable = result as Service[] ?? result.ToArray();
        enumerable.Should().HaveCount(2);
        enumerable.Should().Contain(s => s.Id == serviceId1);
        enumerable.Should().Contain(s => s.Id == serviceId2);

        _mockMetricsService.Verify(x => x.IncrementCounter("rec.get_recommendations_for_user."), Times.Once);
    }

    [Test]
    public async Task GetRecommendationsForUserAsync_ShouldReturnEmpty_WhenNoRecommendations()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockRecommendationRepository
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<UserRecommendation>());

        _mockServiceRepository
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Service>());

        // Act
        var result = await _service.GetRecommendationsForUserAsync(userId);

        // Assert
        result.Should().BeEmpty();
        _mockMetricsService.Verify(x => x.IncrementCounter("rec.get_recommendations_for_user."), Times.Once);
    }

    #endregion

    #region RecordRecommendationAsync Tests

    [Test]
    public async Task RecordRecommendationAsync_ShouldAddRecommendation_WhenNotExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        _mockRecommendationRepository
            .Setup(x => x.GetLatestByUserAndServiceAsync(userId, serviceId))
            .ReturnsAsync((UserRecommendation?)null);

        // Act
        await _service.RecordRecommendationAsync(userId, serviceId);

        // Assert
        _mockRecommendationRepository.Verify(x => x.AddAsync(It.Is<UserRecommendation>(r =>
            r.UserId == userId &&
            r.ServiceId == serviceId &&
            r.RecommendedAt != default)), Times.Once);

        _mockRecommendationRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        _mockMetricsService.Verify(x => x.IncrementCounter("rec.record_recommendation."), Times.Once);
    }

    [Test]
    public async Task RecordRecommendationAsync_ShouldNotAddRecommendation_WhenAlreadyExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var existingRecommendation = new UserRecommendation
        {
            UserId = userId,
            ServiceId = serviceId
        };

        _mockRecommendationRepository
            .Setup(x => x.GetLatestByUserAndServiceAsync(userId, serviceId))
            .ReturnsAsync(existingRecommendation);

        // Act
        await _service.RecordRecommendationAsync(userId, serviceId);

        // Assert
        _mockRecommendationRepository.Verify(x => x.AddAsync(It.IsAny<UserRecommendation>()), Times.Never);
        _mockRecommendationRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
        _mockMetricsService.Verify(x => x.IncrementCounter(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region IncrementClickAsync Tests

    [Test]
    public async Task IncrementClickAsync_ShouldIncrementClick_WhenRecommendationExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var recommendation = new UserRecommendation
        {
            UserId = userId,
            ServiceId = serviceId,
            ClickCount = 5
        };

        _mockRecommendationRepository
            .Setup(x => x.GetLatestByUserAndServiceAsync(userId, serviceId))
            .ReturnsAsync(recommendation);

        // Act
        await _service.IncrementClickAsync(userId, serviceId);

        // Assert
        recommendation.ClickCount.Should().Be(6);
        _mockRecommendationRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        _mockMetricsService.Verify(x => x.IncrementCounter("rec.increment_click."), Times.Once);
    }

    [Test]
    public async Task IncrementClickAsync_ShouldDoNothing_WhenRecommendationNotExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        _mockRecommendationRepository
            .Setup(x => x.GetLatestByUserAndServiceAsync(userId, serviceId))
            .ReturnsAsync((UserRecommendation?)null);

        // Act
        await _service.IncrementClickAsync(userId, serviceId);

        // Assert
        _mockRecommendationRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
        _mockMetricsService.Verify(x => x.IncrementCounter(It.IsAny<string>()), Times.Never);
    }

    #endregion


    #region GetMlRecommendationsAsync Tests

    [Test]
    public async Task GetMlRecommendationsAsync_ShouldReturnServices_WithDefaultCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var serviceIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var services = serviceIds.Select(id => new Service { Id = id }).ToList();

        _mockMlEngine
            .Setup(x => x.GenerateRecommendationsAsync(userId, 10))
            .ReturnsAsync(serviceIds);

        _mockServiceRepository
            .Setup(x => x.GetByIdsAsync(serviceIds))
            .ReturnsAsync(services);

        // Act
        var result = await _service.GetMlRecommendationsAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        _mockMetricsService.Verify(x => x.IncrementCounter("rec.get_ml_recommendations"), Times.Once);
    }

    [Test]
    public async Task GetMlRecommendationsAsync_ShouldReturnServices_WithCustomCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var count = 5;
        var serviceIds = new[] { Guid.NewGuid() };
        var services = serviceIds.Select(id => new Service { Id = id }).ToList();

        _mockMlEngine
            .Setup(x => x.GenerateRecommendationsAsync(userId, count))
            .ReturnsAsync(serviceIds);

        _mockServiceRepository
            .Setup(x => x.GetByIdsAsync(serviceIds))
            .ReturnsAsync(services);

        // Act
        var result = await _service.GetMlRecommendationsAsync(userId, count);

        // Assert
        result.Should().HaveCount(1);
        _mockMlEngine.Verify(x => x.GenerateRecommendationsAsync(userId, count), Times.Once);
    }

    #endregion

    #region RateServiceAsync Tests

    [Test]
    public async Task RateServiceAsync_ShouldAddRating_WhenValidRating()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var rating = 4.5f;

        // Act
        await _service.RateServiceAsync(userId, serviceId, rating);

        // Assert
        _mockRecommendationRepository.Verify(x => x.AddRatingAsync(userId, serviceId, rating), Times.Once);
        _mockMetricsService.Verify(x => x.IncrementCounter("rec.rate_service"), Times.Once);
    }

    [Test]
    [TestCase(0.5f)]
    [TestCase(5.5f)]
    [TestCase(-1f)]
    [TestCase(6f)]
    public Task RateServiceAsync_ShouldThrowException_WhenInvalidRating(float rating)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RateServiceAsync(userId, serviceId, rating));

        exception?.Message.Should().Be("Rating must be between 1 and 5");
        _mockRecommendationRepository.Verify(
            x => x.AddRatingAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<float>()), Times.Never);
        _mockMetricsService.Verify(x => x.IncrementCounter(It.IsAny<string>()), Times.Never);
        return Task.CompletedTask;
    }

    [Test]
    [TestCase(1f)]
    [TestCase(5f)]
    [TestCase(3.0f)]
    public async Task RateServiceAsync_ShouldAcceptValidBoundaryRatings(float rating)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        // Act
        await _service.RateServiceAsync(userId, serviceId, rating);

        // Assert
        _mockRecommendationRepository.Verify(x => x.AddRatingAsync(userId, serviceId, rating), Times.Once);
        _mockMetricsService.Verify(x => x.IncrementCounter("rec.rate_service"), Times.Once);
    }

    #endregion

    #region PredictRatingAsync Tests

    [Test]
    public async Task PredictRatingAsync_ShouldReturnPredictedRating()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var expectedRating = 4.2f;

        _mockMlEngine
            .Setup(x => x.PredictRatingAsync(userId, serviceId))
            .ReturnsAsync(expectedRating);

        // Act
        var result = await _service.PredictRatingAsync(userId, serviceId);

        // Assert
        result.Should().Be(expectedRating);
        _mockMetricsService.Verify(x => x.IncrementCounter("rec.predict_rating"), Times.Once);
    }

    #endregion

    #region TrainRecommendationModelAsync Tests

    [Test]
    public async Task TrainRecommendationModelAsync_ShouldCallMlEngine()
    {
        // Act
        await _service.TrainRecommendationModelAsync();

        // Assert
        _mockMlEngine.Verify(x => x.TrainModelAsync(), Times.Once);
        _mockMetricsService.Verify(x => x.IncrementCounter("rec.train_model"), Times.Once);
    }

    #endregion

    #region Integration-Style Tests

    [Test]
    public async Task RecordAndIncrementClick_ShouldWork_InSequence()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        _mockRecommendationRepository
            .Setup(x => x.GetLatestByUserAndServiceAsync(userId, serviceId))
            .ReturnsAsync((UserRecommendation)null!);

        var addedRecommendation = new UserRecommendation { UserId = userId, ServiceId = serviceId, ClickCount = 0 };

        _mockRecommendationRepository
            .Setup(x => x.AddAsync(It.IsAny<UserRecommendation>()))
            .Callback<UserRecommendation>(_ =>
            {
                // Simulate that after adding, we can retrieve it
                _mockRecommendationRepository
                    .Setup(x => x.GetLatestByUserAndServiceAsync(userId, serviceId))
                    .ReturnsAsync(addedRecommendation);
            });

        // Act
        await _service.RecordRecommendationAsync(userId, serviceId);
        await _service.IncrementClickAsync(userId, serviceId);

        // Assert
        _mockRecommendationRepository.Verify(x => x.AddAsync(It.IsAny<UserRecommendation>()), Times.Once);
        _mockRecommendationRepository.Verify(x => x.SaveChangesAsync(), Times.Exactly(2));
        addedRecommendation.ClickCount.Should().Be(1);
    }

    #endregion
}
