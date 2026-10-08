using System.Text.Json;
namespace UniflowPortal.Services;

public class ApiResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public int Status { get; init; }
    public JsonElement Root { get; init; }
    public JsonElement Data { get; init; }

    public static ApiResponse Fail(string m, int s = 0) => new() { Success = false, Message = m, Status = s };

    public static ApiResponse Parse(string raw, int status, bool httpOk)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement.Clone();
            var data = root; bool ok = httpOk; string? msg = null;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (Prop(root, "success") is { ValueKind: JsonValueKind.False }) ok = false;
                foreach (var n in new[] { "message", "error", "title" })
                    if (Prop(root, n) is { ValueKind: JsonValueKind.String } m) { msg = m.GetString(); break; }
                if (Prop(root, "data") is JsonElement d) data = d;
            }
            return new ApiResponse { Success = ok, Message = msg ?? (ok ? null : $"Request failed (HTTP {status})"), Status = status, Root = root, Data = data };
        }
        catch { return new ApiResponse { Success = httpOk, Message = httpOk ? null : $"Request failed (HTTP {status})", Status = status }; }
    }

    static JsonElement? Prop(JsonElement e, string name)
    {
        if (e.ValueKind != JsonValueKind.Object) return null;
        foreach (var p in e.EnumerateObject()) if (p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return p.Value;
        return null;
    }

    static JsonElement? FindArray(JsonElement e, int depth = 0)
    {
        if (e.ValueKind == JsonValueKind.Array) return e;
        if (e.ValueKind != JsonValueKind.Object || depth > 2) return null;
        foreach (var p in e.EnumerateObject()) if (p.Value.ValueKind == JsonValueKind.Array) return p.Value;
        foreach (var p in e.EnumerateObject()) if (p.Value.ValueKind == JsonValueKind.Object && FindArray(p.Value, depth + 1) is JsonElement a) return a;
        return null;
    }

    public List<JsonRow> Rows() => FindArray(Data) is JsonElement a ? a.EnumerateArray().Select(x => new JsonRow(x)).ToList() : new();
    public JsonRow? Item() => Data.ValueKind == JsonValueKind.Object ? new JsonRow(Data) : null;
    public int Total()
    {
        var t = Root.ValueKind == JsonValueKind.Object ? new JsonRow(Root).Int("total", "totalCount", "totalRecords", "totalItems", "totalRows") : 0;
        return t > 0 ? t : Rows().Count;
    }
}
