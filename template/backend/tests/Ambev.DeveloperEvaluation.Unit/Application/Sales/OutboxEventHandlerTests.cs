using System.Text.Json;
using Ambev.DeveloperEvaluation.Application.Sales.Events.Handlers;
using Ambev.DeveloperEvaluation.Application.Sales.Events.Notifications;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class OutboxEventHandlerTests
{
    [Fact(DisplayName = "SaleCreatedEvent handler should write outbox message with correct event name and payload")]
    public async Task Given_SaleCreatedEvent_When_Handled_Then_ShouldWriteOutboxMessage()
    {
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var logger = Substitute.For<ILogger<SaleCreatedEventHandler>>();
        var handler = new SaleCreatedEventHandler(logger, outboxRepository);
        OutboxMessage? capturedMessage = null;

        _ = outboxRepository.AddAsync(Arg.Do<OutboxMessage>(msg => capturedMessage = msg), Arg.Any<CancellationToken>());

        var notification = new SaleCreatedEvent(
            Guid.NewGuid(), "SALE-001", Guid.NewGuid(), "Customer",
            Guid.NewGuid(), "Branch", 100m, 2, DateTime.UtcNow);

        await handler.Handle(notification, CancellationToken.None);

        await outboxRepository.Received(1).AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        Assert.NotNull(capturedMessage);
        Assert.Equal(nameof(SaleCreatedEvent), capturedMessage!.EventName);

        var deserialized = JsonSerializer.Deserialize<JsonElement>(capturedMessage.Payload);
        Assert.Equal("SALE-001", deserialized.GetProperty("saleNumber").GetString());
        Assert.Equal(100m, deserialized.GetProperty("totalAmount").GetDecimal());
    }

    [Fact(DisplayName = "SaleCancelledEvent handler should write outbox message with correct event name and payload")]
    public async Task Given_SaleCancelledEvent_When_Handled_Then_ShouldWriteOutboxMessage()
    {
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var logger = Substitute.For<ILogger<SaleCancelledEventHandler>>();
        var handler = new SaleCancelledEventHandler(logger, outboxRepository);
        OutboxMessage? capturedMessage = null;

        _ = outboxRepository.AddAsync(Arg.Do<OutboxMessage>(msg => capturedMessage = msg), Arg.Any<CancellationToken>());

        var saleId = Guid.NewGuid();
        var notification = new SaleCancelledEvent(saleId, "SALE-002", DateTime.UtcNow);

        await handler.Handle(notification, CancellationToken.None);

        await outboxRepository.Received(1).AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        Assert.NotNull(capturedMessage);
        Assert.Equal(nameof(SaleCancelledEvent), capturedMessage!.EventName);

        var deserialized = JsonSerializer.Deserialize<JsonElement>(capturedMessage.Payload);
        Assert.Equal("SALE-002", deserialized.GetProperty("saleNumber").GetString());
        Assert.Equal(saleId.ToString(), deserialized.GetProperty("saleId").GetString());
    }

    [Fact(DisplayName = "SaleItemCancelledEvent handler should write outbox message with correct event name and payload")]
    public async Task Given_SaleItemCancelledEvent_When_Handled_Then_ShouldWriteOutboxMessage()
    {
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var logger = Substitute.For<ILogger<SaleItemCancelledEventHandler>>();
        var handler = new SaleItemCancelledEventHandler(logger, outboxRepository);
        OutboxMessage? capturedMessage = null;

        _ = outboxRepository.AddAsync(Arg.Do<OutboxMessage>(msg => capturedMessage = msg), Arg.Any<CancellationToken>());

        var itemId = Guid.NewGuid();
        var notification = new SaleItemCancelledEvent(
            Guid.NewGuid(), "SALE-003", itemId, "Product X", 80m, DateTime.UtcNow);

        await handler.Handle(notification, CancellationToken.None);

        await outboxRepository.Received(1).AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        Assert.NotNull(capturedMessage);
        Assert.Equal(nameof(SaleItemCancelledEvent), capturedMessage!.EventName);

        var deserialized = JsonSerializer.Deserialize<JsonElement>(capturedMessage.Payload);
        Assert.Equal("SALE-003", deserialized.GetProperty("saleNumber").GetString());
        Assert.Equal("Product X", deserialized.GetProperty("productName").GetString());
        Assert.Equal(80m, deserialized.GetProperty("newSaleTotal").GetDecimal());
    }
}
