// <copyright file="QuestPanelService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.QuestPanel;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Quests;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.Views.Character;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.GameLogic.Views.Quest;

/// <summary>
/// Sequential main quests from the Quest Master NPC.
/// Finish quest N to unlock quest N+1.
/// </summary>
public static class QuestPanelService
{
    /// <summary>
    /// Custom Quest Master NPC number (Lorencia 130,134).
    /// </summary>
    public const short QuestNpcNumber = 691;

    /// <summary>
    /// Box of Kundun group.
    /// </summary>
    public const byte KundunGroup = 14;

    /// <summary>
    /// Box of Kundun number.
    /// </summary>
    public const short KundunNumber = 11;

    /// <summary>
    /// Talisman of Chaos Assembly group.
    /// </summary>
    public const byte TcaGroup = 14;

    /// <summary>
    /// Talisman of Chaos Assembly number.
    /// </summary>
    public const short TcaNumber = 96;

    /// <summary>
    /// Builds the status snapshot for the current stage.
    /// </summary>
    public static QuestPanelStatus BuildStatus(Player player)
    {
        PreparePlayer(player);

        var stage = GetStage(player);
        var total = QuestPanelCatalog.Count;
        var def = QuestPanelCatalog.Get(stage);

        if (def is null)
        {
            return new QuestPanelStatus
            {
                Name = "Main Quest Complete",
                TargetLabel = string.Empty,
                Kills = 0,
                Required = 0,
                Claimed = true,
                CanClaim = false,
                Accepted = false,
                State = QuestPanelState.Claimed,
                Stage = total,
                Total = total,
            };
        }

        var kills = (int)GetAttributeValue(player, Stats.QuestPanelSpiderKills);
        var accepted = GetAttributeValue(player, Stats.QuestPanelAccepted) >= 1f;
        var canClaim = accepted && kills >= def.RequiredKills;

        QuestPanelState state;
        if (canClaim)
        {
            state = QuestPanelState.Complete;
        }
        else if (accepted)
        {
            state = QuestPanelState.InProgress;
        }
        else
        {
            state = QuestPanelState.Available;
        }

        return new QuestPanelStatus
        {
            Name = def.Title,
            TargetLabel = def.TargetLabel,
            Kills = Math.Clamp(kills, 0, def.RequiredKills),
            Required = def.RequiredKills,
            Claimed = false,
            CanClaim = canClaim,
            Accepted = accepted,
            State = state,
            Stage = stage,
            Total = total,
            RequiredLevel = QuestPanelCatalog.GetRequiredLevel(stage),
        };
    }

