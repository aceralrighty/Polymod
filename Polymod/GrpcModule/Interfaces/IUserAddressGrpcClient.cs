using PolyMod.API.DTOs.Users;

namespace PolyMod.GrpcModule.Interfaces;

public interface IUserAddressGrpcClient
{
    Task<bool> UserExistsAsync(Guid userId);
    Task<UserDto?> GetUserAsync(Guid userId);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task<UserAddressResponse?> GetUserAddressAsync(Guid addressId);
    Task<IEnumerable<UserAddressResponse>> GetUserAddressesAsync(Guid userId);
    Task<UserAddressResponse> CreateUserAddressAsync(UserAddressRequest request);
    Task<UserAddressResponse> UpdateUserAddressAsync(UserAddressRequest request);
    Task<bool> DeleteUserAddressAsync(Guid addressId);
}
