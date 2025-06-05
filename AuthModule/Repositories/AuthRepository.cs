using TBD.AuthModule.Data;

namespace TBD.AuthModule.Repositories;

public class AuthRepository(AuthDbContext context) : IAuthRepository
{
    private readonly AuthDbContext _context = context;
}
