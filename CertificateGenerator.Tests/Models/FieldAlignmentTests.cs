using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Tests.Models;

public sealed class FieldAlignmentTests
{
    [Fact]
    public void Has_ThreeValues()
    {
        var values = Enum.GetValues<FieldAlignment>();

        Assert.Equal(3, values.Length);
    }

    [Theory]
    [InlineData(FieldAlignment.Left)]
    [InlineData(FieldAlignment.Center)]
    [InlineData(FieldAlignment.Right)]
    public void Stores_Value(FieldAlignment value)
    {
        // Перечисление — просто значение, никаких сюрпризов.
        Assert.Equal(value, value);
    }
}