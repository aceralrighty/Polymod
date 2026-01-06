using System.Linq.Expressions;
using AutoMapper;
using PolyMod.AddressModule.Exceptions;
using PolyMod.AddressModule.Models;
using PolyMod.AddressModule.Repositories;
using PolyMod.API.DTOs.Users;
using PolyMod.UserModule.Services;

namespace PolyMod.AddressModule.Services;

public class UserAddressService(
    IMapper mapper,
    IUserService userService,
    IUserAddressRepository repository)
    : IUserAddressService
{
    // private readonly DbSet<UserAddress> _dbSet = context.Set<UserAddress>();

    public async Task<List<IGrouping<string?, UserAddress>>> GroupByUserStateAsync()
    {
        var addresses = await repository.GroupByUserStateAsync();
        return addresses.Count == 0
            ? throw new UserStateGroupException("There are no states to group in the database")
            : addresses;
    }

    public async Task<List<IGrouping<string?, UserAddress>>> GroupByZipCodeAsync()
    {
        return await repository.GroupByZipCodeAsync();
    }

    public async Task<List<IGrouping<string?, UserAddress>>> GroupByCityAsync()
    {
        try
        {
            var groupedCities = await repository.GroupByCityAsync();
            return groupedCities.Count == 0
                ? throw new CityGroupingNotAvailableException("There are no cities to group in the database")
                : groupedCities;
        }
        catch (Exception e) when (e is not CityGroupingNotAvailableException)
        {
            throw new CityGroupingNotAvailableException("There are no cities to group in the database", e);
        }
    }

    public async Task<IEnumerable<UserAddress>> GetAllAsync(Guid userId)
    {
        return await repository.GetByUserIdAsync(userId);
    }

    public async Task<IEnumerable<UserAddress>> FindAsync(
        Expression<Func<UserAddress, bool>> expression)
    {
        return await repository.FindAsync(expression);
    }

    public async Task AddAsync(UserAddress entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity), "The address entity cannot be null.");

        await repository.CreateAsync(entity);
    }

    public async Task AddRangeAsync(IEnumerable<UserAddress> entities)
    {
        if (entities == null)
        {
            throw new ArgumentNullException(nameof(entities), "The address collection cannot be null.");
        }

        await repository.AddRangeAsync(entities);
    }

    public async Task UpdateAsync(UserAddress entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await repository.UpdateAsync(entity);
    }

    public async Task RemoveAsync(UserAddress entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await repository.DeleteAsync(entity.Id);
    }

    public async Task<UserAddress> UpdateUserAddress(UserAddressRequest userAddressDto)
    {
        var user = await userService.GetUserByIdAsync(userAddressDto.UserId);
        if (user == null)
        {
            throw new ArgumentException("User not found, cannot update address.");
        }

        var existingAddress = await repository.GetByIdAsync(userAddressDto.Id);
        if (existingAddress == null)
        {
            throw new ArgumentNullException(nameof(existingAddress), "User Address does not exist");
        }

        mapper.Map(userAddressDto, existingAddress);

        return await repository.UpdateAsync(existingAddress);
    }
}
