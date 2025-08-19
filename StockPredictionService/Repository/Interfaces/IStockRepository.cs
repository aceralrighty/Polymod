using StockPredictionService.Models;
using StockPredictionService.Models.Stocks;
using StockPredictionService.Shared.Repositories;

namespace StockPredictionService.Repository.Interfaces;

public interface IStockRepository: IGenericRepository<RawData>
{
    new Task AddAsync(RawData rawData);
    new Task<IEnumerable<RawData>> GetAllAsync();
    Task<RawData?> GetByTableIdAsync(Guid id);
    Task SaveStockAsync(List<Stock> stock);

    Task<IEnumerable<RawData>> GetBySymbolAsync(string symbol);
    Task<IEnumerable<RawData>> GetByHighestVolumeAsync(float volume);

    Task<IEnumerable<RawData>> GetByLowestCloseAsync(float close);

    Task<IEnumerable<RawData>> GetByLatestDateAsync(string date);
}
