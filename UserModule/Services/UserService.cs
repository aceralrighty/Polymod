
using TBD.UserModule.Models;
using TBD.UserModule.Repositories;

namespace TBD.UserModule.Services;

internal class UserService(IUserRepository userRepository) : IUserService
{
    public async Task<User> GetUserByIdAsync(Guid id)
    {
        return await userRepository.GetByIdAsync(id);
    }

    public async Task<User> GetUserByEmailAsync(string email)
    {
        return await userRepository.GetByEmailAsync(email);
    }

    public async Task<User> GetUserByUsernameAsync(string username)
    {
        return await userRepository.GetByUsernameAsync(username);
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await userRepository.GetAllAsync();
    }

    public async Task CreateUserAsync(User user)
    {
        await userRepository.AddAsync(user);
    }

    public async Task UpdateUserAsync(User user)
    {
        await userRepository.UpdateAsync(user);
    }

    public async Task DeleteUserAsync(Guid id)
    {
        var user = await userRepository.GetByIdAsync(id);
        await userRepository.RemoveAsync(user);
    }
}