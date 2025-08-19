using Grpc.Core;
using PolyMod.Shared.Utils;
using PolyMod.UserModule.Repositories;
using PolyMod.UserModule.Services;
// Explicit aliases to avoid any ambiguity
using DomainUser = PolyMod.UserModule.Models.User;
using GrpcUser = Userservice.User;
using GrpcGetUserResponse = Userservice.GetUserResponse;
using GrpcGetUserRequest = Userservice.GetUserRequest;
using GrpcGetUserByEmailRequest = Userservice.GetUserByEmailRequest;
using GrpcCreateUserResponse = Userservice.CreateUserResponse;
using GrpcCreateUserRequest = Userservice.CreateUserRequest;
using GrpcUserExistsRequest = Userservice.UserExistsRequest;
using GrpcUserExistsResponse = Userservice.UserExistsResponse;
using GrpcValidateUsersRequest = Userservice.ValidateUsersRequest;
using GrpcValidateUsersResponse = Userservice.ValidateUsersResponse;
using GrpcUserValidation = Userservice.UserValidation;

namespace PolyMod.GrpcModule;

/// <summary>
/// The UserGrpcService class provides gRPC endpoint implementations for user-related operations
/// such as retrieving user details, validating users, creating new users, and checking user existence.
/// </summary>
/// <remarks>
/// This service is built on top of the UserServiceBase abstract class, which is part of the
/// gRPC framework provided for user service definitions. The class relies heavily on the IUserRepository
/// to manage persistence and business logic. Each method in this class handles specific gRPC requests
/// and invokes corresponding business logic or repository calls.
/// </remarks>
/// <example>
/// This class is typically registered as a gRPC service in the application's DI container.
/// </example>
/// <seealso cref="IUserRepository"/>
/// <seealso cref="UserService.UserServiceBase"/>
public class UserGrpcService(IUserRepository userRepository, ILogger<UserGrpcService> logger)
    : UserService.UserServiceBase
{
    public override async Task<GrpcGetUserResponse> GetUser(GrpcGetUserRequest request, ServerCallContext context)
    {
        try
        {
            logger.LogInformation("GetUser called with UserId: {UserId}", request.UserId);

            if (!Guid.TryParse(request.UserId, out var userId))
            {
                logger.LogWarning("Invalid GUID format for UserId: {UserId}", request.UserId);
                return new GrpcGetUserResponse { Exists = false };
            }

            var domainUser = await userRepository.GetByIdAsync(userId);
            return domainUser == null
                ? new GrpcGetUserResponse { Exists = false }
                : new GrpcGetUserResponse { Exists = true, User = MapToGrpcUser(domainUser) };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetUser for UserId: {UserId}", request.UserId);
            throw new RpcException(new Status(StatusCode.Internal, $"Error retrieving user: {ex.Message}"));
        }
    }

    public override async Task<GrpcGetUserResponse> GetUserByEmail(GrpcGetUserByEmailRequest request,
        ServerCallContext context)
    {
        try
        {
            logger.LogInformation("GetUserByEmail called with Email: {Email}", request.Email);

            var user = await userRepository.GetByEmailAsync(request.Email);
            if (user != null)
            {
                return new GrpcGetUserResponse { Exists = true, User = MapToGrpcUser(user) };
            }

            logger.LogInformation("User not found with Email: {Email}", request.Email);
            return new GrpcGetUserResponse { Exists = false };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetUserByEmail for Email: {Email}", request.Email);
            throw new RpcException(new Status(StatusCode.Internal, $"Error retrieving user by email: {ex.Message}"));
        }
    }

    public override async Task<GrpcCreateUserResponse> CreateUser(GrpcCreateUserRequest request,
        ServerCallContext context)
    {
        var hashing = new Hasher();
        try
        {
            logger.LogInformation("CreateUser called with Email: {Email}", request.Email);

            // Check if user already exists
            var existingUser = await userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return new GrpcCreateUserResponse { Success = false, Message = "User with this email already exists" };
            }

            // Create domain user
            var domainUser = new DomainUser
            {
                Username = request.Username ?? string.Empty,
                Email = request.Email ?? string.Empty,
                Password = hashing.HashPassword(request.Password)
            };

            var createdUser = await userRepository.CreateAsync(domainUser);
            logger.LogInformation("User created with Id: {UserId}", createdUser.Id);

            return new GrpcCreateUserResponse
            {
                Success = true, Message = "User created successfully", User = MapToGrpcUser(createdUser)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in CreateUser for Email: {Email}", request.Email);
            // IMPORTANT: Return the gRPC response type (not any DTO)
            return new GrpcCreateUserResponse { Success = false, Message = $"Error creating user: {ex.Message}" };
        }
    }

    public override async Task<GrpcUserExistsResponse> UserExists(GrpcUserExistsRequest request,
        ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.UserId, out var userId))
            {
                return new GrpcUserExistsResponse { Exists = false };
            }

            var user = await userRepository.GetByIdAsync(userId);
            var response = new GrpcUserExistsResponse { Exists = user != null };

            if (user != null)
            {
                response.User = MapToGrpcUser(user);
            }

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in UserExists for UserId: {UserId}", request.UserId);
            return new GrpcUserExistsResponse { Exists = false };
        }
    }

    public override async Task<GrpcValidateUsersResponse> ValidateUsers(GrpcValidateUsersRequest request,
        ServerCallContext context)
    {
        try
        {
            var userIds = request.UserIds
                .Select(id => Guid.TryParse(id, out var guid) ? guid : (Guid?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();

            var existingUsers = await userRepository.GetByIdsAsync(userIds);
            var validations = new List<GrpcUserValidation>();

            foreach (var requestedId in request.UserIds)
            {
                if (!Guid.TryParse(requestedId, out var userId))
                {
                    validations.Add(new GrpcUserValidation { UserId = requestedId, Exists = false });
                    continue;
                }

                var user = existingUsers.FirstOrDefault(u => u.Id == userId);
                var validation = new GrpcUserValidation { UserId = requestedId, Exists = user != null };

                if (user != null)
                {
                    validation.User = MapToGrpcUser(user);
                }

                validations.Add(validation);
            }

            return new GrpcValidateUsersResponse { Validations = { validations } };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ValidateUsers");
            throw new RpcException(new Status(StatusCode.Internal, $"Error validating users: {ex.Message}"));
        }
    }

    // Domain -> gRPC mapping
    private static GrpcUser MapToGrpcUser(DomainUser domainUser)
    {
        return new GrpcUser
        {
            Id = domainUser.Id.ToString(),
            Username = domainUser.Username ?? string.Empty,
            Email = domainUser.Email ?? string.Empty,
            CreatedAt = domainUser.CreatedAt.ToString("O"),
            UpdatedAt = domainUser.UpdatedAt?.ToString("O") ?? string.Empty
        };
    }

    // gRPC -> Domain mapping (only use this if you actually need to accept gRPC users into your domain)
    private static DomainUser MapToDomainUser(GrpcUser grpcUser)
    {
        if (grpcUser == null) return null!;

        return new DomainUser
        {
            Id = Guid.Parse(grpcUser.Id),
            Username = grpcUser.Username,
            Email = grpcUser.Email,
            CreatedAt = !string.IsNullOrEmpty(grpcUser.CreatedAt)
                ? DateTime.Parse(grpcUser.CreatedAt)
                : DateTime.UtcNow,
            UpdatedAt = !string.IsNullOrEmpty(grpcUser.UpdatedAt)
                ? DateTime.Parse(grpcUser.UpdatedAt)
                : null
        };
    }
}
