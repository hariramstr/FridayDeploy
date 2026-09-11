using Serilog.Events;

namespace FridayDeploy.Serilog;

/// <summary>Converts a <see cref="LogEventPropertyValue"/> to a plain .NET object suitable for JSON serialization.</summary>
internal static class LogEventPropertyFlattener
{
    public static object? Flatten(LogEventPropertyValue value) => value switch
    {
        ScalarValue s     => s.Value,
        SequenceValue q   => q.Elements.Select(Flatten).ToList(),
        StructureValue t  => t.Properties.ToDictionary(p => p.Name, p => Flatten(p.Value)),
        DictionaryValue d => d.Elements.ToDictionary(kv => kv.Key.ToString(), kv => Flatten(kv.Value)),
        _                 => value.ToString(),
    };
}
