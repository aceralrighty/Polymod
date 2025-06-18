using AutoMapper;
using TBD.API.DTOs;
using TBD.API.DTOs.Users;
using TBD.MetricsModule.Services;
using TBD.Shared.Utils;
using TBD.UserModule.Models;
using TBD.UserModule.Repositories;

namespace TBD.UserModule.Services;

public class UserService(
    IUserRepository userRepository,
    IMapper mapper,
    IHasher hasher,
    IMetricsServiceFactory metricsServiceFactory) : IUserService
{
    private readonly IMetricsService _metricsService = metricsServiceFactory.CreateMetricsService("UserModule");

    public async Task<UserDto?> GetUserByIdAsync(Guid id)
    {
        _metricsService.IncrementCounter("user.get_by_id.attempt");

        try
        {
            var user = await userRepository.GetByIdAsync(id);
            var userDto = mapper.Map<UserDto>(user);

            _metricsService.IncrementCounter(userDto != null ? "user.get_by_id.success" : "user.get_by_id.not_found");

            return userDto;
        }
        catch (Exception ex)
        {
            _metricsService.IncrementCounter($"user.get_by_id.error: {ex.Message}");
            throw;
        }
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        _metricsService.IncrementCounter("user.get_by_email.attempt");

        try
        {
            var user = await userRepository.GetByEmailAsync(email);
            var userDto = mapper.Map<UserDto>(user);

            _metricsService.IncrementCounter(userDto != null
                ? "user.get_by_email.success"
                : "user.get_by_email.not_found");

            return userDto;
        }
        catch (Exception ex)
        {
            _metricsService.IncrementCounter($"user.get_by_email.error: {ex.Message}");
            throw;
        }
    }

    public async Task<UserDto?> GetUserByUsernameAsync(string username)
    {
        _metricsService.IncrementCounter("user.get_by_username.attempt");

        try
        {
            var user = await userRepository.GetByUsernameAsync(username);
            var userDto = mapper.Map<UserDto>(user);

            _metricsService.IncrementCounter(userDto != null
                ? "user.get_by_username.success"
                : "user.get_by_username.not_found");

            return userDto;
        }
        catch (Exception ex)
        {
            _metricsService.IncrementCounter($"user.get_by_username.error: {ex.Message}");
            throw;
        }
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(int page = 1, int pageSize = 50)
    {
        _metricsService.IncrementCounter("user.get_paged.attempt");

        try
        {
            // Validate parameters
            if (page < 1) page = 1;
            if (pageSize is < 1 or > 100) pageSize = 50; // Max 100 to prevent abuse

            var totalCount = await userRepository.GetCountAsync();
            var users = await userRepository.GetPagedAsync(page, pageSize);

            var result = new PagedResult<UserDto>
            {
                Items = mapper.Map<IEnumerable<UserDto>>(users),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };


            _metricsService.IncrementCounter("user.get_paged.success");

            return result;
        }
        catch (Exception ex)
        {
            _metricsService.IncrementCounter($"user.get_paged.error: {ex.Message}");
            throw;
        }
    }

    [Obsolete("This method is deprecated. Use the new one with pagination to avoid memory issues.")]
    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        _metricsService.IncrementCounter("user.get_all.deprecated_call");

        try
        {
            var users = await userRepository.GetAllAsync();
            var userDtos = mapper.Map<IEnumerable<UserDto>>(users);


            _metricsService.IncrementCounter("user.get_all.success");

            return userDtos;
        }
        catch (Exception ex)
        {
            _metricsService.IncrementCounter($"user.get_all.error -> {ex.Message}");
            throw;
        }
    }

    public async Task CreateUserAsync(UserDto? userDto)
    {
        _metricsService.IncrementCounter("user.create.attempt");

        try
        {
            // Add null check for userDto early if you prefer ArgumentNullException here
            ArgumentNullException.ThrowIfNull(userDto);

            var user = mapper.Map<User>(userDto);
            if (string.IsNullOrWhiteSpace(user.Password) || string.IsNullOrWhiteSpace(userDto.Password))
            {
                _metricsService.IncrementCounter("user.create.password_validation_failed");
                throw new ArgumentException("Password cannot be empty");
            }

            user.Password = hasher.HashPassword(userDto.Password);
            await userRepository.AddAsync(user);


            _metricsService.IncrementCounter("user.create.success");
        }
        catch (ArgumentException ex)
        {
            _metricsService.IncrementCounter($"user.create.validation_error: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _metricsService.IncrementCounter($"user.create.error: {ex.Message}");
            throw;
        }
    }

    public async Task UpdateUserAsync(UserDto? userDto)
    {
        _metricsService.IncrementCounter("user.update.attempt");

        try
        {
            var user = mapper.Map<User>(userDto);
            await userRepository.UpdateAsync(user);


            _metricsService.IncrementCounter("user.update.success");
        }
        catch (Exception ex)
        {
            _metricsService.IncrementCounter($"user.update.error: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteUserAsync(Guid id)
    {
        _metricsService.IncrementCounter("user.delete.attempt");

        try
        {
            var user = await userRepository.GetByIdAsync(id);

            await userRepository.RemoveAsync(user);


            _metricsService.IncrementCounter("user.delete.success");
        }
        catch (Exception ex)
        {
            _metricsService.IncrementCounter($"user.delete.error: {ex.Message}");
            throw;
        }
    }
}
