using System;
using Microsoft.Extensions.Logging;

namespace Soenneker.MsTeams.Util;

internal class Logs
{
    public static readonly Action<ILogger, Exception?> LogSkippingChannel = LoggerMessage.Define(LogLevel.Debug, new EventId(1, nameof(LogSkippingChannel)),
        "Skipping sending MSTeams notification due to channel config");

    public static readonly Action<ILogger, string, Exception?> LogSkippingLocal = LoggerMessage.Define<string>(LogLevel.Debug,
        new EventId(2, nameof(LogSkippingLocal)), "Skipping sending service bus message because we're in local environment and {variable} was true");
}