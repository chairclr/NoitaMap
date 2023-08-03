using System.Numerics;
using System.Reflection;
using NoitaMap.Map.Components;

namespace NoitaMap.Map.Entities;

public class Entity
{
    public string Name = "";

    public string Tags = "";

    public byte LifetimePhase;

    public string FileName = "";

    public Vector2 Position;

    public float Rotation;

    public Vector2 Scale;

    public readonly ComponentSchema Schema;

    public List<Component> Components = new List<Component>();

    public Entity(ComponentSchema schema)
    {
        Schema = schema;
    }

    public void Deserialize(BinaryReader reader)
    {
        Name = reader.ReadNoitaString()!;

        LifetimePhase = reader.ReadByte();

        FileName = reader.ReadNoitaString()!;

        Tags = reader.ReadNoitaString()!;

        float x = reader.ReadBESingle();
        float y = reader.ReadBESingle();

        Position = new Vector2(x, y);

        float scalex = reader.ReadBESingle();
        float scaley = reader.ReadBESingle();

        Scale = new Vector2(scalex, scaley);

        Rotation = reader.ReadBESingle();

        int componentCount = reader.ReadBEInt32();

        Components = new List<Component>(componentCount);

        Assembly assembly = typeof(Entity).Assembly;

        for (int i = 0; i < componentCount; i++)
        {
            string componentName = reader.ReadNoitaString()!;

            Type? type = assembly.GetType($"NoitaMap.Map.Components.{componentName}");

            if (type is null)
            {
                DummyComponent component = new DummyComponent(componentName, Schema);

                component.Deserialize(reader);

                Components.Add(component);
            }
            else
            {
                Component component = (Component)Activator.CreateInstance(type, componentName)!;

                component.Deserialize(reader);

                Components.Add(component);
            }
        }
    }
}
