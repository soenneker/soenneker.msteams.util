using System.Linq;
using System.Text.Json.Serialization;
using Soenneker.Messages.MsTeams;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.AdaptiveCards.Util.Registrars;
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
    /// <param name="services">Service collection that receives the registration.</param>
    /// <returns>The same service collection, so additional registrations can be chained.</returns>
    public static IServiceCollection AddMsTeamsUtilAsSingleton(this IServiceCollection services)
    {
        AddTeamsJsonContext(services);
        services.AddAdaptiveCardsUtilAsSingleton()
                .AddServiceBusTransmitterAsSingleton()
                .AddMsTeamsSenderAsSingleton()
                .TryAddSingleton<IMsTeamsUtil, MsTeamsUtil>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="IMsTeamsUtil"/> as a scoped service. <para/>
    /// </summary>
    /// <param name="services">Service collection that receives the registration.</param>
    /// <returns>The same service collection, so additional registrations can be chained.</returns>
    public static IServiceCollection AddMsTeamsUtilAsScoped(this IServiceCollection services)
    {
        AddTeamsJsonContext(services);
        services.AddAdaptiveCardsUtilAsScoped().AddServiceBusTransmitterAsScoped().AddMsTeamsSenderAsSingleton().TryAddScoped<IMsTeamsUtil, MsTeamsUtil>();

        return services;
    }
    private static void AddTeamsJsonContext(IServiceCollection services)
    {
        if (services.Any(descriptor => descriptor.ServiceType == typeof(JsonSerializerContext)
            && ReferenceEquals(descriptor.ImplementationInstance, MsTeamsJsonContext.Default)))
            return;

        // Preserve the application's primary context (the last registration), while contributing Teams metadata.
        services.Insert(0, ServiceDescriptor.Singleton<JsonSerializerContext>(MsTeamsJsonContext.Default));
    }
}
