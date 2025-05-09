﻿using System.Linq;
using System.Numerics;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Speech;
using Content.Shared.Whitelist;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Sunrise.UserActions.Tabs;

[GenerateTypedNameReferences]
public sealed partial class EmotesTabControl : BaseTabControl
{
    [Dependency] private readonly EntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private TimeSpan _lastEmoteTime;
    private static readonly TimeSpan EmoteCooldown = TimeSpan.FromSeconds(2f);

    public EmotesTabControl()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
    }

    public override bool UpdateState()
    {
        EmotesList.RemoveAllChildren();

        var player = _playerManager.LocalEntity;
        if (player is not { Valid: true })
            return false;

        var emotes = _prototypeManager.EnumeratePrototypes<EmotePrototype>()
            .Where(emote => IsEmoteAvailable(emote, player.Value))
            .OrderBy(x => x.Category)
            .ThenBy(x => x.ID)
            .ToList();

        if (emotes.Count == 0)
            return false;

        var currentRow = CreateNewRow();
        EmotesList.AddChild(currentRow);

        foreach (var emote in emotes)
        {
            var button = CreateEmoteButton(emote);
            currentRow.AddChild(button);
        }

        UpdateButtonsLayout();
        return true;
    }

    private void UpdateButtonsLayout()
    {
        if (!EmotesList.Children.Any())
            return;

        var maxWidth = Math.Min(300, Width);
        var firstRow = EmotesList.Children.First() as BoxContainer;
        if (firstRow == null)
            return;

        var currentRow = firstRow;
        var rowWidth = 0f;

        var allButtons = EmotesList.Children
            .OfType<BoxContainer>()
            .SelectMany(row => row.Children.ToList())
            .ToList();

        foreach (var button in allButtons)
        {
            button.Parent?.RemoveChild(button);
        }

        foreach (var child in EmotesList.Children.Skip(1).ToList())
        {
            EmotesList.RemoveChild(child);
        }
        firstRow.RemoveAllChildren();

        foreach (var button in allButtons)
        {
            if (rowWidth + 100 > maxWidth)
            {
                currentRow = CreateNewRow();
                EmotesList.AddChild(currentRow);
                rowWidth = 0;
            }

            currentRow.AddChild(button);
            rowWidth += 100;
        }
    }

    private BoxContainer CreateNewRow()
    {
        return new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
        };
    }

    private Button CreateEmoteButton(EmotePrototype emote)
    {
        var container = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            MinSize = new Vector2(0, 24),
            Margin = new Thickness(1)
        };

        var label = new RichTextLabel
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            Text = Loc.GetString(emote.Name)
        };

        var button = new Button
        {
            StyleClasses = { "EmoteButton" },
            HorizontalExpand = true,
            MinSize = new Vector2(0, 24),
            Margin = new Thickness(1)
        };

        container.AddChild(label);
        button.AddChild(container);

        button.OnPressed += _ => OnPlayEmote(new ProtoId<EmotePrototype>(emote.ID));

        return button;
    }

    private bool IsEmoteAvailable(EmotePrototype emote, EntityUid player)
    {
        var whitelistSystem = _entManager.System<EntityWhitelistSystem>();

        if (emote.Category == EmoteCategory.Invalid || emote.Category == EmoteCategory.Verb || emote.ChatTriggers.Count == 0)
            return false;

        if (!whitelistSystem.IsWhitelistPassOrNull(emote.Whitelist, player) ||
            whitelistSystem.IsBlacklistPass(emote.Blacklist, player))
            return false;

        if (!emote.Available &&
            _entManager.TryGetComponent<SpeechComponent>(player, out var speech) &&
            !speech.AllowedEmotes.Contains(emote.ID))
            return false;

        return true;
    }

    private void OnPlayEmote(ProtoId<EmotePrototype> protoId)
    {
        var currentTime = _gameTiming.CurTime;
        if (currentTime - _lastEmoteTime < EmoteCooldown)
            return;

        _lastEmoteTime = currentTime;
        _entManager.RaisePredictiveEvent(new PlayEmoteMessage(protoId));
    }

    protected override void Resized()
    {
        UpdateButtonsLayout();
    }
}
