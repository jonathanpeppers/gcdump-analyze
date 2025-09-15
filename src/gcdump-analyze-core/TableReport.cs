using System.Text;

namespace DotNet.GCDump.Analyze;

/// <summary>
/// Represents the type of a column for formatting purposes.
/// </summary>
public enum ColumnType
{
    /// <summary>Text column - left-aligned.</summary>
    Text,
    /// <summary>Numeric column - right-aligned.</summary>
    Numeric
}

/// <summary>
/// Represents column metadata including name and type.
/// </summary>
public sealed class ColumnInfo
{
    /// <summary>The display name of the column.</summary>
    public string Name { get; }

    /// <summary>The type of the column for formatting purposes.</summary>
    public ColumnType Type { get; }

    public ColumnInfo(string name, ColumnType type)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type;
    }
}

/// <summary>
/// Represents a tabular report with named string columns and rows addressable by column name.
/// Values are objects and should provide meaningful ToString() for rendering.
/// </summary>
public sealed class TableReport
{
    /// <summary>Ordered list of column names to render.</summary>
    public IReadOnlyList<string> Columns { get; }

    /// <summary>Column metadata including type information for formatting.</summary>
    public IReadOnlyList<ColumnInfo> ColumnInfos { get; }

    /// <summary>Ordered list of rows in the table.</summary>
    public IReadOnlyList<TableRow> Rows { get; }

    /// <summary>Optional tree structure for hierarchical data rendering.</summary>
    public IReadOnlyList<TableRow> TreeNodes { get; }

    /// <summary>True if this report represents hierarchical tree data.</summary>
    public bool IsTreeReport { get; }

    public TableReport(IReadOnlyList<string> columns, IReadOnlyList<TableRow> rows)
        : this(columns.Select(c => new ColumnInfo(c, ColumnType.Text)).ToList(), rows)
    {
    }

    public TableReport(IReadOnlyList<ColumnInfo> columnInfos, IReadOnlyList<TableRow> rows)
    {
        ColumnInfos = columnInfos ?? throw new ArgumentNullException(nameof(columnInfos));
        Columns = columnInfos.Select(c => c.Name).ToList();
        Rows = rows ?? throw new ArgumentNullException(nameof(rows));
        TreeNodes = Array.Empty<TableRow>();
        IsTreeReport = false;
    }

    public static TableReport CreateTreeReport(IReadOnlyList<ColumnInfo> columnInfos, IReadOnlyList<TableRow> treeNodes)
    {
        return new TableReport(columnInfos, treeNodes, isTreeReport: true);
    }

    private TableReport(IReadOnlyList<ColumnInfo> columnInfos, IReadOnlyList<TableRow> treeNodes, bool isTreeReport)
    {
        ColumnInfos = columnInfos ?? throw new ArgumentNullException(nameof(columnInfos));
        Columns = columnInfos.Select(c => c.Name).ToList();
        Rows = Array.Empty<TableRow>();
        TreeNodes = treeNodes ?? throw new ArgumentNullException(nameof(treeNodes));
        IsTreeReport = isTreeReport;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);
        Markdown.Write(this, sw);
        return sb.ToString();
    }

    /// <summary>
    /// Convert the report to a tree-style string representation.
    /// </summary>
    public string ToTreeString()
    {
        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);
        Markdown.WriteTree(this, sw);
        return sb.ToString();
    }
}

/// <summary>
/// A single row within a <see cref="TableReport"/>, exposing values by column name.
/// Can also represent a tree node with hierarchical children.
/// </summary>
public sealed class TableRow : IReadOnlyDictionary<string, object?>
{
    private readonly IReadOnlyDictionary<string, object?> _values;

    /// <summary>Child nodes under this row (for hierarchical data).</summary>
    public IReadOnlyList<TableRow> Children { get; }

    public TableRow(IDictionary<string, object?> values)
        : this(values, null)
    {
    }

    public TableRow(IDictionary<string, object?> values, IReadOnlyList<TableRow>? children)
    {
        if (values is null) throw new ArgumentNullException(nameof(values));
        // Store as a case-sensitive map; callers should use the exact column names from the report header.
        _values = new Dictionary<string, object?>(values, StringComparer.Ordinal);
        Children = children ?? Array.Empty<TableRow>();
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
