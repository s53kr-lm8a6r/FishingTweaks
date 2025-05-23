using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace FishingTweaks;

/// <summary>
///     Contains functionality for handling the BobberBar minigame.
///     This part of the mod automatically completes the fishing minigame when it appears,
///     making fishing more convenient by eliminating the need for manual minigame interaction.
/// </summary>
internal sealed partial class ModEntry
{
    /// <summary>
    ///     Handles the BobberBar menu appearance.
    ///     When the fishing minigame appears, it automatically sets the progress
    ///     to maximum and ensures any treasure is caught.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event data.</param>
    private void SkipMinigameOnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (e.NewMenu is not BobberBar bobberBar) return;
        if (!_autoFishing) return;
        if (!_config.EnableSkipMinigame) return;

        var msg = HUDMessage.ForItemGained(ItemRegistry.Create(bobberBar.whichFish), 1, "minigame");

        if (!_config.SatisfiedSkipMinigame(bobberBar.whichFish))
        {
            _config.FishCounter.CurrentCount(bobberBar.whichFish, out var catchCount, out var perfectCount);

            msg.message = Helper.Translation.Get("bobber-bar.needed",
                new
                {
                    fishName = ItemRegistry.Create(bobberBar.whichFish).DisplayName,
                    catchNeeded = Math.Max(_config.MinCatchCountForSkipFishing - catchCount, 0),
                    perfectNeeded = Math.Max(_config.MinPerfectCountForSkipFishing - perfectCount, 0)
                }
            );
            Game1.addHUDMessage(msg);
            return;
        }

        // Set the progress bar to maximum (2.0 is the value that triggers a catch(>=1.0f))
        bobberBar.distanceFromCatching = 2.0f;

        // Catch treasure
        bobberBar.treasureCaught = bobberBar.treasure && _config.SkipMinigameWithTreasure;

        if (_config.SkipMinigameWithPerfect)
            // Perfect on demand, since it is perfect with default so do not need to check like treasure
            bobberBar.perfect = true;
        else
        {
            int baseChance; // in percentage
            switch (bobberBar.motionType)
            {
                // Dart
                case 1:
                    baseChance = 5;
                    break;
                // Smooth
                case 2:
                    baseChance = 90;
                    break;
                // Floater & Sinker
                case 3:
                case 4:
                    baseChance = 22;
                    break;
                // Mixed
                default:
                    baseChance = 54;
                    break;
            }
            baseChance = bobberBar.bossFish ? (int)Math.Ceiling(baseChance / 5f) : baseChance;
            double difficultyMultiplier = (-3.72f + (123f / (1 + Math.Pow(bobberBar.difficulty / 44.29f, 2.11f)))) / 100;

            bobberBar.perfect = new Random().Next(0, 100) <= (baseChance * difficultyMultiplier);
        }

        msg.message = Helper.Translation.Get("bobber-bar.familiar");
        Game1.addHUDMessage(msg);
    }


    private void RecordFishingOnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (!_autoFishing) return;
        if (e.OldMenu is not BobberBar bobberBar) return;
        if (!bobberBar.handledFishResult) return;
        if (bobberBar.distanceFromCatching < 0.5f) return; // missed

        IncrFishCounter(bobberBar.whichFish, bobberBar.perfect);
    }
}
