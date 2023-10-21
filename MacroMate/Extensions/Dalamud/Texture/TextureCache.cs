using System;
using System.Collections.Concurrent;
using System.Threading;
using Dalamud.Interface.Internal;
using Dalamud.Plugin.Services;

namespace MacroMate.Extensions.Dalamud;

public class TextureCache : IDisposable {
    private ITextureProvider textureProvider;

    /// The number of active loading tasks.
    private int loadingTasks = 0;

    private CancellationTokenSource cancellationTokenSource = new();

    private readonly ConcurrentQueue<uint> loadingQueue = new();
    private readonly ConcurrentDictionary<uint, bool> loading = new();
    private readonly ConcurrentDictionary<uint, IDalamudTextureWrap> icons = new();

    public TextureCache(ITextureProvider textureProvider) {
        this.textureProvider = textureProvider;
        Env.Framework.Update += Update;
    }

    public void Dispose() {
        cancellationTokenSource.Dispose();
        foreach (var icon in icons.Values) {
            icon.Dispose();
        }
    }

    public void Clear() {
        cancellationTokenSource.Cancel();

        loadingQueue.Clear();
        loading.Clear();

        foreach (var icon in icons.Values) {
            icon.Dispose();
        }
        icons.Clear();

        cancellationTokenSource = new CancellationTokenSource();
        loadingTasks = 0;
    }

    private void Update(IFramework framework) {
        if (loadingQueue.Count == 0) { return; }

        while (loadingTasks < 10 && loadingQueue.TryDequeue(out var iconId)) {
            Interlocked.Increment(ref loadingTasks);

            var cancellationToken = cancellationTokenSource.Token;
            Env.Framework.RunOnTick(() => {
                cancellationToken.ThrowIfCancellationRequested();

                var icon = textureProvider.GetIcon(iconId);
                if (icon != null) {
                    icons[iconId] = icon;
                    loading.TryRemove(iconId, out var _);
                }
                Interlocked.Decrement(ref loadingTasks);
            }, cancellationToken: cancellationToken);
        }
    }

    /// Returns the Texture Icon for `iconId` if loaded, or a placeholder if not.
    public IDalamudTextureWrap? GetIcon(uint iconId, uint defaultValue = 66001) {
        // Start loading the icon if we don't already have it and aren't already loading it
        if (!icons.ContainsKey(iconId) && !loading.ContainsKey(iconId)) {
            loading[iconId] = true;
            loadingQueue.Enqueue(iconId);
        }

        if (icons.TryGetValue(iconId, out var icon)) {
            return icon;
        }

        return textureProvider.GetIcon(defaultValue, keepAlive: true);
    }
}
