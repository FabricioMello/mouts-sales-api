using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Users;

public class UsersFunctionalTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public UsersFunctionalTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.SchemeName, "functional-test");
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact(DisplayName = "POST /api/Users should allow creating user without authentication")]
    public async Task Given_NoAuthentication_When_CreateUser_Then_ShouldReturnCreated()
    {
        using var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.PostAsJsonAsync("/api/Users", CreateValidUserRequest(), JsonOptions);
        var body = await ReadBodyAsync<ApiResponseWithData<CreateUserResponse>>(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body.Data);
        Assert.NotEqual(Guid.Empty, body.Data.Id);
    }

    [Fact(DisplayName = "GET /api/Users/{id} should return created user")]
    public async Task Given_CreatedUser_When_GetById_Then_ShouldReturnUser()
    {
        var request = CreateValidUserRequest();
        var created = await CreateUserAsync(request);

        var response = await _client.GetAsync($"/api/Users/{created.Id}");
        var body = await ReadBodyAsync<ApiResponseWithData<GetUserResponse>>(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body.Data);
        Assert.Equal(created.Id, body.Data.Id);
        Assert.Equal(request.Username, body.Data.Name);
        Assert.Equal(request.Email, body.Data.Email);
    }

    [Fact(DisplayName = "GET /api/Users/{id} should return unauthorized without token")]
    public async Task Given_NoAuthentication_When_GetUser_Then_ShouldReturnUnauthorized()
    {
        using var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.GetAsync($"/api/Users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "DELETE /api/Users/{id} should delete created user")]
    public async Task Given_CreatedUser_When_Delete_Then_ShouldReturnOkAndUserShouldNotExist()
    {
        var created = await CreateUserAsync(CreateValidUserRequest());

        var deleteResponse = await _client.DeleteAsync($"/api/Users/{created.Id}");
        var getResponse = await _client.GetAsync($"/api/Users/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private async Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request)
    {
        using var anonymousClient = _factory.CreateClient();
        var response = await anonymousClient.PostAsJsonAsync("/api/Users", request, JsonOptions);
        var body = await ReadBodyAsync<ApiResponseWithData<CreateUserResponse>>(response);

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

    private static CreateUserRequest CreateValidUserRequest()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        return new CreateUserRequest
        {
            Username = $"User {suffix}",
            Password = "Test@1234",
            Phone = "+5511999999999",
            Email = $"user.{suffix}@example.com",
            Status = 1,
            Role = 3
        };
    }

    private sealed class CreateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Status { get; set; }
        public int Role { get; set; }
    }

    private class ApiResponseWithData<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    private sealed class CreateUserResponse
    {
        public Guid Id { get; set; }
    }

    private sealed class GetUserResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int Role { get; set; }
        public int Status { get; set; }
    }
}
