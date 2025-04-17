using System.Numerics;

namespace NoitaMap;

public class AreaEntity
{
    public Vector2 Position;

    public string XmlFilePath;

    public AreaEntity(string xmlFilePath, Vector2 position)
    {
        Position = position;
        XmlFilePath = xmlFilePath;
    }
}