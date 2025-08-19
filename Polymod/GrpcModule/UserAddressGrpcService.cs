using Addressservice;
using Grpc.Core;
using PolyMod.AddressModule.Models;
using PolyMod.AddressModule.Repositories;
using PolyMod.UserModule.Repositories;
using GrpcAddress = Addressservice.Address;
using GrpcGetAddressRequest = Addressservice.GetAddressRequest;
using GrpcGetAddressResponse = Addressservice.GetAddressResponse;
using GrpcGetUserAddressesRequest = Addressservice.GetUserAddressesRequest;
using GrpcGetUserAddressesResponse = Addressservice.GetUserAddressesResponse;
using GrpcCreateAddressRequest = Addressservice.CreateAddressRequest;
using GrpcCreateAddressResponse = Addressservice.CreateAddressResponse;
using GrpcUpdateAddressRequest = Addressservice.UpdateAddressRequest;
using GrpcUpdateAddressResponse = Addressservice.UpdateAddressResponse;
using GrpcDeleteAddressRequest = Addressservice.DeleteAddressRequest;
using GrpcDeleteAddressResponse = Addressservice.DeleteAddressResponse;

namespace PolyMod.GrpcModule;

/// <summary>
/// UserAddressGrpcService provides gRPC endpoints for managing user address data.
/// This service interacts with the user and address repositories to execute various address-related operations.
/// </summary>
/// <remarks>
/// The service is designed to handle tasks such as retrieving a specific address, retrieving all addresses for a user,
/// creating a new address, updating an existing address, and deleting an address. It extends the base functionality
/// of AddressServiceBase provided by the gRPC Address Service.
/// </remarks>
public class UserAddressGrpcService(
    IUserRepository userRepository,
    IUserAddressRepository addressRepository,
    ILogger<UserAddressGrpcService> logger)
    : AddressService.AddressServiceBase
{
    public override async Task<GrpcGetAddressResponse> GetAddress(GrpcGetAddressRequest request,
        ServerCallContext context)
    {
        try
        {
            logger.LogInformation("GetAddress called with AddressId: {AddressId}", request.AddressId);

            if (!Guid.TryParse(request.AddressId, out var addressId))
            {
                logger.LogWarning("Invalid GUID format for AddressId: {AddressId}", request.AddressId);
                return new GrpcGetAddressResponse { Exists = false };
            }

            var address = await addressRepository.GetByIdAsync(addressId);

            if (address != null)
            {
                return new GrpcGetAddressResponse { Exists = true, Address = MapToGrpcAddress(address) };
            }

            logger.LogInformation("Address not found with Id: {AddressId}", addressId);
            return new GrpcGetAddressResponse { Exists = false };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetAddress for AddressId: {AddressId}", request.AddressId);
            throw new RpcException(new Status(StatusCode.Internal, $"Error retrieving address: {ex.Message}"));
        }
    }

    public override async Task<GrpcGetUserAddressesResponse> GetUserAddresses(GrpcGetUserAddressesRequest request,
        ServerCallContext context)
    {
        try
        {
            logger.LogInformation("GetUserAddresses called with UserId: {UserId}", request.UserId);

            if (!Guid.TryParse(request.UserId, out var userId))
            {
                logger.LogWarning("Invalid GUID format for UserId: {UserId}", request.UserId);
                return new GrpcGetUserAddressesResponse();
            }

            var addresses = await addressRepository.GetByUserIdAsync(userId);
            var userAddresses = addresses as UserAddress[] ?? addresses.ToArray();
            var response = new GrpcGetUserAddressesResponse { TotalCount = userAddresses.Length };

            response.Addresses.AddRange(userAddresses.Select(MapToGrpcAddress));
            logger.LogInformation("Found {Count} addresses for UserId: {UserId}", userAddresses.Length, userId);

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetUserAddresses for UserId: {UserId}", request.UserId);
            throw new RpcException(new Status(StatusCode.Internal, $"Error retrieving user addresses: {ex.Message}"));
        }
    }

    public override async Task<GrpcCreateAddressResponse> CreateAddress(GrpcCreateAddressRequest request,
        ServerCallContext context)
    {
        try
        {
            logger.LogInformation("CreateAddress called for UserId: {UserId}", request.UserId);

            if (!Guid.TryParse(request.UserId, out var userId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID format"));
            }

            // Verify user exists in the domain (repository returns domain user)
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
            logger.LogInformation("Address created with Id: {AddressId} for UserId: {UserId}", createdAddress.Id,
                userId);

            return new GrpcCreateAddressResponse
            {
                Success = true, Message = "Address created successfully", Address = MapToGrpcAddress(createdAddress)
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in CreateAddress for UserId: {UserId}", request.UserId);
            throw new RpcException(new Status(StatusCode.Internal, $"Error creating address: {ex.Message}"));
        }
    }

    public override async Task<GrpcUpdateAddressResponse> UpdateAddress(GrpcUpdateAddressRequest request,
        ServerCallContext context)
    {
        try
        {
            logger.LogInformation("UpdateAddress called for AddressId: {AddressId}", request.AddressId);

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

            existingAddress.UserId = userId;
            existingAddress.Address1 = request.Address1;
            existingAddress.Address2 = request.Address2;
            existingAddress.City = request.City;
            existingAddress.State = request.State;
            existingAddress.ZipCode = request.ZipCode;

            var updatedAddress = await addressRepository.UpdateAsync(existingAddress);
            logger.LogInformation("Address updated with Id: {AddressId}", addressId);

            return new GrpcUpdateAddressResponse
            {
                Success = true, Message = "Address updated successfully", Address = MapToGrpcAddress(updatedAddress)
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in UpdateAddress for AddressId: {AddressId}", request.AddressId);
            throw new RpcException(new Status(StatusCode.Internal, $"Error updating address: {ex.Message}"));
        }
    }

    public override async Task<GrpcDeleteAddressResponse> DeleteAddress(GrpcDeleteAddressRequest request,
        ServerCallContext context)
    {
        try
        {
            logger.LogInformation("DeleteAddress called for AddressId: {AddressId}", request.AddressId);

            if (!Guid.TryParse(request.AddressId, out var addressId))
            {
                return new GrpcDeleteAddressResponse { Success = false, Message = "Invalid address ID format" };
            }

            var deleted = await addressRepository.DeleteAsync(addressId);

            return new GrpcDeleteAddressResponse
            {
                Success = deleted, Message = deleted ? "Address deleted successfully" : "Address not found"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in DeleteAddress for AddressId: {AddressId}", request.AddressId);
            return new GrpcDeleteAddressResponse { Success = false, Message = $"Error deleting address: {ex.Message}" };
        }
    }

    private static GrpcAddress MapToGrpcAddress(UserAddress address)
    {
        return new GrpcAddress
        {
            Id = address.Id.ToString(),
            UserId = address.UserId.ToString(),
            Address1 = address.Address1 ?? string.Empty,
            Address2 = address.Address2 ?? string.Empty,
            City = address.City ?? string.Empty,
            State = address.State ?? string.Empty,
            ZipCode = address.ZipCode ?? string.Empty,
            CreatedAt = address.CreatedAt.ToString("O"),
            UpdatedAt = address.UpdatedAt?.ToString("O") ?? string.Empty
        };
    }
}
