using TBD.Repository.Base;

namespace TBD.Repository.User;

public interface IUserRepository: IGenericRepository<Models.Entities.User>
{
    Task<Models.Entities.User> GetByEmailAsync(string email);
    Task<Models.Entities.User> GetByUsernameAsync(string username);
}