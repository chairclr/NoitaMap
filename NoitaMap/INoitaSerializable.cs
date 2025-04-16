namespace NoitaMap;

public interface INoitaSerializable
{
    void Deserialize(BinaryReader reader);

    void Serialize(BinaryWriter writer);
}