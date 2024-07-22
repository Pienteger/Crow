namespace Crow.Models;

public record JunkYard(string Path, long Size, JunkType JunkType)
{
    public string Icon => JunkType == JunkType.File ? "📄" : "📁";
}
