using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TBD.Data;
using TBD.Interfaces.Services;
using TBD.Models.DTOs;
using TBD.Models.Entities;
using TBD.Repository.UserAddress;

namespace TBD.Services;

public class UserAddressService(GenericDatabaseContext context, IMapper mapper)
    : IUserAddressRepository, IUserAddressService
{
    protected readonly GenericDatabaseContext _context = context;
    private readonly DbSet<UserAddress> _dbSet = context.Set<UserAddress>();

    public async Task<List<IGrouping<string?, UserAddress>>> GroupByUserStateAsync(UserAddress userAddress)
    {
        return await _dbSet.GroupBy(ua => ua.State).ToListAsync() ??
               throw new NullReferenceException(nameof(userAddress));
    }

    public async Task<List<IGrouping<int, UserAddress>>> GroupByZipCodeAsync(UserAddress userAddress)
    {
        return await _dbSet.GroupBy(ua => ua.ZipCode ?? 0).ToListAsync();
    }

    public async Task<List<IGrouping<string?, UserAddress>>> GroupByCityAsync(UserAddress userAddress)
    {
        return await _dbSet.GroupBy(ua => ua.City).ToListAsync() ??
               throw new NullReferenceException(nameof(userAddress));
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

    public async Task<IEnumerable<UserAddress>> FindAsync(Expression<Func<UserAddress, bool>> expression)
    {
        return await _dbSet.Where(expression).ToListAsync();
    }

    public async Task<UserAddress> GetByIdAsync(Guid id)
    {
        return await _dbSet.FirstOrDefaultAsync(i => i.Id == id).WaitAsync(TimeSpan.FromSeconds(30)) ??
               throw new InvalidOperationException();
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
        var existingAddress = await _dbSet.FirstOrDefaultAsync(i => i.Id == userAddressDto.Id);
        if (existingAddress == null)
        {
            throw new ArgumentNullException(nameof(existingAddress), "User Address does not exist");
        }

        mapper.Map(userAddressDto, existingAddress);
        await _context.SaveChangesAsync();
        return existingAddress;
    }
}