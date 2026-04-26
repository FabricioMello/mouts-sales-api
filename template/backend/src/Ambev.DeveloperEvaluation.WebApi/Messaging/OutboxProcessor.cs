namespace Ambev.DeveloperEvaluation.WebApi.Messaging;

public class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly int _pollingIntervalSeconds;
    private readonly int _batchSize;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessor> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _pollingIntervalSeconds = configuration.GetValue("Outbox:PollingIntervalSeconds", 5);
        _batchSize = configuration.GetValue("Outbox:BatchSize", 20);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Outbox processor started with polling interval {Interval}s and batch size {BatchSize}",
            _pollingIntervalSeconds,
            _batchSize);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_pollingIntervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<OutboxMessageProcessor>();
        await processor.ProcessPendingMessagesAsync(_batchSize, cancellationToken);
    }
}