    /// <summary>
    /// Opens the NPC quest list and pushes current status.
    /// </summary>
    public static async ValueTask ShowNpcDialogAsync(Player player)
    {
        PreparePlayer(player);

        // Talking to the Quest Master accepts the current quest when level requirement is met.
        var stage = GetStage(player);
        if (QuestPanelCatalog.Get(stage) is not null
            && MeetsLevelRequirement(player, stage)
            && GetAttributeValue(player, Stats.QuestPanelAccepted) < 1f)
        {
            GetOrCreateStat(player, Stats.QuestPanelAccepted).Value = 1;
        }

        var status = BuildStatus(player);
        var list = BuildNpcQuestList(player);
        await player.InvokeViewPlugInAsync<IShowQuestPanelPlugIn>(p => p.ShowQuestNpcListAsync(status.Stage, status.Total, list)).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IShowQuestPanelPlugIn>(p => p.ShowQuestNpcDialogAsync(status)).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds every main quest row for the Quest Master dialog.
    /// </summary>
    public static IReadOnlyList<QuestPanelNpcListEntry> BuildNpcQuestList(Player player)
    {
        PreparePlayer(player);
        var stage = GetStage(player);
        var total = QuestPanelCatalog.Count;
        var current = QuestPanelCatalog.Get(stage);
        var kills = (int)GetAttributeValue(player, Stats.QuestPanelSpiderKills);
        var accepted = GetAttributeValue(player, Stats.QuestPanelAccepted) >= 1f;
        var canClaim = current is not null && accepted && kills >= current.RequiredKills;
        var chainDone = current is null;

        var list = new List<QuestPanelNpcListEntry>(total);
        for (var i = 0; i < total; i++)
        {
            var def = QuestPanelCatalog.Quests[i];
            byte listState;
            if (chainDone || i < stage)
            {
                listState = 4;
            }
            else if (i > stage)
            {
                listState = 0;
            }
            else if (canClaim)
            {
                listState = 3;
            }
            else if (accepted)
            {
                listState = 2;
            }
            else
            {
                listState = 1;
            }

            list.Add(new QuestPanelNpcListEntry(i, listState, def.Title));
        }

        return list;
    }

    /// <summary>
    /// Accepts the current-stage quest.
    /// </summary>
    public static async ValueTask TryAcceptAsync(Player player)
    {
        EnsureAttributeDefinitions(player);
        MigrateLegacyProgress(player);

        if (QuestPanelCatalog.Get(GetStage(player)) is null)
        {
            return;
        }

        if (!MeetsLevelRequirement(player, GetStage(player)))
        {
            var blocked = BuildStatus(player);
            await player.InvokeViewPlugInAsync<IShowQuestPanelPlugIn>(p => p.ShowQuestPanelStatusAsync(blocked)).ConfigureAwait(false);
            return;
        }

        if (GetAttributeValue(player, Stats.QuestPanelAccepted) >= 1f)
        {
            var existing = BuildStatus(player);
            await player.InvokeViewPlugInAsync<IShowQuestPanelPlugIn>(p => p.ShowQuestPanelStatusAsync(existing)).ConfigureAwait(false);
            return;
        }

        GetOrCreateStat(player, Stats.QuestPanelAccepted).Value = 1;
        var status = BuildStatus(player);
        await player.InvokeViewPlugInAsync<IShowQuestPanelPlugIn>(p => p.ShowQuestPanelStatusAsync(status)).ConfigureAwait(false);
    }

    /// <summary>
    /// Increments kill progress when the killed monster matches the current quest.
    /// </summary>
    public static async ValueTask TryRegisterKillAsync(Player player, Monster monster)
    {
        if (monster.SummonedBy is not null || monster.Definition is null)
        {
            return;
        }

        PreparePlayer(player);

        var def = QuestPanelCatalog.Get(GetStage(player));
        if (def is null)
        {
            return;
        }

        if (monster.Definition.Number != def.MonsterNumber)
        {
            return;
        }

        if (!MeetsLevelRequirement(player, GetStage(player)))
        {
            return;
        }

        if (GetAttributeValue(player, Stats.QuestPanelAccepted) < 1f)
        {
            GetOrCreateStat(player, Stats.QuestPanelAccepted).Value = 1;
        }

        var attr = GetOrCreateStat(player, Stats.QuestPanelSpiderKills);
        if (attr.Value >= def.RequiredKills)
        {
            return;
        }

        attr.Value += 1;
        var status = BuildStatus(player);
        await player.InvokeViewPlugInAsync<IShowQuestPanelPlugIn>(p => p.ShowQuestPanelStatusAsync(status)).ConfigureAwait(false);
    }

    /// <summary>
    /// Claims the current quest reward and unlocks the next stage.
    /// </summary>
    public static async ValueTask<QuestPanelClaimResult> TryClaimAsync(Player player)
    {
        if (player.SelectedCharacter is null || player.Inventory is null)
        {
            return QuestPanelClaimResult.Failed;
        }

        EnsureAttributeDefinitions(player);
        MigrateLegacyProgress(player);

        var stage = GetStage(player);
        var def = QuestPanelCatalog.Get(stage);
        if (def is null)
        {
            return QuestPanelClaimResult.AlreadyClaimed;
        }

        var status = BuildStatus(player);
        if (!status.CanClaim)
        {
            return QuestPanelClaimResult.RequirementsNotMet;
        }

        if (def.Experience > 0)
        {
            var exp = def.Experience > int.MaxValue ? int.MaxValue : (int)def.Experience;
            await player.AddExperienceAsync(exp, null).ConfigureAwait(false);
        }

        if (def.Money > 0 && !player.TryAddMoney(def.Money))
        {
            return QuestPanelClaimResult.InventoryFull;
        }

        if (def.KundunCount > 0)
        {
            var result = await TryGrantItemAsync(player, KundunGroup, KundunNumber, def.KundunLevel, def.KundunCount).ConfigureAwait(false);
            if (result != QuestPanelClaimResult.Success)
            {
                return result;
            }
        }

        if (def.TcaCount > 0)
        {
            var result = await TryGrantItemAsync(player, TcaGroup, TcaNumber, 0, def.TcaCount).ConfigureAwait(false);
            if (result != QuestPanelClaimResult.Success)
            {
                return result;
            }
        }

        if (def.LevelUpPoints > 0)
        {
            GetOrCreateStat(player, Stats.QuestPanelPermanentPoints).Value += def.LevelUpPoints;
            player.SelectedCharacter.LevelUpPoints += def.LevelUpPoints;
            await player.InvokeViewPlugInAsync<ILegacyQuestRewardPlugIn>(
                p => p.ShowAsync(player, QuestRewardType.LevelUpPoints, def.LevelUpPoints, null)).ConfigureAwait(false);
            await player.InvokeViewPlugInAsync<IUpdateLevelPlugIn>(p => p.UpdateLevelAsync()).ConfigureAwait(false);
        }

        // Advance to next quest in the chain.
        GetOrCreateStat(player, Stats.QuestPanelStage).Value = stage + 1;
        GetOrCreateStat(player, Stats.QuestPanelSpiderKills).Value = 0;
        GetOrCreateStat(player, Stats.QuestPanelAccepted).Value = 0;
        if (QuestPanelCatalog.Get(stage + 1) is null)
        {
            GetOrCreateStat(player, Stats.QuestPanelClaimed).Value = 1;
        }

        return QuestPanelClaimResult.Success;
    }

    /// <summary>
    /// Abandons the current quest (keeps stage, clears accept/kills).
    /// </summary>
    public static async ValueTask TryAbandonAsync(Player player)
    {
        EnsureAttributeDefinitions(player);
        if (QuestPanelCatalog.Get(GetStage(player)) is null)
        {
            return;
        }

        GetOrCreateStat(player, Stats.QuestPanelSpiderKills).Value = 0;
        GetOrCreateStat(player, Stats.QuestPanelAccepted).Value = 0;
        var status = BuildStatus(player);
        await player.InvokeViewPlugInAsync<IShowQuestPanelPlugIn>(p => p.ShowQuestPanelStatusAsync(status)).ConfigureAwait(false);
    }

    /// <summary>
    /// Permanent level-up point bonus from all completed main quests (added on every reset).
    /// </summary>
    public static int GetPermanentPointBonus(Player player)
    {
        EnsureAttributeDefinitions(player);
        MigratePermanentQuestPoints(player);
        return (int)Math.Clamp(GetAttributeValue(player, Stats.QuestPanelPermanentPoints), 0, int.MaxValue);
    }

    private static int GetStage(Player player)
        => Math.Clamp((int)GetAttributeValue(player, Stats.QuestPanelStage), 0, QuestPanelCatalog.Count);

    private static async ValueTask<QuestPanelClaimResult> TryGrantItemAsync(
        Player player,
        byte group,
        short number,
        byte level,
        int count)
    {
        if (player.Inventory is null)
        {
            return QuestPanelClaimResult.Failed;
        }

        var itemDefinition = player.GameContext.Configuration.Items
            .FirstOrDefault(i => i.Group == group && i.Number == number);
        if (itemDefinition is null)
        {
            return QuestPanelClaimResult.Failed;
        }

        for (var i = 0; i < count; i++)
        {
            var item = player.PersistenceContext.CreateNew<Item>();
            item.Definition = itemDefinition;
            item.Level = level;
            item.Durability = 1;

            if (await player.Inventory.AddItemAsync(item).ConfigureAwait(false))
            {
                await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(item)).ConfigureAwait(false);
            }
            else if (player.CurrentMap is { } map)
            {
                await map.AddAsync(new DroppedItem(item, player.Position, map, player, player.GetAsEnumerable())).ConfigureAwait(false);
            }
            else
            {
                return QuestPanelClaimResult.InventoryFull;
            }
        }

        return QuestPanelClaimResult.Success;
    }

