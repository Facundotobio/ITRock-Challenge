using ITRockChallenge.Application.Interfaces;
using ITRockChallenge.Domain;
using ITRockChallenge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ITRockChallenge.Infrastructure.Repositories;

public class EfTaskRepository : ITaskRepository
{
    private readonly ApplicationDbContext _context;

    public EfTaskRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<TodoTask> Items, int TotalCount)> GetPagedByUserIdAsync(
        string userId,
        int page,
        int pageSize,
        bool? completed,
        string? search,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var query = _context.Tasks.Where(t => t.UserId == userId);

        if (completed.HasValue)
        {
            query = query.Where(t => t.Completed == completed.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var cleanSearch = search.Trim().ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(cleanSearch)
                                  || t.Description.ToLower().Contains(cleanSearch));
        }

        if (fromDate.HasValue)
        {
            var utcFromDate = DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc);
            query = query.Where(t => t.CreatedAt >= utcFromDate);
        }

        if (toDate.HasValue)
        {
            var utcToDate = DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc).Date.AddDays(1).AddTicks(-1);
            query = query.Where(t => t.CreatedAt <= utcToDate);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<TodoTask> AddAsync(TodoTask task)
    {
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        return task;
    }

    public async Task<TodoTask?> FindByIdAsync(Guid id) =>
        await _context.Tasks.FindAsync(id);

    public async Task UpdateAsync(TodoTask task)
    {
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(TodoTask task)
    {
        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<TodoTask> tasks)
    {
        _context.Tasks.AddRange(tasks);
        await _context.SaveChangesAsync();
    }
}
