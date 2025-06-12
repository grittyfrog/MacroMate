using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MacroMate.Subscription;

public class SubscriptionTaskDetails {
    public Guid Id = Guid.NewGuid();
    public DateTimeOffset CreatedAt = DateTimeOffset.UtcNow;
    public string? Message { get; set; } = null;
    public bool IsError { get; set; } = false;

    public string Summary {
        get {
            if (Children.Count > 0) {
                return Message + "  •  " + Children.OrderBy(c => c.CreatedAt).LastOrDefault()?.Summary ?? "";
            } else {
                return Message ?? "";
            }
        }
    }

    private int _loadingCount = 0;
    public int LoadingCount { get => _loadingCount; }

    public bool IsLoading => LoadingCount > 0 || Children.Any(child => child.IsLoading);
    public string? LastLoadingMessage => Children.LastOrDefault(c => c.IsLoading)?.Message;
    public bool HasError => IsError || Children.Any(child => child.IsError);
    public int ErrorCount => (IsError ? 1 : 0) + Children.Sum(child => child.ErrorCount);

    public async Task<T> Loading<T>(Func<Task<T>> block) {
        return await Catching(async () => {
            try {
                Interlocked.Increment(ref _loadingCount);
                return await block();
            } finally {
                Interlocked.Decrement(ref _loadingCount);
            }
        });
    }

    public async Task Loading(Func<Task> block) {
        await Catching(async () => {
            try {
                Interlocked.Increment(ref _loadingCount);
                await block();
            } finally {
                Interlocked.Decrement(ref _loadingCount);
            }
        });
    }

    public async Task Catching(Func<Task> block) {
        try {
            await block();
        } catch (Exception ex) {
            var errChainParent = Child(ex.Message);
            var inner = ex.InnerException;
            while (inner != null) {
                errChainParent = errChainParent.Child(inner.Message);
                inner = inner.InnerException;
            }
            throw;
        }
    }

    public async Task<T> Catching<T>(Func<Task<T>> block) {
        try {
            return await block();
        } catch (Exception ex) {
            var errChainParent = Child(ex.Message);
            var inner = ex.InnerException;
            while (inner != null) {
                errChainParent = errChainParent.Child(inner.Message);
                inner = inner.InnerException;
            }
            throw;
        }
    }

    public SubscriptionTaskDetails Child(string message) {
        var childScope = new SubscriptionTaskDetails { Message = message };
        Children.Add(childScope);
        return childScope;
    }

    public ConcurrentBag<SubscriptionTaskDetails> Children = new();
}