    private static bool MeetsLevelRequirement(Player player, int stage)
    {
        var required = QuestPanelCatalog.GetRequiredLevel(stage);
        var level = player.Level;
        return level >= required;
    }

    private static void PreparePlayer(Player player)
    {
        EnsureAttributeDefinitions(player);
        MigrateLegacyProgress(player);
        MigratePermanentQuestPoints(player);
        FixSpiderStageMigration(player);
    }

    /// <summary>
    /// Back-fills permanent quest points for characters who completed quests before this feature.
    /// </summary>
    private static void MigratePermanentQuestPoints(Player player)
    {
        if (GetAttributeValue(player, Stats.QuestPanelPermanentPoints) > 0f)
        {
            return;
        }

        var stage = GetStage(player);
        var chainDone = GetAttributeValue(player, Stats.QuestPanelClaimed) >= 1f;
        var completedCount = chainDone ? QuestPanelCatalog.Count : stage;
        if (completedCount <= 0)
        {
            return;
        }

        long sum = 0;
        for (var i = 0; i < completedCount; i++)
        {
            var quest = QuestPanelCatalog.Get(i);
            if (quest is not null)
            {
                sum += quest.LevelUpPoints;
            }
        }

        if (sum > 0)
        {
            GetOrCreateStat(player, Stats.QuestPanelPermanentPoints).Value = sum;
        }
    }

