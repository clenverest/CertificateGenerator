namespace CertificateGenerator.Core.Models;

public sealed class ColumnInfo
{
    public int Index { get; }
    public string Name { get; }

    public ColumnInfo(int index, string name)
    {
        Index = index;
        Name = name;
    }
}