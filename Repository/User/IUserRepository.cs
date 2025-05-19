using TBD.Models;
using TBD.Repository;

namespace TBD.Interfaces.Services;

public interface IUserRepository: IGenericRepository<User>
{
    Task<User> GetByEmailAsync(string email);
    Task<User> GetByUsernameAsync(string username);
}