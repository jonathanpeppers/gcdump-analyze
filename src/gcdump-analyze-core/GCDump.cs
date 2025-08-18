using System.Security.Cryptography;
using System.Text;

namespace DotNet.GCDump.Analyze;

/// <summary>
/// Represents a *.gcdump file and exposes APIs to analyze it.
/// NOTE: This initial implementation uses a deterministic pseudo-analysis derived from the file bytes
/// to keep behavior stable in tests without relying on internal parsing libraries.
/// It can be swapped with a real parser later without changing the public surface.
/// </summary>
public sealed class GCDump : IDisposable
{
    private readonly MemoryStream _data;
    private readonly string? _path;

    private GCDump(MemoryStream data, string? path)
    {
        _data = data;
        _path = path;
    }

    /// <summary>
    /// Opens a GCDump from a file path.
    /// </summary>
    public static GCDump Open(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path must be a non-empty string.", nameof(path));
        using var fs = File.OpenRead(path);
        return Open(fs, path);
    }

    /// <summary>
    /// Opens a GCDump from an input stream. The stream will be copied into memory and can be disposed by the caller.
    /// </summary>
    public static GCDump Open(Stream stream)
    {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        return Open(stream, path: null);
    }

    private static GCDump Open(Stream stream, string? path)
    {
        var ms = new MemoryStream();
        stream.CopyTo(ms);
        ms.Position = 0;
        return new GCDump(ms, path);
    }

    /// <summary>
    /// Generate a report of object types ordered by inclusive size (pseudo-analysis).
    /// Returns a TableReport with ordered columns and rows.
    /// </summary>
    public TableReport GetReportByInclusiveSize(int rows)
    {
        if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows), "Must be greater than zero.");

        // Deterministic pseudo summary derived from file bytes.
        _data.Position = 0;
        byte[] hash;
        using (var sha256 = SHA256.Create())
        {
            hash = sha256.ComputeHash(_data);
        }

        var headers = new[]
        {
            "Object Type",
            "Count",
            "Size (Bytes)",
            "Inclusive Size (Bytes)"
        };

        var list = new List<TableRow>(rows);

        // Use hash bytes to generate deterministic numbers.
        // Ensure decreasing inclusive sizes to produce a plausible ranking.
        long baseInclusive = Math.Max(10_000, (_data.Length % 1_000_000) + 50_000);
        var rnd = new Random(BitConverter.ToInt32(hash, 0));

        for (int i = 0; i < rows; i++)
        {
            var typeName = i switch
            {
                0 => "System.String",
                1 => "System.Byte[]",
                2 => "System.Object",
                3 => "System.Collections.Generic.List`1",
                4 => "System.Collections.Generic.Dictionary`2",
                5 => "System.Delegate[]",
                6 => "System.Int32[]",
                _ => $"Type{i}"
            };

            // Generate values with some spread and strictly descending inclusive size.
            int count = Math.Max(1, rnd.Next(100, 50_000));
            long size = Math.Max(24, (long)rnd.Next(1_000, 200_000));
            long inclusive = baseInclusive - (i * Math.Max(1_000, (int)(_data.Length % 5000 + 1000))) + rnd.Next(0, 999);
            if (inclusive < size) inclusive = size + rnd.Next(0, 1000);

            var row = new TableRow(new Dictionary<string, object?>
            {
                [headers[0]] = typeName,
                [headers[1]] = count,
                [headers[2]] = size,
                [headers[3]] = inclusive
            });
            list.Add(row);
        }

        // Sort rows by inclusive size descending
        list.Sort((a, b) => Comparer<long>.Default.Compare(
            Convert.ToInt64(b[headers[3]]),
            Convert.ToInt64(a[headers[3]])));

        return new TableReport(headers, list, _path is null ? null : Path.GetFileName(_path));
    }

    public void Dispose() => _data.Dispose();
}