    /// <summary>
    /// Legacy single Spider Hunt → sequential chain.
    /// </summary>
    private static void MigrateLegacyProgress(Player player)
    {
        if (player.SelectedCharacter?.Attributes.Any(a => a.Definition?.Id == Stats.QuestPanelStage.Id) == true)
        {
            return;
        }

        if (GetAttributeValue(player, Stats.QuestPanelClaimed) >= 1f)
        {
            // Finished old single Spider Hunt → unlock quest 2 (Bull Fighter, index 1).
            GetOrCreateStat(player, Stats.QuestPanelStage).Value = 1;
            GetOrCreateStat(player, Stats.QuestPanelClaimed).Value = 0;
            GetOrCreateStat(player, Stats.QuestPanelSpiderKills).Value = 0;
            GetOrCreateStat(player, Stats.QuestPanelAccepted).Value = 0;
            return;
        }

        var kills = GetAttributeValue(player, Stats.QuestPanelSpiderKills);
        var accepted = GetAttributeValue(player, Stats.QuestPanelAccepted) >= 1f;
        if (kills > 0f || accepted)
        {
            // Legacy Spider Hunt progress → quest 1 (index 0) is Spider Hunt.
            const int spiderStage = 0;
            GetOrCreateStat(player, Stats.QuestPanelStage).Value = spiderStage;
            if (accepted)
            {
                GetOrCreateStat(player, Stats.QuestPanelAccepted).Value = 1;
            }

            var spiderReq = QuestPanelCatalog.Get(spiderStage)?.RequiredKills ?? 15;
            GetOrCreateStat(player, Stats.QuestPanelSpiderKills).Value = Math.Min(kills, spiderReq);
            return;
        }

        GetOrCreateStat(player, Stats.QuestPanelStage).Value = 0;
    }

    /// <summary>
    /// Characters migrated to stage 3 for Spider Hunt (old catalog) are moved to stage 0.
    /// </summary>
    private static void FixSpiderStageMigration(Player player)
    {
        var stage = (int)GetAttributeValue(player, Stats.QuestPanelStage);
        if (stage != 3)
        {
            return;
        }

        var kills = GetAttributeValue(player, Stats.QuestPanelSpiderKills);
        var accepted = GetAttributeValue(player, Stats.QuestPanelAccepted) >= 1f;
        if (kills <= 0f && !accepted)
        {
            return;
        }

        const int spiderStage = 0;
        GetOrCreateStat(player, Stats.QuestPanelStage).Value = spiderStage;
        var spiderReq = QuestPanelCatalog.Get(spiderStage)?.RequiredKills ?? 15;
        GetOrCreateStat(player, Stats.QuestPanelSpiderKills).Value = Math.Min(kills, spiderReq);
        if (accepted)
        {
            GetOrCreateStat(player, Stats.QuestPanelAccepted).Value = 1;
        }
    }

    private static void EnsureAttributeDefinitions(Player player)
    {
        EnsureDefinition(player, Stats.QuestPanelSpiderKills);
        EnsureDefinition(player, Stats.QuestPanelClaimed);
        EnsureDefinition(player, Stats.QuestPanelAccepted);
        EnsureDefinition(player, Stats.QuestPanelStage);
        EnsureDefinition(player, Stats.QuestPanelPermanentPoints);
    }

    private static void EnsureDefinition(Player player, AttributeDefinition template)
    {
        var config = player.GameContext.Configuration;
        if (config.Attributes.Any(a => a.Id == template.Id))
        {
            return;
        }

        try
        {
            var persistent = player.PersistenceContext.CreateNew<AttributeDefinition>(
                template.Id,
                template.Designation,
                template.Description);
            config.Attributes.Add(persistent);
        }
        catch (Exception)
        {
            // Update plugin should add persistent attributes.
        }
    }

    private static float GetAttributeValue(Player player, AttributeDefinition template)
    {
        var attr = player.SelectedCharacter?.Attributes
            .FirstOrDefault(a => a.Definition is not null && a.Definition.Id == template.Id);
        return attr?.Value ?? 0f;
    }

    private static StatAttribute GetOrCreateStat(Player player, AttributeDefinition template)
    {
        var character = player.SelectedCharacter
            ?? throw new InvalidOperationException("No character selected.");
        var existing = character.Attributes
            .FirstOrDefault(a => a.Definition is not null && a.Definition.Id == template.Id);
        if (existing is not null)
        {
            return existing;
        }

        var definition = player.GameContext.Configuration.Attributes.First(a => a.Id == template.Id);
        var created = player.PersistenceContext.CreateNew<StatAttribute>(definition, 0);
        character.Attributes.Add(created);
        return created;
    }
}
