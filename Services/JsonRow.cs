using System.Text;
using System.Text.Json;
namespace UniflowPortal.Services;

/// <summary>
/// Tolerant view over one JSON object: names match case-insensitively and ignore underscores,
/// so "fullName", "FullName" and "full_name" are the same. Nested objects are flattened (top level wins).
/// Swap for typed DTOs once you've pinned down the response shapes.
/// </summary>
public class JsonRow
{
    readonly Dictionary<string, JsonElement> _d = new();
    public JsonElement Element { get; }
    public List<(string Name, JsonElement Value)> Scalars { get; } = new();

    public JsonRow(JsonElement e)
    {
        Element = e;
        if (e.ValueKind != JsonValueKind.Object) return;
        foreach (var p in e.EnumerateObject())
            if (p.Value.ValueKind is JsonValueKind.Number or JsonValueKind.String or JsonValueKind.True or JsonValueKind.False)
                Scalars.Add((p.Name, p.Value));
        Add(e, 0);
    }
    void Add(JsonElement e, int depth)
    {
        if (e.ValueKind != JsonValueKind.Object || depth > 3) return;
        foreach (var p in e.EnumerateObject()) { var k = Norm(p.Name); if (!_d.ContainsKey(k)) _d[k] = p.Value; }
        foreach (var p in e.EnumerateObject()) if (p.Value.ValueKind == JsonValueKind.Object) Add(p.Value, depth + 1);
    }
    static string Norm(string s) => s.Replace("_", "").ToLowerInvariant();

    JsonElement? Find(string[] names)
    {
        foreach (var n in names)
            if (_d.TryGetValue(Norm(n), out var v) && v.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)) return v;
        return null;
    }
    public bool Has(params string[] n) => Find(n) is not null;
    public string Str(params string[] n) => Find(n) is JsonElement v ? (v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : v.ToString()) : "";
    public decimal Dec(params string[] n)
    {
        if (Find(n) is not JsonElement v) return 0;
        if (v.ValueKind == JsonValueKind.Number) return v.TryGetDecimal(out var d) ? d : (decimal)v.GetDouble();
        return decimal.TryParse(v.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var x) ? x : 0;
    }
    public int Int(params string[] n) => (int)Dec(n);
    public bool Bool(params string[] n) => Find(n) is JsonElement v && (v.ValueKind == JsonValueKind.True || v.ToString() is "1" or "true" or "True");

    /// <summary>All scalar values joined, for client-side text search.</summary>
    public string Text() { var sb = new StringBuilder(); foreach (var (_, v) in Scalars) sb.Append(v.ToString()).Append(' '); return sb.ToString(); }
}
