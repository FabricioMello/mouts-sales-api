using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Integration.Fixtures;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Sales;

public class OutboxRepositoryIntegrationTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public OutboxRepositoryIntegrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "AddAsync should persist outbox message")]
    public async Task Given_OutboxMessage_When_Added_Then_ShouldBePersisted()
    {
        await using var context = _fixture.CreateDbContext();
        var repository = new OutboxRepository(context);
        var message = new OutboxMessage("TestEvent", "{\"key\":\"value\"}", DateTime.UtcNow);

        await repository.AddAsync(message);
        await context.SaveChangesAsync();

        var pending = await repository.GetPendingAsync(10);

        Assert.Contains(pending, m => m.Id == message.Id);
    }

    [Fact(DisplayName = "GetPendingAsync should return only unprocessed messages")]
    public async Task Given_MixedMessages_When_GetPending_Then_ShouldReturnOnlyUnprocessed()
    {
        await using var context = _fixture.CreateDbContext();
        var repository = new OutboxRepository(context);

        var processed = new OutboxMessage("Event1", "{}", DateTime.UtcNow);
        var pending1 = new OutboxMessage("Event2", "{}", DateTime.UtcNow);
        var pending2 = new OutboxMessage("Event3", "{}", DateTime.UtcNow);

        await repository.AddAsync(processed);
        await repository.AddAsync(pending1);
        await repository.AddAsync(pending2);
        await context.SaveChangesAsync();

        await repository.MarkAsProcessedAsync(processed.Id);

        var pending = await repository.GetPendingAsync(10);

        Assert.DoesNotContain(pending, m => m.Id == processed.Id);
        Assert.Contains(pending, m => m.Id == pending1.Id);
        Assert.Contains(pending, m => m.Id == pending2.Id);
    }

    [Fact(DisplayName = "MarkAsProcessedAsync should set ProcessedAt")]
    public async Task Given_Message_When_MarkAsProcessed_Then_ShouldSetTimestamp()
    {
        await using var context = _fixture.CreateDbContext();
        var repository = new OutboxRepository(context);
        var message = new OutboxMessage("TestEvent", "{}", DateTime.UtcNow);

        await repository.AddAsync(message);
        await context.SaveChangesAsync();

        await repository.MarkAsProcessedAsync(message.Id);

        await using var verifyContext = _fixture.CreateDbContext();
        var updated = await verifyContext.OutboxMessages.FindAsync(message.Id);

        Assert.NotNull(updated);
        Assert.NotNull(updated.ProcessedAt);
    }

    [Fact(DisplayName = "MarkAsFailedAsync should increment retry count and set error")]
    public async Task Given_Message_When_MarkAsFailed_Then_ShouldIncrementRetryAndSetError()
    {
        await using var context = _fixture.CreateDbContext();
        var repository = new OutboxRepository(context);
        var message = new OutboxMessage("TestEvent", "{}", DateTime.UtcNow);

        await repository.AddAsync(message);
        await context.SaveChangesAsync();

        await repository.MarkAsFailedAsync(message.Id, "Connection refused");

        await using var verifyContext = _fixture.CreateDbContext();
        var updated = await verifyContext.OutboxMessages.FindAsync(message.Id);

        Assert.NotNull(updated);
        Assert.Equal(1, updated.RetryCount);
        Assert.Equal("Connection refused", updated.LastError);
    }
}
