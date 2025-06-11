namespace TBD.Shared.Repositories;

public class CacheOptions
{
    public TimeSpan DefaultCacheDuration { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan GetAllCacheDuration { get; set; } = TimeSpan.FromMinutes(2);
    public TimeSpan GetByIdCacheDuration { get; set; } = TimeSpan.FromMinutes(10);
    public int MaxCachedItems { get; set; } = 1000;
    public bool EnableCaching { get; set; } = true;
    public string CacheKeyPrefix { get; set; } = "Repo";
}
