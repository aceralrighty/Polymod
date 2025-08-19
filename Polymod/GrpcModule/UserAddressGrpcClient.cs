using Bogus.DataSets;
using Grpc.Net.Client;
using PolyMod.API.DTOs.Users;
using PolyMod.GrpcModule.Interfaces;
using PolyMod.UserModule.Services;
using CreateAddressRequest = Addressservice.CreateAddressRequest;
using UserAddressResponse = PolyMod.API.DTOs.Users.UserAddressResponse;

namespace PolyMod.GrpcModule;

/// <summary>
/// Represents a gRPC client implementation for managing user and address-related operations.
/// </summary>
public class UserAddressGrpcClient : IUserAddressGrpcClient, IDisposable
{
    private readonly GrpcChannel _addressChannel;
    private readonly GrpcChannel _userChannel;
    private readonly AddressService.AddressServiceClient _addressClient;
    private readonly UserService.UserServiceClient _userClient;
    private readonly ILogger<UserAddressGrpcClient> _logger;

    public UserAddressGrpcClient(IConfiguration configuration, ILogger<UserAddressGrpcClient> logger)
    {
        _logger = logger;
        var grpcAddress = configuration.GetConnectionString("GrpcService") ?? "https://localhost:7001";

        var channelOptions = new GrpcChannelOptions
        {
            HttpHandler = CreateHttpHandler(),
            MaxReceiveMessageSize = 4 * 1024 * 1024, // 4MB
            MaxSendMessageSize = 4 * 1024 * 1024 // 4MB
        };

        _addressChannel = GrpcChannel.ForAddress(grpcAddress, channelOptions);
        _userChannel = GrpcChannel.ForAddress(grpcAddress, channelOptions);

        _addressClient = new AddressService.AddressServiceClient(_addressChannel);
        _userClient = new UserService.UserServiceClient(_userChannel);
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
            var request = new UserExistsRequest { UserId = userId.ToString() };
            var response = await _userClient.UserExistsAsync(request);
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
            var response = await _userClient.GetUserAsync(request);

            return !response.Exists ? null : MapUserToDto(response.User);
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
            var response = await _userClient.GetUserByEmailAsync(request);

            return !response.Exists ? null : MapUserToDto(response.User);
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
            var request = new GetAddressRequest { AddressId = addressId.ToString() };
            var response = await _addressClient.GetAddressAsync(request);

            return !response.Exists ? null : MapAddressToDto(response.Address);
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
            var request = new GetUserAddressesRequest { UserId = userId.ToString() };
            var response = await _addressClient.GetUserAddressesAsync(request);

            return response.Addresses.Select(MapAddressToDto);
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
            var grpcRequest = new CreateAddressRequest
            {
                UserId = request.UserId.ToString(),
                Address1 = request.Address1 ?? string.Empty,
                Address2 = request.Address2,
                City = request.City ?? string.Empty,
                State = request.State ?? string.Empty,
                ZipCode = request.ZipCode ?? string.Empty
            };

            var response = await _addressClient.CreateAddressAsync(grpcRequest);
            return MapAddressToDto(response.Address);
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
            var grpcRequest = new UpdateAddressRequest
            {
                AddressId = request.Id.ToString(),
                UserId = request.UserId.ToString(),
                Address1 = request.Address1 ?? string.Empty,
                Address2 = request.Address2,
                City = request.City ?? string.Empty,
                State = request.State ?? string.Empty,
                ZipCode = request.ZipCode ?? string.Empty
            };

            var response = await _addressClient.UpdateAddressAsync(grpcRequest);
            return MapAddressToDto(response.Address);
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
            var request = new DeleteAddressRequest { AddressId = addressId.ToString() };
            var response = await _addressClient.DeleteAddressAsync(request);
            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting address {AddressId}", addressId);
            return false;
        }
    }

    private static UserDto MapUserToDto(User grpcUser)
    {
        return new UserDto(Guid.Parse(grpcUser.Id), grpcUser.Email)
        {
            Username = grpcUser.Username
        };
    }

    private static UserAddressResponse MapAddressToDto(Address grpcAddress)
    {
        return new UserAddressResponse
        {
            Id = Guid.Parse(grpcAddress.Id),
            UserId = Guid.Parse(grpcAddress.UserId),
            Address1 = grpcAddress.Address1,
            Address2 = grpcAddress.Address2,
            City = grpcAddress.City,
            State = grpcAddress.State,
            ZipCode = grpcAddress.ZipCode
        };
    }

    public void Dispose()
    {
        _addressChannel.Dispose();
        _userChannel.Dispose();
        GC.SuppressFinalize(this);
    }
}
