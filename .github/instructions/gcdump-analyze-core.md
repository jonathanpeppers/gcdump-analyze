# Core Library

* Uses `Microsoft.Diagnostics.Monitoring.EventPipe` from the `dotnet-tools` feed:
  * https://dev.azure.com/dnceng/public/_artifacts/feed/dotnet-tools/NuGet/Microsoft.Diagnostics.Monitoring.EventPipe/overview/9.0.637302

* `GCDump` type represents a `*.gcdump` file. APIs for opening the file with a `string path` or `Stream`.

* `GetReportByInclusiveSize(int rows)` returns a `TableReport` sorted by Inclusive Size (retained) with columns:
  * Object Type, Count, Size (Bytes), Inclusive Size (Bytes)

* `GetReportBySize(int rows)` returns a `TableReport` sorted by shallow Size (Bytes) with the same columns.
* `GetReportByCount(int rows)` returns a `TableReport` sorted by Count with the same columns.
* `GetReportByName(string nameContains)` returns a `TableReport` filtered to rows whose Object Type contains the substring (case-insensitive), sorted by Inclusive Size.
* `GetPathsToRoot(string nameContains)` returns a markdown-ready table representing the hot paths to GC roots for matching types; columns: `Object Type` (indented) and `Reference Count`.

| Object Type                                                                                                          |  Count |   Size (Bytes) | Inclusive Size (Bytes) |
|----------------------------------------------------------------------------------------------------------------------|-------:|---------------:|-----------------------:|
| Dictionary<Microsoft.Maui.Controls.BindableProperty, Microsoft.Maui.Controls.BindableObject.BindablePropertyContext> |  4,025 |        322,000 |             33,105,512 |
| Microsoft.Maui.Controls.BindableObject.BindablePropertyContext                                                       | 32,279 |      1,807,624 |             30,969,496 |
| Delegate[]                                                                                                           |    793 |         76,904 |             28,169,872 |
| List<Microsoft.Maui.Controls.Element>                                                                                |  2,368 |         75,776 |             27,681,536 |
| Microsoft.Maui.Controls.Element[]                                                                                    |    718 |         50,624 |             27,667,744 |
| Microsoft.Maui.Controls.SetterSpecificityList                                                                        | 32,279 |      4,131,712 |             25,267,016 |
| Entry<Microsoft.Maui.Controls.BindableProperty, Microsoft.Maui.Controls.BindableObject.BindablePropertyContext>      |  1,025 |      1,101,248 |             24,822,216 |
| Microsoft.Maui.Controls.Grid                                                                                         |    208 |        174,720 |             21,786,456 |

Where this is an example if passed 8 rows.

* `Markdown` is a static class for rendering markdown tables from a `TableReport`.

* `Markdown.Write(TableReport, System.IO.TextWriter)` writes the report to the passed in `TextWriter`.

## Implementation notes

* Parsing: uses `Microsoft.Diagnostics.Tracing` (`GCHeapDump`) to load the dump into a `MemoryGraph`.
* Retained size: computed via `SpanningTree` dominators; attribution avoids double-counting within same-type chains.
* Output: `TableReport` is stable and used by CLI, tests, and MCP server tools.
* Sorting: Internally uses a `SortMode` enum (InclusiveSize, Size, Count) rather than booleans.


## Types

```csharp
public sealed class TableReport
{
  public IReadOnlyList<string> Columns { get; }
  public IReadOnlyList<TableRow> Rows { get; }
}

public sealed class TableRow : IReadOnlyDictionary<string, object?>
{
  // Access values by column name; values should provide meaningful ToString().
}
```
