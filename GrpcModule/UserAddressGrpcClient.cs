using Grpc.Net.Client;
using TBD.API.DTOs.Users;
using TBD.GrpcModule.Interfaces;
using Useraddressservice;
using UserAddressResponse = TBD.API.DTOs.Users.UserAddressResponse;

namespace TBD.GrpcModule;

public class UserAddressGrpcClient : IUserAddressGrpcClient, IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly UserAddressService.UserAddressServiceClient _client;
    private readonly ILogger<UserAddressGrpcClient> _logger;

    public UserAddressGrpcClient(IConfiguration configuration, ILogger<UserAddressGrpcClient> logger)
    {
        _logger = logger;
        var grpcAddress = configuration.GetConnectionString("GrpcService") ?? "https://localhost:7001";

        _channel = GrpcChannel.ForAddress(grpcAddress, new GrpcChannelOptions
        {
            HttpHandler = CreateHttpHandler(),
            MaxReceiveMessageSize = 4 * 1024 * 1024, // 4MB
            MaxSendMessageSize = 4 * 1024 * 1024 // 4MB
        });

        _client = new UserAddressService.UserAddressServiceClient(_channel);
    }

    private static HttpClientHandler CreateHttpHandler()
    {
        var handler = new HttpClientHandler();

        // For development only - in production, use proper certificates
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        }

        return handler;
    }

    public async Task<bool> UserExistsAsync(Guid userId)
    {
        try
        {
            var request = new GetUserRequest { UserId = userId.ToString() };
            var response = await _client.GetUserAsync(request);
            return response.Exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user {UserId} exists", userId);
            return false;
        }
    }

    public async Task<UserDto?> GetUserAsync(Guid userId)
    {
        try
        {
            var request = new GetUserRequest { UserId = userId.ToString() };
            var response = await _client.GetUserAsync(request);

            return !response.Exists
                ? null
                : new UserDto(Guid.Parse(response.UserId), response.Email) { Username = response.Username };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", userId);
            return null;
        }
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        try
        {
            var request = new GetUserByEmailRequest { Email = email };
            var response = await _client.GetUserByEmailAsync(request);

            return !response.Exists
                ? null
                : new UserDto(Guid.Parse(response.UserId), response.Email) { Username = response.Username };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email {Email}", email);
            return null;
        }
    }

    public async Task<UserAddressResponse?> GetUserAddressAsync(Guid addressId)
    {
        try
        {
            var request = new GetUserAddressRequest { AddressId = addressId.ToString() };
            var response = await _client.GetUserAddressAsync(request);

            return !response.Exists ? null : MapToDto(response.Address);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting address {AddressId}", addressId);
            return null;
        }
    }

    public async Task<IEnumerable<UserAddressResponse>> GetUserAddressesAsync(Guid userId)
    {
        try
        {
            var request = new GetUserAddressesByUserIdRequest { UserId = userId.ToString() };
            var response = await _client.GetUserAddressesByUserIdAsync(request);

            return response.Addresses.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting addresses for user {UserId}", userId);
            return [];
        }
    }

    public async Task<UserAddressResponse> CreateUserAddressAsync(UserAddressRequest request)
    {
        try
        {
            var grpcRequest = new CreateUserAddressRequest
            {
                UserId = request.UserId.ToString(),
                Address1 = request.Address1 ?? string.Empty,
                Address2 = request.Address2,
                City = request.City ?? string.Empty,
                State = request.State ?? string.Empty,
                ZipCode = request.ZipCode ?? string.Empty
            };

            var response = await _client.CreateUserAddressAsync(grpcRequest);
            return MapToDto(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating address for user {UserId}", request.UserId);
            throw;
        }
    }

    public async Task<UserAddressResponse> UpdateUserAddressAsync(UserAddressRequest request)
    {
        try
        {
            var grpcRequest = new UpdateUserAddressRequest
            {
                AddressId = request.Id.ToString(),
                UserId = request.UserId.ToString(),
                Address1 = request.Address1 ?? string.Empty,
                Address2 = request.Address2,
                City = request.City ?? string.Empty,
                State = request.State ?? string.Empty,
                ZipCode = request.ZipCode ?? string.Empty
            };

            var response = await _client.UpdateUserAddressAsync(grpcRequest);
            return MapToDto(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating address {AddressId}", request.Id);
            throw;
        }
    }

    public async Task<bool> DeleteUserAddressAsync(Guid addressId)
    {
        try
        {
            var request = new DeleteUserAddressRequest { AddressId = addressId.ToString() };
            var response = await _client.DeleteUserAddressAsync(request);
            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting address {AddressId}", addressId);
            return false;
        }
    }

    private static UserAddressResponse MapToDto(Useraddressservice.UserAddressResponse grpcResponse)
    {
        return new UserAddressResponse
        {
            Id = Guid.Parse(grpcResponse.Id),
            UserId = Guid.Parse(grpcResponse.UserId),
            Address1 = grpcResponse.Address1,
            Address2 = grpcResponse.Address2,
            City = grpcResponse.City,
            State = grpcResponse.State,
            ZipCode = grpcResponse.ZipCode
        };
    }

    public void Dispose()
    {
        _channel.Dispose();
        GC.SuppressFinalize(this);
    }
}
