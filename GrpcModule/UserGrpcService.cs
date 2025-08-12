using Grpc.Core;
using TBD.AddressModule.Models;
using TBD.AddressModule.Repositories;
using TBD.Shared.Events.Interfaces;
using TBD.UserModule.Repositories;
using Useraddressservice;

namespace TBD.GrpcModule;

public class UserAddressGrpcService(
    IUserRepository userRepository,
    IUserAddressRepository addressRepository,
    IUserReadService userReadService,
    ILogger<UserAddressGrpcService> logger)
    : UserAddressService.UserAddressServiceBase
{
    public override async Task<GetUserResponse> GetUser(GetUserRequest request, ServerCallContext context)
    {
        try
        {
            logger.LogInformation("GetUser called with UserId: {UserId}", request.UserId);

            if (!Guid.TryParse(request.UserId, out var userId))
            {
                logger.LogWarning("Invalid GUID format for UserId: {UserId}", request.UserId);
                return new GetUserResponse { Exists = false };
            }

            var user = await userRepository.GetByIdAsync(userId);

            return new GetUserResponse
            {
                Exists = true,
                UserId = user.Id.ToString(), // ✅ Fixed field name to match proto
                Username = user.Username ?? string.Empty,
                Email = user.Email ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetUser for UserId: {UserId}", request.UserId);
            throw new RpcException(new Status(StatusCode.Internal, $"Error retrieving user: {ex.Message}"));
        }
    }

    public override async Task<GetUserResponse> GetUserByEmail(GetUserByEmailRequest request, ServerCallContext context)
    {
        try
        {
            logger.LogInformation("GetUserByEmail called with Email: {Email}", request.Email);

            var user = await userReadService.GetUserByEmailAsync(request.Email);

            if (user != null)
            {
                return new GetUserResponse
                {
                    Exists = true,
                    UserId = user.Id.ToString(), // ✅ Fixed field name to match proto
                    Username = user.Username ?? string.Empty,
                    Email = user.Email
                };
            }

            logger.LogInformation("User not found with Email: {Email}", request.Email);
            return new GetUserResponse { Exists = false };

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetUserByEmail for Email: {Email}", request.Email);
            throw new RpcException(new Status(StatusCode.Internal, $"Error retrieving user by email: {ex.Message}"));
        }
    }

    public override async Task<GetUserAddressResponse> GetUserAddress(GetUserAddressRequest request, ServerCallContext context)
    {
        try
        {
            logger.LogInformation("GetUserAddress called with AddressId: {AddressId}", request.AddressId);

            if (!Guid.TryParse(request.AddressId, out var addressId))
            {
                logger.LogWarning("Invalid GUID format for AddressId: {AddressId}", request.AddressId);
                return new GetUserAddressResponse { Exists = false };
            }

            var address = await addressRepository.GetByIdAsync(addressId);

            if (address != null)
            {
                return new GetUserAddressResponse { Exists = true, Address = MapToGrpcResponse(address) };
            }

            logger.LogInformation("Address not found with Id: {AddressId}", addressId);
            return new GetUserAddressResponse { Exists = false };

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetUserAddress for AddressId: {AddressId}", request.AddressId);
            throw new RpcException(new Status(StatusCode.Internal, $"Error retrieving address: {ex.Message}"));
        }
    }

    public override async Task<GetUserAddressesResponse> GetUserAddressesByUserId(GetUserAddressesByUserIdRequest request, ServerCallContext context)
    {
        try
        {
            logger.LogInformation("GetUserAddressesByUserId called with UserId: {UserId}", request.UserId);

            if (!Guid.TryParse(request.UserId, out var userId))
            {
                logger.LogWarning("Invalid GUID format for UserId: {UserId}", request.UserId);
                return new GetUserAddressesResponse();
            }

            var addresses = await addressRepository.GetByUserIdAsync(userId);
            var response = new GetUserAddressesResponse();

            var userAddresses = addresses as UserAddress[] ?? addresses.ToArray();
            if (userAddresses.Any())
            {
                response.Addresses.AddRange(userAddresses.Select(MapToGrpcResponse));
                logger.LogInformation("Found {Count} addresses for UserId: {UserId}", userAddresses.Count(), userId);
            }
            else
            {
                logger.LogInformation("No addresses found for UserId: {UserId}", userId);
            }

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetUserAddressesByUserId for UserId: {UserId}", request.UserId);
            throw new RpcException(new Status(StatusCode.Internal, $"Error retrieving user addresses: {ex.Message}"));
        }
    }

    public override async Task<UserAddressResponse> CreateUserAddress(CreateUserAddressRequest request, ServerCallContext context)
    {
        try
        {
            logger.LogInformation("CreateUserAddress called for UserId: {UserId}", request.UserId);

            if (!Guid.TryParse(request.UserId, out var userId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID format"));
            }

            // Verify user exists
            var userExists = await userRepository.GetByIdAsync(userId);
            if (userExists == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "User not found"));
            }

            var address = new UserAddress(
                userId: userId,
                user: userExists,
                address1: request.Address1,
                address2: request.Address2,
                city: request.City,
                state: request.State,
                zipCode: request.ZipCode
            );

            var createdAddress = await addressRepository.CreateAsync(address);
            logger.LogInformation("Address created with Id: {AddressId} for UserId: {UserId}", createdAddress.Id, userId);

            return MapToGrpcResponse(createdAddress);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in CreateUserAddress for UserId: {UserId}", request.UserId);
            throw new RpcException(new Status(StatusCode.Internal, $"Error creating address: {ex.Message}"));
        }
    }

    public override async Task<UserAddressResponse> UpdateUserAddress(UpdateUserAddressRequest request, ServerCallContext context)
    {
        try
        {
            logger.LogInformation("UpdateUserAddress called for AddressId: {AddressId}", request.AddressId);

            if (!Guid.TryParse(request.AddressId, out var addressId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid address ID format"));
            }

            if (!Guid.TryParse(request.UserId, out var userId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID format"));
            }

            var existingAddress = await addressRepository.GetByIdAsync(addressId);
            if (existingAddress == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Address not found"));
            }

            // Update properties
            existingAddress.UserId = userId;
            existingAddress.Address1 = request.Address1;
            existingAddress.Address2 = request.Address2;
            existingAddress.City = request.City;
            existingAddress.State = request.State;
            existingAddress.ZipCode = request.ZipCode;

            var updatedAddress = await addressRepository.UpdateAsync(existingAddress);
            logger.LogInformation("Address updated with Id: {AddressId}", addressId);

            return MapToGrpcResponse(updatedAddress);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in UpdateUserAddress for AddressId: {AddressId}", request.AddressId);
            throw new RpcException(new Status(StatusCode.Internal, $"Error updating address: {ex.Message}"));
        }
    }

    public override async Task<DeleteUserAddressResponse> DeleteUserAddress(DeleteUserAddressRequest request, ServerCallContext context)
    {
        try
        {
            logger.LogInformation("DeleteUserAddress called for AddressId: {AddressId}", request.AddressId);

            if (!Guid.TryParse(request.AddressId, out var addressId))
            {
                return new DeleteUserAddressResponse
                {
                    Success = false,
                    Message = "Invalid address ID format"
                };
            }

            var deleted = await addressRepository.DeleteAsync(addressId);

            var response = new DeleteUserAddressResponse
            {
                Success = deleted,
                Message = deleted ? "Address deleted successfully" : "Address not found"
            };

            logger.LogInformation("Delete operation for AddressId: {AddressId} - Success: {Success}", addressId, deleted);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in DeleteUserAddress for AddressId: {AddressId}", request.AddressId);
            return new DeleteUserAddressResponse
            {
                Success = false,
                Message = $"Error deleting address: {ex.Message}"
            };
        }
    }

    private static UserAddressResponse MapToGrpcResponse(UserAddress address)
    {
        return new UserAddressResponse
        {
            Id = address.Id.ToString(),
            UserId = address.UserId.ToString(),
            Address1 = address.Address1 ?? string.Empty,
            Address2 = address.Address2 ?? string.Empty,
            City = address.City ?? string.Empty,
            State = address.State ?? string.Empty,
            ZipCode = address.ZipCode ?? string.Empty
        };
    }
}
