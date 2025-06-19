using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.AdaptiveCard.Util.Registrars;
using Soenneker.MsTeams.Sender.Registrars;
using Soenneker.MsTeams.Util.Abstract;
using Soenneker.ServiceBus.Transmitter.Registrars;

namespace Soenneker.MsTeams.Util.Registrars;

/// <summary>
/// A centralized utility for sending rich, configurable Adaptive Card messages to Microsoft Teams channels via a service bus, with environment-aware filtering and dynamic content generation.
/// </summary>
public static class MsTeamsUtilRegistrar
{
    /// <summary>
    /// Adds <see cref="IMsTeamsUtil"/> as a singleton service. <para/>
    /// </summary>
    public static IServiceCollection AddMsTeamsUtilAsSingleton(this IServiceCollection services)
    {
        services.AddAdaptiveCardUtilAsSingleton()
                .AddServiceBusTransmitterAsSingleton()
                .AddMsTeamsSenderAsSingleton()
                .TryAddSingleton<IMsTeamsUtil, MsTeamsUtil>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="IMsTeamsUtil"/> as a scoped service. <para/>
    /// </summary>
    public static IServiceCollection AddMsTeamsUtilAsScoped(this IServiceCollection services)
    {
        services.AddAdaptiveCardUtilAsScoped().AddServiceBusTransmitterAsScoped().AddMsTeamsSenderAsSingleton().TryAddScoped<IMsTeamsUtil, MsTeamsUtil>();

        return services;
    }
}