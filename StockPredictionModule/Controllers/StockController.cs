using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TBD.MetricsModule.Services.Interfaces;
using TBD.StockPredictionModule.Context;
using TBD.StockPredictionModule.ML.Interface;
using TBD.StockPredictionModule.Models;
using TBD.StockPredictionModule.Models.Stocks;

namespace TBD.StockPredictionModule.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockController : ControllerBase
    {
        private readonly StockDbContext _context;
        private readonly IMlStockPredictionEngine _predictionEngine;
        private readonly IMetricsService _metricsService;

        // OpenTelemetry metrics
        private static readonly Meter Meter = new("TBD.StockController", "1.0.0");

        private static readonly Counter<int> ApiCallsCounter =
            Meter.CreateCounter<int>("stock_api_calls_total", "Total number of API calls");

        private static readonly Counter<int> ApiErrorsCounter =
            Meter.CreateCounter<int>("stock_api_errors_total", "Total number of API errors");

        private static readonly Histogram<double> ApiDurationHistogram =
            Meter.CreateHistogram<double>("stock_api_duration_seconds", "Duration of API calls in seconds");

        private static readonly Counter<int> PredictionRequestsCounter =
            Meter.CreateCounter<int>("stock_prediction_requests_total", "Total number of prediction requests");

        private static readonly Counter<int> ModelTrainingRequestsCounter =
            Meter.CreateCounter<int>("stock_model_training_requests_total", "Total number of model training requests");

        public StockController(StockDbContext context, IMlStockPredictionEngine predictionEngine,
            IMetricsServiceFactory metricsServiceFactory)
        {
            _context = context;
            _predictionEngine = predictionEngine;
            _metricsService = metricsServiceFactory.CreateMetricsService("StockController");
        }

        // GET: api/Stock
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Stock>>> GetStocks()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                ApiCallsCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "GetStocks"));
                _metricsService.IncrementCounter("stock.controller.get_stocks");

                var stocks = await _context.Stocks.ToListAsync();
                return stocks;
            }
            catch (Exception)
            {
                ApiErrorsCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "GetStocks"));
                _metricsService.IncrementCounter("stock.controller.get_stocks.error");
                throw;
            }
            finally
            {
                stopwatch.Stop();
                ApiDurationHistogram.Record(stopwatch.Elapsed.TotalSeconds,
                    new KeyValuePair<string, object?>("endpoint", "GetStocks"));
            }
        }

        // GET: api/Stock/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Stock>> GetStock(Guid id)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                ApiCallsCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "GetStock"));
                _metricsService.IncrementCounter("stock.controller.get_stock");

                var stock = await _context.Stocks.FindAsync(id);

                if (stock == null)
                {
                    _metricsService.IncrementCounter("stock.controller.get_stock.not_found");
                    return NotFound();
                }

                return stock;
            }
            catch (Exception)
            {
                ApiErrorsCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "GetStock"));
                _metricsService.IncrementCounter("stock.controller.get_stock.error");
                throw;
            }
            finally
            {
                stopwatch.Stop();
                ApiDurationHistogram.Record(stopwatch.Elapsed.TotalSeconds,
                    new KeyValuePair<string, object?>("endpoint", "GetStock"));
            }
        }

        // PUT: api/Stock/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutStock(Guid id, Stock stock)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                ApiCallsCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "PutStock"));
                _metricsService.IncrementCounter("stock.controller.put_stock");

                if (id != stock.Id)
                {
                    _metricsService.IncrementCounter("stock.controller.put_stock.bad_request");
                    return BadRequest();
                }

                _context.Entry(stock).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                    _metricsService.IncrementCounter("stock.controller.put_stock.success");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StockExists(id))
                    {
                        _metricsService.IncrementCounter("stock.controller.put_stock.not_found");
                        return NotFound();
                    }
                    else
                    {
                        _metricsService.IncrementCounter("stock.controller.put_stock.concurrency_error");
                        throw;
                    }
                }

                return NoContent();
            }
            catch (Exception)
            {
                ApiErrorsCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "PutStock"));
                _metricsService.IncrementCounter("stock.controller.put_stock.error");
                throw;
            }
            finally
            {
                stopwatch.Stop();
                ApiDurationHistogram.Record(stopwatch.Elapsed.TotalSeconds,
                    new KeyValuePair<string, object?>("endpoint", "PutStock"));
            }
        }

        // POST: api/Stock
        [HttpPost]
        public async Task<ActionResult<Stock>> PostStock(Stock stock)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                ApiCallsCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "PostStock"));
                _metricsService.IncrementCounter("stock.controller.post_stock");

                _context.Stocks.Add(stock);
                await _context.SaveChangesAsync();

                _metricsService.IncrementCounter("stock.controller.post_stock.success");
                return CreatedAtAction("GetStock", new { id = stock.Id }, stock);
            }
            catch (Exception)
            {
                ApiErrorsCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "PostStock"));
                _metricsService.IncrementCounter("stock.controller.post_stock.error");
                throw;
            }
            finally
            {
                stopwatch.Stop();
                ApiDurationHistogram.Record(stopwatch.Elapsed.TotalSeconds,
                    new KeyValuePair<string, object?>("endpoint", "PostStock"));
            }
        }

        // DELETE: api/Stock/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStock(Guid id)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                ApiCallsCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "DeleteStock"));
                _metricsService.IncrementCounter("stock.controller.delete_stock");

                var stock = await _context.Stocks.FindAsync(id);
                if (stock == null)
                {
                    _metricsService.IncrementCounter("stock.controller.delete_stock.not_found");
                    return NotFound();
                }

                _context.Stocks.Remove(stock);
                await _context.SaveChangesAsync();

                _metricsService.IncrementCounter("stock.controller.delete_stock.success");
                return NoContent();
            }
            catch (Exception)
            {
                ApiErrorsCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "DeleteStock"));
                _metricsService.IncrementCounter("stock.controller.delete_stock.error");
                throw;
            }
            finally
            {
                stopwatch.Stop();
                ApiDurationHistogram.Record(stopwatch.Elapsed.TotalSeconds,
                    new KeyValuePair<string, object?>("endpoint", "DeleteStock"));
            }
        }

        // GET: api/Stock/model/status
        [HttpGet("model/status")]
        public async Task<ActionResult<object>> GetModelStatus()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                ApiCallsCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "GetModelStatus"));
                _metricsService.IncrementCounter("stock.controller.model_status");

                var isModelTrained = await _predictionEngine.IsModelTrainedAsync();

                return Ok(new
                {
                    isModelTrained,
                    message = isModelTrained
                        ? "Model is trained and ready for predictions"
                        : "Model needs to be trained",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception)
            {
                ApiErrorsCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "GetModelStatus"));
                _metricsService.IncrementCounter("stock.controller.model_status.error");
                throw;
            }
            finally
            {
                stopwatch.Stop();
                ApiDurationHistogram.Record(stopwatch.Elapsed.TotalSeconds,
                    new KeyValuePair<string, object?>("endpoint", "GetModelStatus"));
            }
        }

        // POST: api/Stock/model/train
        [HttpPost("model/train")]
        public async Task<ActionResult<object>> TrainModel([FromBody] List<RawData> rawData)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                ApiCallsCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "TrainModel"));
                ModelTrainingRequestsCounter.Add(1);
                _metricsService.IncrementCounter("stock.controller.train_model");

                if (rawData.Count == 0)
                {
                    _metricsService.IncrementCounter("stock.controller.train_model.invalid_data");
                    return BadRequest(new { error = "Training data is required" });
                }

                await _predictionEngine.TrainModelAsync(rawData);

                _metricsService.IncrementCounter("stock.controller.train_model.success");
                return Ok(new
                {
                    message = "Model trained successfully",
                    recordsProcessed = rawData.Count,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                ApiErrorsCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "TrainModel"));
                _metricsService.IncrementCounter("stock.controller.train_model.error");
                return BadRequest(new { error = ex.Message });
            }
            finally
            {
                stopwatch.Stop();
                ApiDurationHistogram.Record(stopwatch.Elapsed.TotalSeconds,
                    new KeyValuePair<string, object?>("endpoint", "TrainModel"));
            }
        }

        // POST: api/Stock/predict/{symbol}
        [HttpPost("predict/{symbol}")]
        public async Task<ActionResult<object>> PredictStock(string symbol, [FromBody] List<RawData> rawData)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                ApiCallsCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "PredictStock"));
                PredictionRequestsCounter.Add(1, new KeyValuePair<string, object?>("symbol", symbol));
                _metricsService.IncrementCounter("stock.controller.predict_stock");

                if (string.IsNullOrWhiteSpace(symbol))
                {
                    _metricsService.IncrementCounter("stock.controller.predict_stock.invalid_symbol");
                    return BadRequest(new { error = "Stock symbol is required" });
                }

                if (rawData.Count == 0)
                {
                    _metricsService.IncrementCounter("stock.controller.predict_stock.invalid_data");
                    return BadRequest(new { error = "Historical data is required for prediction" });
                }

                var prediction = await _predictionEngine.GeneratePredictAsync(rawData, symbol);

                _metricsService.IncrementCounter("stock.controller.predict_stock.success");
                return Ok(new
                {
                    prediction,
                    symbol,
                    confidence = "Model confidence varies based on data quality",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                ApiErrorsCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "PredictStock"));
                _metricsService.IncrementCounter("stock.controller.predict_stock.error");
                return BadRequest(new { error = ex.Message });
            }
            finally
            {
                stopwatch.Stop();
                ApiDurationHistogram.Record(stopwatch.Elapsed.TotalSeconds,
                    new KeyValuePair<string, object?>("endpoint", "PredictStock"));
            }
        }

        // GET: api/Stock/predict/batch
        [HttpPost("predict/batch")]
        public async Task<ActionResult<object>> PredictMultipleStocks([FromBody] BatchPredictionRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                ApiCallsCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "PredictMultipleStocks"));
                _metricsService.IncrementCounter("stock.controller.predict_batch");

                if (request.Symbols.Count == 0)
                {
                    _metricsService.IncrementCounter("stock.controller.predict_batch.invalid_symbols");
                    return BadRequest(new { error = "At least one stock symbol is required" });
                }

                if (request.RawData.Count == 0)
                {
                    _metricsService.IncrementCounter("stock.controller.predict_batch.invalid_data");
                    return BadRequest(new { error = "Historical data is required for predictions" });
                }

                var predictions = new List<object>();
                var errors = new List<object>();

                foreach (var symbol in request.Symbols)
                {
                    try
                    {
                        var prediction = await _predictionEngine.GeneratePredictAsync(request.RawData, symbol);
                        predictions.Add(new { symbol, prediction, status = "success" });

                        PredictionRequestsCounter.Add(1, new KeyValuePair<string, object?>("symbol", symbol));
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new { symbol, error = ex.Message, status = "error" });

                        _metricsService.IncrementCounter("stock.controller.predict_batch.symbol_error");
                    }
                }

                _metricsService.IncrementCounter("stock.controller.predict_batch.success");
                return Ok(new
                {
                    predictions,
                    errors,
                    totalRequested = request.Symbols.Count,
                    successCount = predictions.Count,
                    errorCount = errors.Count,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                ApiErrorsCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "PredictMultipleStocks"));
                _metricsService.IncrementCounter("stock.controller.predict_batch.error");
                return BadRequest(new { error = ex.Message });
            }
            finally
            {
                stopwatch.Stop();
                ApiDurationHistogram.Record(stopwatch.Elapsed.TotalSeconds,
                    new KeyValuePair<string, object?>("endpoint", "PredictMultipleStocks"));
            }
        }

        private bool StockExists(Guid id)
        {
            return _context.Stocks.Any(e => e.Id == id);
        }
    }

    // Helper class for batch predictions
    public class BatchPredictionRequest
    {
        public List<string> Symbols { get; set; } = [];
        public List<RawData> RawData { get; set; } = [];
    }
}
