using System.Text.Json;
using System.Text.Json.Serialization;

namespace NoitaMap.Map.Components;

public class ObjectSchema
{
    private static Dictionary<string, ObjectSchema> SchemaCache = new Dictionary<string, ObjectSchema>();

    public string Name;

    public List<ObjectSchemaField> SchemaFields = new List<ObjectSchemaField>();

    private ObjectSchema(string className)
    {
        if (className.StartsWith("class "))
        {
            className = className[6..];
        }

        ObjectSchemaRoot root = JsonSerializer.Deserialize<ObjectSchemaRoot>(File.ReadAllText($"Assets/Objects/{className}.json"))!;

        Name = root.Name;

        SchemaFields = root.Fields;
    }

    public static ObjectSchema GetSchema(string name)
    {
        if (SchemaCache.TryGetValue(name, out ObjectSchema? schema))
        {
            return schema;
        }

        schema = new ObjectSchema(name);

        SchemaCache.Add(name, schema);

        return schema;
    }

    public class ObjectSchemaField
    {
        [JsonPropertyName("offset")]
        public int? Offset { get; set; }

        [JsonPropertyName("comment")]
        public string Comment { get; set; } = string.Empty;

        [JsonPropertyName("example_range_min")]
        public int? ExampleRangeMin { get; set; }

        [JsonPropertyName("example_range_max")]
        public double? ExampleRangeMax { get; set; }

        [JsonPropertyName("kind")]
        public string Kind { get; set; } = string.Empty;

        [JsonPropertyName("default_value")]
        public string DefaultValue { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public int? Size { get; set; }

        [JsonPropertyName("raw_type")]
        public string RawType { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    sealed class ObjectSchemaRoot
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("base")]
        public string Base { get; set; } = string.Empty;

        [JsonPropertyName("fields")]
        public List<ObjectSchemaField> Fields { get; set; } = new List<ObjectSchemaField>();
    }
}
