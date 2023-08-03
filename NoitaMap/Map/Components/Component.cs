namespace NoitaMap.Map.Components;

public abstract class Component
{
    public readonly string ComponentName;

    public bool Enabled;

    public string Tags = "";

    public Component(string name)
    {
        ComponentName = name;
    }

    public virtual void Deserialize(BinaryReader reader)
    {
        byte otherByte = reader.ReadByte();

        Enabled = reader.ReadBoolean();

        Tags = reader.ReadNoitaString() ?? "";

    }
}
