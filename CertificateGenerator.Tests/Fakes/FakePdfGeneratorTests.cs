using CertificateGenerator.Core.Models;
using CertificateGenerator.Tests.Fakes;

namespace CertificateGenerator.Tests.Fakes;

public sealed class FakePdfGeneratorTests
{
    [Fact]
    public void GenerateCertificate_ReturnsResult()
    {
        var fake = new FakePdfGenerator
        {
            Result = new byte[] { 1, 2, 3 }
        };

        var result = fake.GenerateCertificate(
            "template.png",
            Array.Empty<FieldPlacement>(),
            new Dictionary<string, string>());

        Assert.Equal(new byte[] { 1, 2, 3 }, result);
    }

    [Fact]
    public void GenerateCertificate_StoresArguments()
    {
        var fake = new FakePdfGenerator();
        var placements = new List<FieldPlacement>
        {
            new("ФИО", 100, 200, 48, "Arial", FieldAlignment.Center)
        };
        var values = new Dictionary<string, string> { ["ФИО"] = "Иванов Иван" };

        fake.GenerateCertificate("template.png", placements, values);

        Assert.Equal("template.png", fake.LastTemplatePath);
        Assert.Same(placements, fake.LastPlacements);
        Assert.Same(values, fake.LastFieldValues);
        Assert.Equal(1, fake.CallCount);
    }

    [Fact]
    public void GenerateCertificate_IncrementsCallCount()
    {
        var fake = new FakePdfGenerator();

        fake.GenerateCertificate("a.png", Array.Empty<FieldPlacement>(), new Dictionary<string, string>());
        fake.GenerateCertificate("b.png", Array.Empty<FieldPlacement>(), new Dictionary<string, string>());

        Assert.Equal(2, fake.CallCount);
    }

    [Fact]
    public void GenerateCertificate_Throws_WhenThrowOnGenerateSet()
    {
        var expected = new IOException("Тестовая ошибка");
        var fake = new FakePdfGenerator
        {
            ThrowOnGenerate = expected
        };

        var actual = Assert.Throws<IOException>(() =>
            fake.GenerateCertificate(
                "template.png",
                Array.Empty<FieldPlacement>(),
                new Dictionary<string, string>()));

        Assert.Same(expected, actual);
    }
}