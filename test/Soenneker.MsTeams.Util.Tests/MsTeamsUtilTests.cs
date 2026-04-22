using Soenneker.MsTeams.Util.Abstract;
using Soenneker.Tests.HostedUnit;

namespace Soenneker.MsTeams.Util.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public class MsTeamsUtilTests : HostedUnitTest
{
    private readonly IMsTeamsUtil _util;

    public MsTeamsUtilTests(Host host) : base(host)
    {
        _util = Resolve<IMsTeamsUtil>(true);
    }

    [Test]
    public void Default()
    {

    }
}
