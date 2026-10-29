namespace Crow.Models;

public record JunkYard(string Path, long Size, JunkType JunkType, string? Label = null)
{
    public string Icon => JunkType == JunkType.File ? "📄" : "📁";
}
