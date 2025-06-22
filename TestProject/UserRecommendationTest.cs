using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using TBD.RecommendationModule.Models.Recommendations;
using TBD.ServiceModule.Models;
using TBD.UserModule.Models;

namespace TestProject.RecommendationModule.Models;

[TestFixture]
[TestOf(typeof(UserRecommendation))]
public class UserRecommendationTest
{
    [Test]
    public void UserRecommendation_Should_Have_Default_Values()
    {
        // Act
        var recommendation = new UserRecommendation();

        // Assert
        recommendation.UserId.Should().Be(Guid.Empty);
        recommendation.ServiceId.Should().Be(Guid.Empty);
        recommendation.Rating.Should().Be(0);
        recommendation.User.Should().BeNull();
        recommendation.Service.Should().BeNull();
        recommendation.RecommendedAt.Should().Be(default);
        recommendation.ClickCount.Should().Be(0);
    }

    [Test]
    public void UserRecommendation_Should_StoreAssigned_Values()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        const float rating = 4.5f;
        var recommendedAt = DateTime.UtcNow;
        const int clickCount = 7;

        var userMock = new Mock<User>();
        var serviceMock = new Mock<Service>();

        // Act
        var recommendation = new UserRecommendation
        {
            UserId = userId,
            ServiceId = serviceId,
            Rating = rating,
            RecommendedAt = recommendedAt,
            ClickCount = clickCount,
            User = userMock.Object,
            Service = serviceMock.Object
        };

        // Assert
        recommendation.UserId.Should().Be(userId);
        recommendation.ServiceId.Should().Be(serviceId);
        recommendation.Rating.Should().Be(rating);
        recommendation.RecommendedAt.Should().BeCloseTo(recommendedAt, TimeSpan.FromSeconds(1));
        recommendation.ClickCount.Should().Be(clickCount);
        recommendation.User.Should().Be(userMock.Object);
        recommendation.Service.Should().Be(serviceMock.Object);
    }

    [Test]
    public void UserRecommendation_Should_ValidateRequired_Properties()
    {
        // Arrange
        var recommendation = new UserRecommendation();

        // Act
        var validationContext = new ValidationContext(recommendation);
        var validationResults = new List<ValidationResult>();

        // Assert
        Validator.TryValidateObject(recommendation, validationContext, validationResults, true).Should().BeFalse();
        validationResults.Should().Contain(result => result.MemberNames.Contains(nameof(UserRecommendation.UserId)));
        validationResults.Should().Contain(result => result.MemberNames.Contains(nameof(UserRecommendation.ServiceId)));
    }



    [Test]
    public void UserRecommendation_Should_Be_Valid_When_Required_Properties_Are_Set()
    {
        // Arrange
        var recommendation = new UserRecommendation { UserId = Guid.NewGuid(), ServiceId = Guid.NewGuid() };

        // Act
        var validationContext = new ValidationContext(recommendation);
        var validationResults = new List<ValidationResult>();

        // Assert
        Validator.TryValidateObject(recommendation, validationContext, validationResults, true).Should().BeTrue();
    }

    [Test]
    public void UserRecommendation_ClickCount_Should_Default_ToZero()
    {
        // Assert
        var recommendation = new UserRecommendation();
        recommendation.ClickCount.Should().Be(0);
    }

    [Test]
    public void UserRecommendation_RecommendedAt_Should_Store_DateTimeProperly()
    {
        // Arrange
        var currentDateTime = DateTime.UtcNow;

        // Act
        var recommendation = new UserRecommendation { RecommendedAt = currentDateTime };

        // Assert
        recommendation.RecommendedAt.Should().BeCloseTo(currentDateTime, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void UserRecommendation_Rating_Should_Accept_FloatValues()
    {
        // Arrange
        const float rating = 4.5f;

        // Act
        var recommendation = new UserRecommendation { Rating = rating };

        // Assert
        recommendation.Rating.Should().Be(rating);
    }

    [Test]
    public void UserRecommendation_UserAndService_Should_Accept_Null_Values()
    {
        // Act
        var recommendation = new UserRecommendation { User = null, Service = null };

        // Assert
        recommendation.User.Should().BeNull();
        recommendation.Service.Should().BeNull();
    }

    [Test]
    public void UserRecommendation_Should_Allow_ClickCount_Modification()
    {
        // Arrange
        var recommendation = new UserRecommendation {
            // Act
            ClickCount = 10 };

        // Assert
        recommendation.ClickCount.Should().Be(10);
    }
}
