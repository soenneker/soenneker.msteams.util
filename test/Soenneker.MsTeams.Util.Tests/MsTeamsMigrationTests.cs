using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Soenneker.AdaptiveCards.Util;
using Soenneker.Dtos.MsTeams.Card;
using Soenneker.Messages.MsTeams;
using Soenneker.MsTeams.Sender.Abstract;
using Soenneker.MsTeams.Util.Registrars;
using Soenneker.ServiceBus.Message;
using Soenneker.ServiceBus.Transmitter.Abstract;
using Soenneker.Utils.Json;

namespace Soenneker.MsTeams.Util.Tests;

public class MsTeamsMigrationTests
{
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task SendsGeneratedCardsThroughBothRoutes(bool useQueue)
    {
        var config = CreateConfig(useQueue);
        var transmitter = new CapturingTransmitter();
        var sender = new CapturingSender();
        using var util = new MsTeamsUtil(config, transmitter, new AdaptiveCardsUtil(config: config), NullLogger<MsTeamsUtil>.Instance, sender);
        using var cancellation = new CancellationTokenSource();
        await util.SendMessage("Report", "Errors", facts: new Dictionary<string, string?> { ["Name"] = "Ada" }, cancellationToken: cancellation.Token);

        MsTeamsCard card;
        if (useQueue)
        {
            MsTeamsMessage message = transmitter.Message ?? throw new Exception("Queue was not used.");
            if (sender.Card is not null || message.NewtonsoftSerialize || message.Channel != "Errors" || message.Queue != "msteams"
                || message.Type != "msteams" || transmitter.Token != cancellation.Token)
                throw new Exception("Queue routing or envelope changed.");

            // Ensure Teams registration preserves the application's primary JSON context.
            var services = new ServiceCollection();
            services.AddSingleton<JsonSerializerContext>(Soenneker.AdaptiveCards.Dtos.SchemaJsonContext.Default);
            services.AddMsTeamsUtilAsSingleton();
            services.AddMsTeamsUtilAsSingleton();
            using ServiceProvider provider = services.BuildServiceProvider();
            if (!ReferenceEquals(provider.GetRequiredService<JsonSerializerContext>(), Soenneker.AdaptiveCards.Dtos.SchemaJsonContext.Default))
                throw new Exception("The application's primary JSON context was replaced.");
            // Exercise the real transport serializer with the current Service Bus API.
            var serializer = new ServiceBusMessageUtil(config, NullLogger<ServiceBusMessageUtil>.Instance);
            var transport = serializer.BuildMessage(message, message.Type) ?? throw new Exception("Queue serialization failed.");
            MsTeamsMessage roundTrip = JsonUtil.Deserialize(transport.Body.ToString(), MsTeamsJsonContext.Default.MsTeamsMessage)!;
            if (roundTrip.Id != message.Id || roundTrip.Channel != message.Channel || roundTrip.CreatedAt != message.CreatedAt)
                throw new Exception("Envelope did not round trip.");
            card = roundTrip.MsTeamsCard;
        }
        else
        {
            if (transmitter.Message is not null || sender.Channel != "Errors" || sender.Token != cancellation.Token)
                throw new Exception("Immediate routing changed.");
            card = sender.Card ?? throw new Exception("Sender was not called.");
        }

        using JsonDocument json = JsonDocument.Parse(JsonUtil.Serialize(card, MsTeamsJsonContext.Default.MsTeamsCard));
        JsonElement attachment = json.RootElement.GetProperty("attachments")[0];
        JsonElement content = attachment.GetProperty("content");
        if (attachment.GetProperty("contentType").GetString() != "application/vnd.microsoft.card.adaptive"
            || content.GetProperty("type").GetString() != "AdaptiveCard"
            || content.GetProperty("body")[0].GetProperty("text").GetString() != "Report"
            || content.GetProperty("msteams").GetProperty("width").GetString() != "Full")
            throw new Exception("Teams wire format changed.");
    }

    [Test]
    public async Task ExplicitColumnsAndExceptionDefaultsReachSender()
    {
        var config = CreateConfig(false);
        var sender = new CapturingSender();
        using var util = new MsTeamsUtil(config, new CapturingTransmitter(), new AdaptiveCardsUtil(config: config), NullLogger<MsTeamsUtil>.Instance, sender);
        await util.SendMessage("Table", null, new[] { 42 }, [new AdaptiveCardColumn<int>("Count", item => item.ToString())], "Errors");
        if (sender.Card!.Attachments[0].Content!.Body.Value[2].AsVariant2().Columns.Value[0].Items.Value[0].AsVariant16().Text != "42")
            throw new Exception("Table selector was not applied.");
        await util.SendMessage(new InvalidOperationException("Details"));
        if (sender.Channel != "Errors" || sender.Card!.Attachments[0].Content!.Body.Value[0].AsVariant16().Text != "Exception thrown")
            throw new Exception("Exception defaults changed.");
        sender.Accept = false;
        try { await util.SendMessage("Rejected", "Errors"); }
        catch (InvalidOperationException) { return; }
        throw new Exception("Rejected sends must fail.");
    }

    private static IConfigurationRoot CreateConfig(bool useQueue) => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Environment"] = "Local", ["MsTeams:Enabled"] = "true", ["MsTeams:Errors:Enabled"] = "true", ["MsTeams:UseQueue"] = useQueue.ToString()
    }).Build();

    private sealed class CapturingSender : IMsTeamsSender
    {
        public MsTeamsCard? Card;
        public string? Channel;
        public CancellationToken Token;
        public bool Accept = true;
        public Task<bool> SendCard(MsTeamsCard card, string channel, CancellationToken cancellationToken = default)
        {
            Card = card;
            Channel = channel;
            Token = cancellationToken;
            return Task.FromResult(Accept);
        }
        public Task<bool> SendMessage(MsTeamsMessage message, CancellationToken cancellationToken = default) =>
            SendCard(message.MsTeamsCard, message.Channel, cancellationToken);
    }

    private sealed class CapturingTransmitter : IServiceBusTransmitter
    {
        public MsTeamsMessage? Message;
        public CancellationToken Token;
        public ValueTask SendMessage<T>(T msgModel, bool useQueue = true, CancellationToken cancellationToken = default) where T : Messages.Base.Message
        {
            Message = (MsTeamsMessage)(object)msgModel;
            Token = cancellationToken;
            return ValueTask.CompletedTask;
        }
        public ValueTask InternalSendMessage<T>(T msg, CancellationToken cancellationToken = default) where T : Messages.Base.Message => throw new NotSupportedException();
        public ValueTask SendMessages<T>(IList<T> msgModels, bool useQueue = true, CancellationToken cancellationToken = default) where T : Messages.Base.Message => throw new NotSupportedException();
        public ValueTask InternalSendMessages<T>(IList<T> msgModels, CancellationToken cancellationToken = default) where T : Messages.Base.Message => throw new NotSupportedException();
    }
}
