using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using TBD.Shared.CachingConfiguration;
using TBD.Shared.Repositories;

namespace TBD.TestProject;

[TestFixture]
public class CachingRepositoryDecoratorTests
    : IDisposable
{
    private Mock<IGenericRepository<TestEntity>> _mockInnerRepository;
    private IMemoryCache _memoryCache;
    private Mock<ILogger<CachingRepositoryDecorator<TestEntity>>> _mockLogger;
    private CachingRepositoryDecorator<TestEntity> _cachingRepository;
    private CacheOptions _cachingOptions;

    [SetUp]
    public void SetUp()
    {
        _mockInnerRepository = new Mock<IGenericRepository<TestEntity>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = new Mock<ILogger<CachingRepositoryDecorator<TestEntity>>>();

        _cachingOptions = new CacheOptions
        {
            DefaultCacheDuration = TimeSpan.FromMinutes(5),
            GetByIdCacheDuration = TimeSpan.FromMinutes(10),
            GetAllCacheDuration = TimeSpan.FromMinutes(2),
            EnableCaching = true,
            CacheKeyPrefix = "Test"
        };

        var mockOptions = new Mock<IOptions<CacheOptions>>();
        mockOptions.Setup(x => x.Value).Returns(_cachingOptions);

        _cachingRepository = new CachingRepositoryDecorator<TestEntity>(
            _mockInnerRepository.Object,
            _memoryCache,
            mockOptions.Object,
            _mockLogger.Object);
    }

    [Test]
    public async Task GetByIdAsync_FirstCall_CallsInnerRepositoryAndCachesResult()
    {
        var entityId = Guid.NewGuid();
        var expectedEntity = new TestEntity { Id = entityId, Name = "Test Entity" };

        _mockInnerRepository.Setup(x => x.GetByIdAsync(entityId)).ReturnsAsync(expectedEntity);

        var result = await _cachingRepository.GetByIdAsync(entityId);

        var cacheKey = $"Test_TestEntity_Id_{entityId}";
        var isCached = _memoryCache.TryGetValue(cacheKey, out TestEntity? cachedEntity);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            result.Should().BeEquivalentTo(expectedEntity);
            _mockInnerRepository.Verify(x => x.GetByIdAsync(entityId), Times.Once);
            Assert.That(isCached, Is.True);
            cachedEntity.Should().BeEquivalentTo(expectedEntity);
        });
    }

    [Test]
    public async Task GetByIdAsync_SecondCall_ReturnsCachedResult()
    {
        var entityId = Guid.NewGuid();
        var expectedEntity = new TestEntity { Id = entityId, Name = "Test Entity" };

        _mockInnerRepository.Setup(x => x.GetByIdAsync(entityId)).ReturnsAsync(expectedEntity);

        await _cachingRepository.GetByIdAsync(entityId);
        var result = await _cachingRepository.GetByIdAsync(entityId);

        Assert.Multiple(() =>
        {
            result.Should().BeEquivalentTo(expectedEntity);
            _mockInnerRepository.Verify(x => x.GetByIdAsync(entityId), Times.Once);
        });
    }

    [Test]
    public async Task GetAllAsync_FirstCall_CallsInnerRepositoryAndCachesResult()
    {
        var expectedEntities = new List<TestEntity>
        {
            new() { Id = Guid.NewGuid(), Name = "Entity 1" }, new() { Id = Guid.NewGuid(), Name = "Entity 2" }
        };

        _mockInnerRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(expectedEntities);

        var result = await _cachingRepository.GetAllAsync();

        Assert.Multiple(() =>
        {
            result.Should().BeEquivalentTo(expectedEntities);
            _mockInnerRepository.Verify(x => x.GetAllAsync(), Times.Once);
        });
    }

    [Test]
    public async Task AddAsync_InvalidatesCache()
    {
        var entityId = Guid.NewGuid();
        var existingEntity = new TestEntity { Id = entityId, Name = "Existing" };
        var newEntity = new TestEntity { Id = Guid.NewGuid(), Name = "New Entity" };

        _mockInnerRepository.Setup(x => x.GetByIdAsync(entityId)).ReturnsAsync(existingEntity);
        _mockInnerRepository.Setup(x => x.AddAsync(newEntity)).Returns(Task.CompletedTask);

        await _cachingRepository.GetByIdAsync(entityId);
        var cacheKey = $"Test_TestEntity_Id_{entityId}";
        var allCacheKey = "Test_TestEntity_All";

        _memoryCache.TryGetValue(cacheKey, out TestEntity? _).Should().BeTrue();

        await _cachingRepository.AddAsync(newEntity);

        var allCacheStillExists = _memoryCache.TryGetValue(allCacheKey, out _);

        Assert.Multiple(() =>
        {
            _mockInnerRepository.Verify(x => x.AddAsync(newEntity), Times.Once);
            Assert.That(allCacheStillExists, Is.False);
        });
    }

    [Test]
    public async Task UpdateAsync_InvalidatesCache()
    {
        var entityId = Guid.NewGuid();
        var originalEntity = new TestEntity { Id = entityId, Name = "Original" };
        var updatedEntity = new TestEntity { Id = entityId, Name = "Updated" };

        _mockInnerRepository.Setup(x => x.GetByIdAsync(entityId)).ReturnsAsync(originalEntity);
        _mockInnerRepository.Setup(x => x.UpdateAsync(updatedEntity)).Returns(Task.CompletedTask);

        await _cachingRepository.GetByIdAsync(entityId);

        await _cachingRepository.UpdateAsync(updatedEntity);

        var cacheKey = $"Test_TestEntity_Id_{entityId}";
        var stillCached = _memoryCache.TryGetValue(cacheKey, out _);

        Assert.Multiple(() =>
        {
            _mockInnerRepository.Verify(x => x.UpdateAsync(updatedEntity), Times.Once);
            Assert.That(stillCached, Is.False);
        });
    }

    [Test]
    public async Task DeleteAsync_InvalidatesCache()
    {
        var entityId = Guid.NewGuid();
        var entityToDelete = new TestEntity { Id = entityId, Name = "To Delete" };

        _mockInnerRepository.Setup(x => x.GetByIdAsync(entityId)).ReturnsAsync(entityToDelete);
        _mockInnerRepository.Setup(x => x.DeleteAsync(entityToDelete)).Returns(Task.CompletedTask);

        await _cachingRepository.GetByIdAsync(entityId);

        await _cachingRepository.DeleteAsync(entityToDelete);

        var cacheKey = $"Test_TestEntity_Id_{entityId}";
        var stillCached = _memoryCache.TryGetValue(cacheKey, out _);

        Assert.Multiple(() =>
        {
            _mockInnerRepository.Verify(x => x.DeleteAsync(entityToDelete), Times.Once);
            Assert.That(stillCached, Is.False);
        });
    }

    [Test]
    public async Task GetByIdAsync_WhenCachingDisabled_AlwaysCallsInnerRepository()
    {
        _cachingOptions.EnableCaching = false;

        var entityId = Guid.NewGuid();
        var expectedEntity = new TestEntity { Id = entityId, Name = "Test Entity" };

        _mockInnerRepository.Setup(x => x.GetByIdAsync(entityId)).ReturnsAsync(expectedEntity);

        await _cachingRepository.GetByIdAsync(entityId);
        await _cachingRepository.GetByIdAsync(entityId);

        _mockInnerRepository.Verify(x => x.GetByIdAsync(entityId), Times.Exactly(2));
    }

    [Test]
    public async Task CacheExpiration_CallsInnerRepositoryAfterExpiration()
    {
        var shortCacheOptions = new CacheOptions()
        {
            GetByIdCacheDuration = TimeSpan.FromMilliseconds(100), EnableCaching = true, CacheKeyPrefix = "Test"
        };

        var mockOptions = new Mock<IOptions<CacheOptions>>();
        mockOptions.Setup(x => x.Value).Returns(shortCacheOptions);

        var shortCacheRepository = new CachingRepositoryDecorator<TestEntity>(
            _mockInnerRepository.Object,
            _memoryCache,
            mockOptions.Object,
            _mockLogger.Object);

        var entityId = Guid.NewGuid();
        var expectedEntity = new TestEntity { Id = entityId, Name = "Test Entity" };

        _mockInnerRepository.Setup(x => x.GetByIdAsync(entityId)).ReturnsAsync(expectedEntity);

        await shortCacheRepository.GetByIdAsync(entityId);
        await Task.Delay(150);
        await shortCacheRepository.GetByIdAsync(entityId);

        _mockInnerRepository.Verify(x => x.GetByIdAsync(entityId), Times.Exactly(2));
    }

    public void Dispose()
    {
        _memoryCache.Dispose();
        GC.SuppressFinalize(this);
    }
}

