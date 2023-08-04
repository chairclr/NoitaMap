using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace NoitaMap.Map.Components;

public partial class ComponentSchema
{
    private static readonly Regex ILoveNollaGames = GenerateCoolRegex();

    private static Dictionary<string, ComponentSchema> SchemaCache = new Dictionary<string, ComponentSchema>();

    public Dictionary<string, ComponentVar[]> Vars = new Dictionary<string, ComponentVar[]>();

    private ComponentSchema(string name)
    {
        string text = File.ReadAllText(Path.Combine("Assets", "Schemas", $"{name}.xml"));

        // Nolla's xml isn't actualy really xml, and the attributes can include < and >, which is not supported by regular xml parses
        // We must replace < and > with &lt; and &gt; in order to actually parse it
        text = ILoveNollaGames.Replace(text, x => x.Value.Replace("<", "&lt;").Replace(">", "&gt;"));

        XmlSerializer serializer = new XmlSerializer(typeof(ComponentSchemaRoot));
        using StringReader reader = new StringReader(text);

        ComponentSchemaRoot schema = (ComponentSchemaRoot)serializer.Deserialize(reader)!;

        Vars = schema.Components.ToDictionary(x => x.Name, x => x.Vars.Select(x => new ComponentVar(x.Name, x.Type, x.Size)).ToArray());
    }

    public static ComponentSchema GetSchema(string name)
    {
        if (SchemaCache.TryGetValue(name, out ComponentSchema? schema))
        {
            return schema;
        }

        schema = new ComponentSchema(name);

        SchemaCache.Add(name, schema);

        return schema;
    }

    [GeneratedRegex("\".+?\"", RegexOptions.Compiled)]
    private static partial Regex GenerateCoolRegex();
}

public class ComponentVar
{
    public string Name;

    public string Type;

    public int Size;

    public ComponentVar(string name, string type, int size)
    {
        Name = name;
        Type = type;
        Size = size;
    }
}

[XmlRoot(ElementName = "Var")]
public class ComponentSchemaVarElement
{
    [NotNull]
    [XmlAttribute(AttributeName = "name")]
    public string? Name { get; set; }

    [XmlAttribute(AttributeName = "size")]
    public int Size { get; set; }

    [NotNull]
    [XmlAttribute(AttributeName = "type")]
    public string? Type { get; set; }
}

[XmlRoot(ElementName = "Component")]
public class ComponentSchemaComponentElement
{
    [NotNull]
    [XmlAttribute(AttributeName = "component_name")]
    public string? Name { get; set; }

    [NotNull]
    [XmlElement(ElementName = "Var")]
    public List<ComponentSchemaVarElement>? Vars { get; set; }
}

[XmlRoot(ElementName = "Schema")]
public class ComponentSchemaRoot
{
    [NotNull]
    [XmlAttribute(AttributeName = "hash")]
    public string? Hash { get; set; }

    [NotNull]
    [XmlElement(ElementName = "Component")]
    public List<ComponentSchemaComponentElement>? Components { get; set; }
}
