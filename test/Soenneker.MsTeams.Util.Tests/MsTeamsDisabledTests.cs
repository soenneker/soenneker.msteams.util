using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Soenneker.MsTeams.Util.Tests;

public class MsTeamsDisabledTests
{
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Disabled_teams_skips_all_send_overloads(bool useQueue)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Environment"] = "Local",
            ["MsTeams:Enabled"] = "false",
            ["MsTeams:Errors:Enabled"] = "true",
            ["MsTeams:UseQueue"] = useQueue.ToString()
        }).Build();

        // No downstream dependencies should be accessed while notifications are disabled.
        using var util = new MsTeamsUtil(configuration, null!, null!, NullLogger<MsTeamsUtil>.Instance, null!);

        await util.SendMessage("Missing metrics", "Errors");
        await util.SendMessage(new InvalidOperationException("Missing metrics"));
        await util.SendMessage("Missing metrics", null, new List<string> { "Buyer" }, "Errors");
        await util.SendMessageCard(null!, "Errors");
    }
}
