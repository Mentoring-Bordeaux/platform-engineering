using System.Collections.Concurrent;

public static class ProvisionStore
{
    public record Job(string Status, object? Outputs = null, string? Error = null);

    private static readonly ConcurrentDictionary<string, Job> _jobs = new();

    public static string Create()
    {
        var id = Guid.NewGuid().ToString("N");
        _jobs[id] = new Job("Running");
        return id;
    }

    public static void Succeed(string id, object outputs) => _jobs[id] = new Job("Succeeded", outputs);
    public static void Fail(string id, string error) => _jobs[id] = new Job("Failed", null, error);

    public static bool TryGet(string id, out Job job) => _jobs.TryGetValue(id, out job!);
}
