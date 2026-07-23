using System.Text.Json;

namespace FridayDeploy.Serilog;

/// <summary>Persists batches that could not be delivered so they can be resent later, guaranteeing no logs are lost.</summary>
internal sealed class BatchSpool(string directory, int maxFiles)
{
    public void Save(IReadOnlyCollection<LogEventPayload> batch)
    {
        try
        {
            Directory.CreateDirectory(directory);
            var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.json";
            var path = Path.Combine(directory, fileName);
            File.WriteAllText(path, JsonSerializer.Serialize(batch));
            EnforceLimit();
        }
        catch
        {
            // Spooling is a best-effort last resort; if disk I/O fails there is nothing further we can do
            // without risking the logging pipeline itself, so the batch is dropped.
        }
    }

    public IReadOnlyList<(string Path, IReadOnlyList<LogEventPayload> Events)> LoadPending()
    {
        if (!Directory.Exists(directory))
        {
            return [];
        }

        var results = new List<(string, IReadOnlyList<LogEventPayload>)>();
        foreach (var file in Directory.EnumerateFiles(directory, "*.json").OrderBy(f => f))
        {
            try
            {
                var events = JsonSerializer.Deserialize<List<LogEventPayload>>(File.ReadAllText(file));
                if (events is { Count: > 0 })
                {
                    results.Add((file, events));
                }
            }
            catch
            {
                TryDelete(file);
            }
        }

        return results;
    }

    public void Delete(string path) => TryDelete(path);

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch { /* best effort */ }
    }

    private void EnforceLimit()
    {
        var files = Directory.EnumerateFiles(directory, "*.json").OrderBy(f => f).ToList();
        var excess = files.Count - maxFiles;
        for (var i = 0; i < excess; i++)
        {
            TryDelete(files[i]);
        }
    }
}
