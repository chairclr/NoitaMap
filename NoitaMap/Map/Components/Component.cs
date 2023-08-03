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
        Console.WriteLine($"{reader.BaseStream.Position}: Component: {ComponentName}");
        byte otherByte = reader.ReadByte();

        Enabled = reader.ReadBoolean();

        Tags = reader.ReadNoitaString() ?? "";

    }
}
