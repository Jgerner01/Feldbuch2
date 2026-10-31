using Feldbuch;
using Xunit;

namespace Feldbuch.Tests;

public class ProgrammVersionTests
{
    [Fact]
    public void Version_EnthaeltKeinenCommitHash()
    {
        Assert.Matches(@"^\d+\.\d+\.\d+$", ProgrammVersion.Version);
    }

    [Fact]
    public void BuildNummer_IstInDieProgrammdateiEingebettet()
    {
        Assert.True(int.TryParse(ProgrammVersion.BuildNummer, out int nummer));
        Assert.True(nummer > 0);
    }
}
