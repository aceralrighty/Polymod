using AutoMapper;
using TBD.API.DTOs;
using TBD.API.Interfaces;
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

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await userRepository.GetAllAsync();
        return mapper.Map<IEnumerable<UserDto>>(users);
    }

    public async Task CreateUserAsync(UserDto userDto)
    {
        var user = mapper.Map<User>(userDto);
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