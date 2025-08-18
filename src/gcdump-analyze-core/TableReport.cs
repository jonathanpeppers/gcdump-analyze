using System.Text;

namespace DotNet.GCDump.Analyze;

/// <summary>
/// Represents a tabular report with named string columns and rows addressable by column name.
/// Values are objects and should provide meaningful ToString() for rendering.
/// </summary>
public sealed class TableReport
{
    /// <summary>Ordered list of column names to render.</summary>
    public IReadOnlyList<string> Columns { get; }

    /// <summary>Ordered list of rows in the table.</summary>
    public IReadOnlyList<TableRow> Rows { get; }

    public TableReport(IReadOnlyList<string> columns, IReadOnlyList<TableRow> rows)
    {
        Columns = columns ?? throw new ArgumentNullException(nameof(columns));
        Rows = rows ?? throw new ArgumentNullException(nameof(rows));
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);
        Markdown.Write(this, sw);
        return sb.ToString();
    }
}

/// <summary>
/// A single row within a <see cref="TableReport"/>, exposing values by column name.
/// </summary>
public sealed class TableRow : IReadOnlyDictionary<string, object?>
{
    private readonly IReadOnlyDictionary<string, object?> _values;

    public TableRow(IDictionary<string, object?> values)
    {
        if (values is null) throw new ArgumentNullException(nameof(values));
        // Store as a case-sensitive map; callers should use the exact column names from the report header.
        _values = new Dictionary<string, object?>(values, StringComparer.Ordinal);
    }

    public object? this[string key] => _values[key];
    public IEnumerable<string> Keys => _values.Keys;
    public IEnumerable<object?> Values => _values.Values;
    public int Count => _values.Count;
    public bool ContainsKey(string key) => _values.ContainsKey(key);
    public bool TryGetValue(string key, out object? value) => _values.TryGetValue(key, out value);
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _values.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _values.GetEnumerator();
}
