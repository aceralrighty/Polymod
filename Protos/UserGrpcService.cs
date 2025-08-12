using Grpc.Core;
using TBD.API.DTOs.Users;
using TBD.UserModule.Repositories;
using TBD.UserModule.Services;

namespace TBD.Protos;

// public class UserGrpcService(IUserRepository repo) : UserService
// {
//     public override async Task<UserAddressResponse> GetUser(UserAddressRequest request, ServerCallContext context)
//     {
//         var user = await repo.GetByIdAsync(request.UserId);
//
//         if (user == null)
//             return new UserAddressResponse { Exists = false };
//
//         return new UserAddressResponse
//         {
//             Exists = true,
//             UserId = user.Id,
//             Email = user.Email
//         };
//     }
// }
