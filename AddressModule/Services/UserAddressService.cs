using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TBD.AddressModule.Data;
using TBD.AddressModule.Exceptions;
using TBD.AddressModule.Models;
using TBD.AddressModule.Repositories;
using TBD.API.DTOs;
using TBD.API.Interfaces;

namespace TBD.AddressModule.Services;

internal class UserAddressService(AddressDbContext context, IMapper mapper, IUserService userService)
    : IUserAddressService, IUserAddressRepository
{
    protected readonly AddressDbContext _context = context;
    private readonly DbSet<UserAddress> _dbSet = context.Set<UserAddress>();


    public async Task<List<IGrouping<string?, UserAddress>>> GroupByUserStateAsync()
    {
        var addresses = await _dbSet.ToListAsync();
        return addresses.GroupBy(ua => ua.State).ToList() ??
               throw new UserStateGroupException("There are no states to group in the database");
    }

    public async Task<List<IGrouping<int, UserAddress>>> GroupByZipCodeAsync()
    {
        return await _dbSet.GroupBy(ua => ua.ZipCode).ToListAsync();
    }


    public async Task<List<IGrouping<string?, UserAddress>>> GroupByCityAsync()
    {
        var addresses = await _dbSet.ToListAsync();
        return addresses.GroupBy(ua => ua.City).ToList() ??
               throw new CityGroupingNotAvailableException("There are no cities to group in the database");
    }

    public async Task<UserAddress> GetByUserAddressAsync(UserAddress userAddress)
    {
        return await _dbSet.FirstOrDefaultAsync(ua =>
                   ua.Address1 == userAddress.Address1 || ua.Address2 == userAddress.Address2) ??
               throw new InvalidOperationException();
    }

    public async Task<IEnumerable<UserAddress>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<IEnumerable<UserAddress>> FindAsync(
        Expression<Func<UserAddress, bool>> expression)
    {
        return await _dbSet.Where(expression).ToListAsync();
    }

    public async Task<UserAddress?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FirstOrDefaultAsync(i => i.Id == id).WaitAsync(TimeSpan.FromSeconds(30));
    }


    public async Task AddAsync(UserAddress entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<UserAddress> entities)
    {
        await _dbSet.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(UserAddress entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveAsync(UserAddress entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<UserAddress> UpdateUserAddress(UserAddressRequest userAddressDto)
    {
        // 1. Validate the user exists before updating the address
        var user = await userService.GetUserByIdAsync(userAddressDto.UserId);
        if (user == null)
        {
            throw new ArgumentException("User not found, cannot update address.");
        }

        // 2. Fetch existing address entity
        var existingAddress = await _dbSet.FirstOrDefaultAsync(i => i.Id == userAddressDto.Id);
        if (existingAddress == null)
        {
            throw new ArgumentNullException(nameof(existingAddress), "User Address does not exist");
        }

        // 3. Map updated fields from DTO to the entity
        mapper.Map(userAddressDto, existingAddress);

        // 4. Save changes to DB
        await _context.SaveChangesAsync();

        return existingAddress;
    }
}