using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;

namespace MultiplayerChests
{
    internal sealed class ModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
            helper.Events.Input.ButtonPressed += OnButtonPressed;

            var harmony = new Harmony(ModManifest.UniqueID);
            harmony.Patch(
                original: AccessTools.Method(typeof(Chest), nameof(Chest.fixLidFrame)),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Prefix_FixLidFrame))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Chest), nameof(Chest.updateWhenCurrentLocation)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Postfix_UpdateWhenCurrentLocation))
            );
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!MetPrerequisites())
                return;

            if (!e.Button.IsActionButton())
                return;

            var tilePos = e.Cursor.GrabTile;
            var objectAtTile = Game1.currentLocation.getObjectAtTile((int)tilePos.X, (int)tilePos.Y);

            if (objectAtTile is Chest chest && IsPlayerChest(chest))
            {
                Helper.Input.Suppress(e.Button);

                chest.Location.localSound(chest.fridge.Value ? "doorCreak" : "openChest");
                chest.performOpenChest();
            }
        }

        internal static bool Prefix_FixLidFrame(Chest __instance, ref int ___currentLidFrame)
        {
            if (IsPlayerChest(__instance))
            {
                if (___currentLidFrame < __instance.startingLidFrame.Value || ___currentLidFrame > __instance.getLastLidFrame())
                {
                    ___currentLidFrame = __instance.startingLidFrame.Value;
                }
                // Prevent `fixLidFrame` from being invoked
                return false;
            }
            return true;
        }

        internal static void Postfix_UpdateWhenCurrentLocation(Chest __instance, ref int ___currentLidFrame)
        {
            if (!MetPrerequisites()) 
                return;

            if (!IsPlayerChest(__instance)) 
                return;

            if (__instance.frameCounter.Value >= 0 && ___currentLidFrame <= __instance.getLastLidFrame())
            {
                if (___currentLidFrame == __instance.getLastLidFrame() && Game1.activeClickableMenu is null)
                {
                    __instance.ShowMenu();
                    var chestMenu = Game1.activeClickableMenu;
                    if (chestMenu is not null)
                    {
                        chestMenu.exitFunction += () =>
                        {
                            __instance.Location.localSound("doorCreakReverse");
                        };
                    }

                }
                ___currentLidFrame += 1;
            }
            else if (___currentLidFrame > __instance.startingLidFrame.Value && Game1.activeClickableMenu is null)
            {
                ___currentLidFrame -= 1;
            }
            else
            {
                __instance.frameCounter.Value = -1;
            }
        }

        private static bool IsPlayerChest(Chest chest)
        {
            return chest.playerChest.Value && new[] { Chest.SpecialChestTypes.BigChest, Chest.SpecialChestTypes.None }.Contains(chest.SpecialChestType);
        }

        private static bool MetPrerequisites()
        {
            return Context.IsWorldReady && Game1.activeClickableMenu is null;
        }
    }
}