[TestFixture]
public class CachingPerformanceTests
{
    [Test]
    public async Task CachePerformance_ShowsSignificantSpeedUp()
    {
        var mockRepository = new Mock<IGenericRepository<TestEntity>>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new CacheOptions { EnableCaching = true });

        var cachingRepository = new CachingRepositoryDecorator<TestEntity>(
            mockRepository.Object, memoryCache, options);

        var entityId = Guid.NewGuid();
        var testEntity = new TestEntity { Id = entityId, Name = "Test" };

        mockRepository.Setup(x => x.GetByIdAsync(entityId)).Returns(async () =>
        {
            await Task.Delay(100); // simulate DB delay
            return testEntity;
        });

        var stopwatch = Stopwatch.StartNew();
        var result1 = await cachingRepository.GetByIdAsync(entityId);
        var firstCallTime = stopwatch.ElapsedMilliseconds;

        stopwatch.Restart();
        var result2 = await cachingRepository.GetByIdAsync(entityId);
        var secondCallTime = stopwatch.ElapsedMilliseconds;

        Assert.Multiple(() =>
        {
            Assert.That(firstCallTime, Is.GreaterThan(90));
            Assert.That(secondCallTime, Is.LessThan(20));
            result1.Should().BeEquivalentTo(result2);
        });

        mockRepository.Verify(x => x.GetByIdAsync(entityId), Times.Once);
    }
}
