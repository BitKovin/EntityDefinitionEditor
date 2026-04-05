using EntityEditor.Models;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EntityEditor.Services;

public static class JsonProjectService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static void Save(string path, ProjectData data)
    {
        var json = JsonSerializer.Serialize(data, Options);
        File.WriteAllText(path, json);
    }

    public static ProjectData Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ProjectData>(json, Options)
               ?? new ProjectData();
    }
}
