using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Sales;

public class SalesFunctionalTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SalesFunctionalTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact(DisplayName = "POST /api/Sales should create sale")]
    public async Task Given_ValidSale_When_Post_Then_ShouldReturnCreated()
    {
        var request = CreateValidSaleRequest(items:
        [
            CreateItem("Product 1", 10, 10m),
            CreateItem("Product 2", 4, 10m)
        ]);

        var response = await _client.PostAsJsonAsync("/api/Sales", request, JsonOptions);
        var body = await ReadBodyAsync<ApiResponseWithData<SaleResponse>>(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.NotNull(body.Data);
        Assert.Equal(request.SaleNumber, body.Data.SaleNumber);
        Assert.Equal(116m, body.Data.TotalAmount);
        Assert.Equal(2, body.Data.Items.Count);
    }

    [Fact(DisplayName = "POST /api/Sales should return conflict when sale number already exists")]
    public async Task Given_DuplicateSaleNumber_When_Post_Then_ShouldReturnConflict()
    {
        var saleNumber = NewSaleNumber();
        await CreateSaleAsync(CreateValidSaleRequest(saleNumber));

        var response = await _client.PostAsJsonAsync("/api/Sales", CreateValidSaleRequest(saleNumber), JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact(DisplayName = "POST /api/Sales should return bad request when sale number is empty")]
    public async Task Given_EmptySaleNumber_When_Post_Then_ShouldReturnBadRequest()
    {
        var request = CreateValidSaleRequest();
        request.SaleNumber = string.Empty;

        var response = await _client.PostAsJsonAsync("/api/Sales", request, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "POST /api/Sales should return bad request when item quantity exceeds limit")]
    public async Task Given_QuantityGreaterThanTwenty_When_Post_Then_ShouldReturnBadRequest()
    {
        var request = CreateValidSaleRequest(items: [CreateItem("Product 1", 21, 10m)]);

        var response = await _client.PostAsJsonAsync("/api/Sales", request, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "GET /api/Sales/{id} should return sale")]
    public async Task Given_ExistingSale_When_GetById_Then_ShouldReturnOk()
    {
        var created = await CreateSaleAsync();

        var response = await _client.GetAsync($"/api/Sales/{created.Id}");
        var body = await ReadBodyAsync<ApiResponseWithData<SaleResponse>>(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body.Data);
        Assert.Equal(created.Id, body.Data.Id);
    }

    [Fact(DisplayName = "GET /api/Sales/{id} should return not found")]
    public async Task Given_UnknownSaleId_When_GetById_Then_ShouldReturnNotFound()
    {
        var response = await _client.GetAsync($"/api/Sales/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "GET /api/Sales should return paginated sales")]
    public async Task Given_Pagination_When_ListSales_Then_ShouldReturnExpectedPage()
    {
        var branchId = Guid.NewGuid();
        await CreateSaleAsync(CreateValidSaleRequest(branchId: branchId));
        await CreateSaleAsync(CreateValidSaleRequest(branchId: branchId));
        await CreateSaleAsync(CreateValidSaleRequest(branchId: branchId));

        var response = await _client.GetAsync($"/api/Sales?_page=1&_size=2&_branchId={branchId}");
        var body = await ReadBodyAsync<PaginatedResponse<SaleResponse>>(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body.Data);
        Assert.Equal(3, body.TotalCount);
        Assert.Equal(2, body.Data.Count);
    }

    [Fact(DisplayName = "GET /api/Sales should filter by customer")]
    public async Task Given_CustomerFilter_When_ListSales_Then_ShouldReturnOnlyCustomerSales()
    {
        var customerId = Guid.NewGuid();
        await CreateSaleAsync(CreateValidSaleRequest(customerId: customerId));
        await CreateSaleAsync(CreateValidSaleRequest(customerId: customerId));
        await CreateSaleAsync(CreateValidSaleRequest(customerId: Guid.NewGuid()));

        var response = await _client.GetAsync($"/api/Sales?_page=1&_size=10&_customerId={customerId}");
        var body = await ReadBodyAsync<PaginatedResponse<SaleResponse>>(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body.Data);
        Assert.Equal(2, body.TotalCount);
        Assert.All(body.Data, sale => Assert.Equal(customerId, sale.CustomerId));
    }

    [Fact(DisplayName = "PATCH /api/Sales/{id}/cancel should cancel sale")]
    public async Task Given_ExistingSale_When_Cancel_Then_ShouldReturnCancelledSale()
    {
        var created = await CreateSaleAsync();

        var response = await _client.PatchAsync($"/api/Sales/{created.Id}/cancel", null);
        var body = await ReadBodyAsync<ApiResponseWithData<SaleResponse>>(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body.Data);
        Assert.True(body.Data.IsCancelled);
        Assert.Equal(created.TotalAmount, body.Data.TotalAmount);
        Assert.All(body.Data.Items, item => Assert.True(item.IsCancelled));
    }

    [Fact(DisplayName = "PATCH /api/Sales/{id}/cancel should return not found")]
    public async Task Given_UnknownSaleId_When_Cancel_Then_ShouldReturnNotFound()
    {
        var response = await _client.PatchAsync($"/api/Sales/{Guid.NewGuid()}/cancel", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "PATCH /api/Sales/{id}/cancel should return conflict when sale is already cancelled")]
    public async Task Given_AlreadyCancelledSale_When_Cancel_Then_ShouldReturnConflict()
    {
        var created = await CreateSaleAsync();
        await _client.PatchAsync($"/api/Sales/{created.Id}/cancel", null);

        var response = await _client.PatchAsync($"/api/Sales/{created.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact(DisplayName = "PATCH /api/Sales/{saleId}/items/{itemId}/cancel should cancel item and recalculate sale")]
    public async Task Given_ExistingSaleItem_When_CancelItem_Then_ShouldRecalculateSaleTotal()
    {
        var created = await CreateSaleAsync(CreateValidSaleRequest(items:
        [
            CreateItem("Product 1", 10, 10m),
            CreateItem("Product 2", 4, 10m)
        ]));
        var itemToCancel = created.Items.Single(item => item.ProductName == "Product 1");

        var response = await _client.PatchAsync($"/api/Sales/{created.Id}/items/{itemToCancel.Id}/cancel", null);
        var body = await ReadBodyAsync<ApiResponseWithData<SaleResponse>>(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body.Data);
        Assert.Equal(36m, body.Data.TotalAmount);
        Assert.Contains(body.Data.Items, item => item.Id == itemToCancel.Id && item.IsCancelled);
    }

    [Fact(DisplayName = "PATCH /api/Sales/{saleId}/items/{itemId}/cancel should return not found")]
    public async Task Given_UnknownSaleItem_When_CancelItem_Then_ShouldReturnNotFound()
    {
        var created = await CreateSaleAsync();

        var response = await _client.PatchAsync($"/api/Sales/{created.Id}/items/{Guid.NewGuid()}/cancel", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "PATCH /api/Sales/{saleId}/items/{itemId}/cancel should return conflict when sale is cancelled")]
    public async Task Given_CancelledSale_When_CancelItem_Then_ShouldReturnConflict()
    {
        var created = await CreateSaleAsync();
        var itemId = created.Items.Single().Id;
        await _client.PatchAsync($"/api/Sales/{created.Id}/cancel", null);

        var response = await _client.PatchAsync($"/api/Sales/{created.Id}/items/{itemId}/cancel", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task<SaleResponse> CreateSaleAsync(CreateSaleRequest? request = null)
    {
        var response = await _client.PostAsJsonAsync("/api/Sales", request ?? CreateValidSaleRequest(), JsonOptions);
        var body = await ReadBodyAsync<ApiResponseWithData<SaleResponse>>(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body.Data);

        return body.Data;
    }

    private static async Task<T> ReadBodyAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<T>(content, JsonOptions);

        Assert.NotNull(result);
        return result;
    }

    private static CreateSaleRequest CreateValidSaleRequest(
        string? saleNumber = null,
        Guid? customerId = null,
        Guid? branchId = null,
        IReadOnlyList<SaleItemRequest>? items = null)
    {
        return new CreateSaleRequest
        {
            SaleNumber = saleNumber ?? NewSaleNumber(),
            SaleDate = DateTime.UtcNow,
            CustomerId = customerId ?? Guid.NewGuid(),
            CustomerName = "Functional Customer",
            BranchId = branchId ?? Guid.NewGuid(),
            BranchName = "Functional Branch",
            Items = items?.ToList() ?? [CreateItem("Functional Product", 5, 20m)]
        };
    }

    private static SaleItemRequest CreateItem(string productName, int quantity, decimal unitPrice)
    {
        return new SaleItemRequest
        {
            ProductId = Guid.NewGuid(),
            ProductName = productName,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }

    private static string NewSaleNumber() => $"SALE-FUNC-{Guid.NewGuid():N}";

    private sealed class CreateSaleRequest
    {
        public string SaleNumber { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public Guid BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public List<SaleItemRequest> Items { get; set; } = [];
    }

    private sealed class SaleItemRequest
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    private class ApiResponseWithData<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    private sealed class PaginatedResponse<T> : ApiResponseWithData<List<T>>
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
    }

    private sealed class SaleResponse
    {
        public Guid Id { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public Guid BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public bool IsCancelled { get; set; }
        public List<SaleItemResponse> Items { get; set; } = [];
    }

    private sealed class SaleItemResponse
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsCancelled { get; set; }
    }
}
