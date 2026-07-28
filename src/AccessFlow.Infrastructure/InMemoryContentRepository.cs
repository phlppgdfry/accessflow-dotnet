using System.Collections.Concurrent;
using AccessFlow.Application;
using AccessFlow.Domain;

namespace AccessFlow.Infrastructure;

public sealed class InMemoryContentRepository : IContentRepository
{
    private readonly ConcurrentDictionary<Guid, ContentItem> items = new();

    public Task AddAsync(ContentItem item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!items.TryAdd(item.Id, item))
        {
            throw new InvalidOperationException("Dit content item bestaat al.");
        }

        return Task.CompletedTask;
    }

    public Task<ContentItem?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        items.TryGetValue(id, out var item);
        return Task.FromResult(item);
    }

    public Task<IReadOnlyList<ContentItem>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<ContentItem> result = items.Values
            .OrderByDescending(item => item.SubmittedAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task UpdateAsync(ContentItem item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        items[item.Id] = item;
        return Task.CompletedTask;
    }
}
