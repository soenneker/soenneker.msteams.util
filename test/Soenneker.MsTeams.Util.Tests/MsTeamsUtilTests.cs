using Soenneker.MsTeams.Util.Abstract;
using Soenneker.Tests.FixturedUnit;
using Xunit;

namespace Soenneker.MsTeams.Util.Tests;

[Collection("Collection")]
public class MsTeamsUtilTests : FixturedUnitTest
{
    private readonly IMsTeamsUtil _util;

    public MsTeamsUtilTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _util = Resolve<IMsTeamsUtil>(true);
    }

    [Fact]
    public void Default()
    {

    }
}
