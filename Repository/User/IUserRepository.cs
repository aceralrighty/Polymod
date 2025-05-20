using TBD.Repository.Services.Base;

namespace TBD.Repository.Services.User;

public interface IUserRepository: IGenericRepository<Models.Entities.User>
{
    Task<Models.Entities.User> GetByEmailAsync(string email);
    Task<Models.Entities.User> GetByUsernameAsync(string username);
}