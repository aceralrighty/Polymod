using AutoMapper;
using TBD.API.DTOs;
using TBD.API.Interfaces;
using TBD.Shared.Utils;
using TBD.UserModule.Models;
using TBD.UserModule.Repositories;

namespace TBD.UserModule.Services;

internal class UserService(IUserRepository userRepository, IMapper mapper) : IUserService
{
    public async Task<UserDto?> GetUserByIdAsync(Guid id)
    {
        var user = await userRepository.GetByIdAsync(id);
        return mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        var user = await userRepository.GetByEmailAsync(email);
        return mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> GetUserByUsernameAsync(string username)
    {
        var user = await userRepository.GetByUsernameAsync(username);
        return mapper.Map<UserDto>(user);
    }
    public async Task<PagedResult<UserDto>> GetUsersAsync(int page = 1, int pageSize = 50)
    {
        // Validate parameters
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50; // Max 100 to prevent abuse

        var totalCount = await userRepository.GetCountAsync();
        var users = await userRepository.GetPagedAsync(page, pageSize);
        
        return new PagedResult<UserDto>
        {
            Items = mapper.Map<IEnumerable<UserDto>>(users),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
    [Obsolete("This method is deprecated. Use GetAllUsersAsync instead")]
    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await userRepository.GetAllAsync();
        return mapper.Map<IEnumerable<UserDto>>(users);
    }

    public async Task CreateUserAsync(UserDto userDto)
    {
        var user = mapper.Map<User>(userDto);
        if (string.IsNullOrWhiteSpace(user.Password) || string.IsNullOrWhiteSpace(userDto.Password))
        {
            throw new ArgumentException("Password cannot be empty");
        }

        // Hash the password
        user.Password = Hasher.HashPassword(userDto.Password);
        await userRepository.AddAsync(user);
    }

    public async Task UpdateUserAsync(UserDto userDto)
    {
        var user = mapper.Map<User>(userDto);
        await userRepository.UpdateAsync(user);
    }

    public async Task DeleteUserAsync(Guid id)
    {
        var user = await userRepository.GetByIdAsync(id);
        await userRepository.RemoveAsync(user);
    }
}