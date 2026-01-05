using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Soenneker.AdaptiveCard.Util.Abstract;
using Soenneker.Dtos.AdaptiveCard.Attachments;
using Soenneker.Dtos.MsTeams.Card;
using Soenneker.Enums.DeployEnvironment;
using Soenneker.Extensions.Configuration;
using Soenneker.Messages.MsTeams;
using Soenneker.MsTeams.Sender.Abstract;
using Soenneker.MsTeams.Util.Abstract;
using Soenneker.ServiceBus.Transmitter.Abstract;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
    private readonly bool _isLocal;

    private readonly ConcurrentDictionary<string, bool> _channelEnabledCache = new(StringComparer.OrdinalIgnoreCase);

    private IDisposable? _reloadRegistration;

    private const string _msTeamsPrefix = "MsTeams:";
    private const string _useQueueKey = "MsTeams:UseQueue";
    private const string _environmentKey = "Environment";
    private const string _defaultErrorChannel = "Errors";
    private const string _defaultExceptionTitle = "Exception thrown";

    public MsTeamsUtil(IConfiguration config, IServiceBusTransmitter servicesBusTransmitter, IAdaptiveCardUtil adaptiveCardUtil, ILogger<MsTeamsUtil> logger,
        IMsTeamsSender msTeamsSender)
    {
        _logger = logger;
        _msTeamsSender = msTeamsSender;
        _config = config;
        _serviceBusTransmitter = servicesBusTransmitter;
        _adaptiveCardUtil = adaptiveCardUtil;

        _useQueue = config.GetValue<bool>(_useQueueKey);

        // Cache local env once (avoid string compare + config lookup each send)
        _isLocal = string.Equals(config.GetValueStrict<string>(_environmentKey), DeployEnvironment.Local, StringComparison.OrdinalIgnoreCase);

        // If configuration supports reload, clear the per-channel cache on change
        // so runtime toggles still work without repeated config reads.
        IChangeToken token = config.GetReloadToken();
        _reloadRegistration = token.RegisterChangeCallback(static state =>
        {
            var self = (MsTeamsUtil)state!;
            self._channelEnabledCache.Clear();

            // Re-register (reload tokens are one-shot)
            self._reloadRegistration?.Dispose();
            self._reloadRegistration = self._config.GetReloadToken()
                                           .RegisterChangeCallback(static s => ((MsTeamsUtil)s!)._channelEnabledCache.Clear(), self);
        }, this);
    }

    public ValueTask SendMessage(string title, string channel, string? summary = null, Dictionary<string, string?>? facts = null, Exception? e = null,
        string? additionalBody = null, bool skipLocal = false, CancellationToken cancellationToken = default)
    {
        if (skipLocal && _isLocal)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                Logs.LogSkippingLocal(_logger, nameof(skipLocal), null);

            return ValueTask.CompletedTask;
        }

        if (!IsChannelEnabledCached(channel))
            return ValueTask.CompletedTask;

        AdaptiveCards.AdaptiveCard card = _adaptiveCardUtil.Build(title, summary, facts, e, additionalBody);

        return _useQueue ? PlaceOnQueue(card, channel, cancellationToken) : SendImmediately(card, channel, cancellationToken);
    }

    public ValueTask SendMessage(Exception e, string? title = null, string? channel = null, string? summary = null, Dictionary<string, string?>? facts = null,
        bool skipLocal = false, CancellationToken cancellationToken = default)
    {
        if (skipLocal && _isLocal)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                Logs.LogSkippingLocal(_logger, nameof(skipLocal), null);

            return ValueTask.CompletedTask;
        }

        channel ??= _defaultErrorChannel;
        title ??= _defaultExceptionTitle;

        if (!IsChannelEnabledCached(channel))
            return ValueTask.CompletedTask;

        AdaptiveCards.AdaptiveCard card = _adaptiveCardUtil.Build(title, summary, facts, e);

        return _useQueue ? PlaceOnQueue(card, channel, cancellationToken) : SendImmediately(card, channel, cancellationToken);
    }

    public ValueTask SendMessage<T>(string title, string? summary, List<T> items, string channel, bool skipLocal = false,
        CancellationToken cancellationToken = default)
    {
        if (skipLocal && _isLocal)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                Logs.LogSkippingLocal(_logger, nameof(skipLocal), null);

            return ValueTask.CompletedTask;
        }

        if (!IsChannelEnabledCached(channel))
            return ValueTask.CompletedTask;

        AdaptiveCards.AdaptiveCard card = _adaptiveCardUtil.BuildTable(title, items, summary);

        return _useQueue ? PlaceOnQueue(card, channel, cancellationToken) : SendImmediately(card, channel, cancellationToken);
    }

    public ValueTask SendMessageCard(AdaptiveCards.AdaptiveCard card, string channel, bool skipLocal = false, CancellationToken cancellationToken = default)
    {
        if (skipLocal && _isLocal)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                Logs.LogSkippingLocal(_logger, nameof(skipLocal), null);

            return ValueTask.CompletedTask;
        }

        if (!IsChannelEnabledCached(channel))
            return ValueTask.CompletedTask;

        return _useQueue ? PlaceOnQueue(card, channel, cancellationToken) : SendImmediately(card, channel, cancellationToken);
    }

    private ValueTask SendImmediately(AdaptiveCards.AdaptiveCard card, string channel, CancellationToken cancellationToken)
    {
        // You’ll still allocate these payload objects; that’s unavoidable unless you change DTO shapes.
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

        // Avoid async state machine; wrap the returned task.
        Task sendTask = _msTeamsSender.SendCard(msTeamsCard, channel, cancellationToken);
        return new ValueTask(sendTask);
    }

    private ValueTask PlaceOnQueue(AdaptiveCards.AdaptiveCard card, string channel, CancellationToken cancellationToken)
    {
        var message = new MsTeamsMessage(card, channel);
        return _serviceBusTransmitter.SendMessage(message, cancellationToken: cancellationToken);
    }

    private bool IsChannelEnabledCached(string channel)
    {
        return _channelEnabledCache.GetOrAdd(channel, static (ch, state) =>
        {
            MsTeamsUtil self = state!;
            bool enabled = self._config.GetValueStrict<bool>($"{_msTeamsPrefix}{ch}:Enabled");

            if (!enabled && self._logger.IsEnabled(LogLevel.Debug))
                Logs.LogSkippingChannel(self._logger, null);

            return enabled;
        }, this);
    }

    public void Dispose()
    {
        _reloadRegistration?.Dispose();
        _reloadRegistration = null;
    }
}