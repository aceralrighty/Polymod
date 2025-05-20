using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TBD.Data;
using TBD.Models.DTOs;
using TBD.Repository.UserAddress;

namespace TBD.Services.UserAddress;

public class UserAddressService(GenericDatabaseContext context, IMapper mapper)
    : IUserAddressRepository, IUserAddressService
{
    protected readonly GenericDatabaseContext _context = context;
    private readonly DbSet<Models.Entities.UserAddress> _dbSet = context.Set<Models.Entities.UserAddress>();

    public async Task<List<IGrouping<string?, Models.Entities.UserAddress>>> GroupByUserStateAsync(Models.Entities.UserAddress userAddress)
    {
        return await _dbSet.GroupBy(ua => ua.State).ToListAsync() ??
               throw new NullReferenceException(nameof(userAddress));
    }

    public async Task<List<IGrouping<int, Models.Entities.UserAddress>>> GroupByZipCodeAsync(Models.Entities.UserAddress userAddress)
    {
        return await _dbSet.GroupBy(ua => ua.ZipCode ?? 0).ToListAsync();
    }

    public async Task<List<IGrouping<string?, Models.Entities.UserAddress>>> GroupByCityAsync(Models.Entities.UserAddress userAddress)
    {
        return await _dbSet.GroupBy(ua => ua.City).ToListAsync() ??
               throw new NullReferenceException(nameof(userAddress));
    }

    public async Task<Models.Entities.UserAddress> GetByUserAddressAsync(Models.Entities.UserAddress userAddress)
    {
        return await _dbSet.FirstOrDefaultAsync(ua =>
                   ua.Address1 == userAddress.Address1 || ua.Address2 == userAddress.Address2) ??
               throw new InvalidOperationException();
    }

    public async Task<IEnumerable<Models.Entities.UserAddress>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<IEnumerable<Models.Entities.UserAddress>> FindAsync(Expression<Func<Models.Entities.UserAddress, bool>> expression)
    {
        return await _dbSet.Where(expression).ToListAsync();
    }

    public async Task<Models.Entities.UserAddress> GetByIdAsync(Guid id)
    {
        return await _dbSet.FirstOrDefaultAsync(i => i.Id == id).WaitAsync(TimeSpan.FromSeconds(30)) ??
               throw new InvalidOperationException();
    }

    public async Task AddAsync(Models.Entities.UserAddress entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Models.Entities.UserAddress> entities)
    {
        await _dbSet.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Models.Entities.UserAddress entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveAsync(Models.Entities.UserAddress entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<Models.Entities.UserAddress> UpdateUserAddress(UserAddressRequest userAddressDto)
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