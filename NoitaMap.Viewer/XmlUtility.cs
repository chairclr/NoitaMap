using System.Xml.Serialization;

namespace NoitaMap;

public static class XmlUtility
{
    public static T LoadXml<T>(string xmlContent)
    {
        XmlSerializer gfxSerializer = new XmlSerializer(typeof(T));
        using StringReader xmlReader = new StringReader(xmlContent);

        return (T)gfxSerializer.Deserialize(xmlReader)!;
    }
}
