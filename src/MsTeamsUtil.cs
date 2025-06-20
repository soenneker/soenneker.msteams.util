﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Soenneker.AdaptiveCard.Util.Abstract;
using Soenneker.Dtos.AdaptiveCard.Attachments;
using Soenneker.Dtos.MsTeams.Card;
using Soenneker.Enums.DeployEnvironment;
using Soenneker.Extensions.Configuration;
using Soenneker.Extensions.Task;
using Soenneker.Messages.MsTeams;
using Soenneker.MsTeams.Sender.Abstract;
using Soenneker.MsTeams.Util.Abstract;
using Soenneker.ServiceBus.Transmitter.Abstract;

namespace Soenneker.MsTeams.Util;

///<inheritdoc cref="IMsTeamsUtil"/>
public sealed class MsTeamsUtil : IMsTeamsUtil
{
    private readonly IConfiguration _config;
    private readonly IServiceBusTransmitter _serviceBusTransmitter;
    private readonly ILogger<MsTeamsUtil> _logger;
    private readonly IAdaptiveCardUtil _adaptiveCardUtil;
    private readonly IMsTeamsSender _msTeamsSender;

    private readonly bool _useQueue;

    public MsTeamsUtil(IConfiguration config, IServiceBusTransmitter servicesBusTransmitter, IAdaptiveCardUtil adaptiveCardUtil, ILogger<MsTeamsUtil> logger,
        IMsTeamsSender msTeamsSender)
    {
        _logger = logger;
        _msTeamsSender = msTeamsSender;
        _config = config;
        _serviceBusTransmitter = servicesBusTransmitter;
        _adaptiveCardUtil = adaptiveCardUtil;

        _useQueue = config.GetValue<bool>("MsTeams:UseQueue");
    }

    public ValueTask SendMessage(string title, string channel, string? summary = null, Dictionary<string, string?>? facts = null, Exception? e = null,
        string? additionalBody = null, bool skipLocal = false, CancellationToken cancellationToken = default)
    {
        if (ShouldSkipBecauseLocal(skipLocal))
            return ValueTask.CompletedTask;

        if (!IsChannelEnabled(channel))
            return ValueTask.CompletedTask;

        AdaptiveCards.AdaptiveCard card = _adaptiveCardUtil.Build(title, summary, facts, e, additionalBody);

        if (_useQueue)
            return PlaceOnQueue(card, channel, cancellationToken);

        return SendImmediately(card, channel, cancellationToken);
    }

    public ValueTask SendMessage(Exception e, string? title = null, string? channel = null, string? summary = null, Dictionary<string, string?>? facts = null,
        bool skipLocal = false, CancellationToken cancellationToken = default)
    {
        if (ShouldSkipBecauseLocal(skipLocal))
            return ValueTask.CompletedTask;

        if (channel == null)
            channel = "Errors";

        title ??= "Exception thrown";

        if (!IsChannelEnabled(channel))
            return ValueTask.CompletedTask;

        AdaptiveCards.AdaptiveCard card = _adaptiveCardUtil.Build(title, summary, facts, e);

        if (_useQueue)
            return PlaceOnQueue(card, channel, cancellationToken);

        return SendImmediately(card, channel, cancellationToken);
    }

    public ValueTask SendMessage<T>(string title, string? summary, List<T> items, string channel, bool skipLocal = false,
        CancellationToken cancellationToken = default)
    {
        if (ShouldSkipBecauseLocal(skipLocal))
            return ValueTask.CompletedTask;

        if (!IsChannelEnabled(channel))
            return ValueTask.CompletedTask;

        AdaptiveCards.AdaptiveCard card = _adaptiveCardUtil.BuildTable(title, items, summary);

        if (_useQueue)
            return PlaceOnQueue(card, channel, cancellationToken);

        return SendImmediately(card, channel, cancellationToken);
    }

    public ValueTask SendMessageCard(AdaptiveCards.AdaptiveCard card, string channel, bool skipLocal = false, CancellationToken cancellationToken = default)
    {
        if (ShouldSkipBecauseLocal(skipLocal))
            return ValueTask.CompletedTask;

        if (!IsChannelEnabled(channel))
            return ValueTask.CompletedTask;

        if (_useQueue)
            return PlaceOnQueue(card, channel, cancellationToken);

        return SendImmediately(card, channel, cancellationToken);
    }

    private async ValueTask SendImmediately(AdaptiveCards.AdaptiveCard card, string channel, CancellationToken cancellationToken = default)
    {
        var msTeamsCard = new MsTeamsCard
        {
            Type = "message",
            Attachments =
            [
                new AdaptiveCardAttachments
                {
                    Content = card
                }
            ]
        };

        await _msTeamsSender.SendCard(msTeamsCard, channel, cancellationToken).NoSync();
    }

    private ValueTask PlaceOnQueue(AdaptiveCards.AdaptiveCard card, string channel, CancellationToken cancellationToken = default)
    {
        var message = new MsTeamsMessage(card, channel);

        return _serviceBusTransmitter.SendMessage(message, cancellationToken: cancellationToken);
    }

    private bool IsChannelEnabled(string channel)
    {
        var enabled = _config.GetValueStrict<bool>($"MsTeams:{channel}:Enabled");

        if (!enabled)
            _logger.LogDebug("Skipping sending MSTeams notification due to channel config");

        return enabled;
    }

    private bool ShouldSkipBecauseLocal(bool skipLocal = false)
    {
        if (!skipLocal)
            return false;

        if (_config.GetValueStrict<string>("Environment") != DeployEnvironment.Local)
            return false;

        _logger.LogDebug("Skipping sending service bus message because we're in local environment and {variable} was true", nameof(skipLocal));
        return true;
    }
}