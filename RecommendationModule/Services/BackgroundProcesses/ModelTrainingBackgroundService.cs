using TBD.RecommendationModule.Services.Interface;

namespace TBD.RecommendationModule.Services.BackgroundProcesses;

internal class ModelTrainingBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<ModelTrainingBackgroundService> logger)
    : BackgroundService
{
    private readonly TimeSpan _trainingInterval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var recommendationService = scope.ServiceProvider.GetRequiredService<IRecommendationService>();

                logger.LogInformation("Starting scheduled model training...");
                await recommendationService.TrainRecommendationModelAsync();
                logger.LogInformation("Model training completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during model training");
            }

            await Task.Delay(_trainingInterval, stoppingToken);
        }
    }
}
