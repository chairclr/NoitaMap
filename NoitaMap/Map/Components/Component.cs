using NoitaMap.Map.Entities;

namespace NoitaMap.Map.Components;

public abstract class Component
{
    public Entity Entity;

    public readonly string ComponentName;

    public bool Enabled;

    public string Tags = "";

    public Component(Entity entity, string name)
    {
        Entity = entity;

        ComponentName = name;
    }

    public virtual void Deserialize(BinaryReader reader)
    {
        byte otherByte = reader.ReadByte();

        Enabled = reader.ReadBoolean();

        Tags = reader.ReadNoitaString() ?? "";

    }
}
