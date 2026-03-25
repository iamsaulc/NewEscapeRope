using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;

namespace NewEscapeRope
{
    public class ModEntry : Mod
    {
        // Qualified item IDs must match the entries in the Content Patcher pack.
        private const string EscapeRopeId = "(O)NewEscapeRope.EscapeRope";
        private const string FloorRopeId  = "(O)NewEscapeRope.FloorRope";

        public override void Entry(IModHelper helper)
        {
            helper.Events.Input.ButtonPressed += OnButtonPressed;
        }

        // ── Input ────────────────────────────────────────────────────────────────

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsPlayerFree) return;
            if (!e.Button.IsActionButton()) return;
            if (Game1.activeClickableMenu != null || Game1.player.hasMenuOpen.Value || Game1.eventUp) return;

            Item? held = Game1.player.CurrentItem;
            if (held is null) return;

            bool isEscape = held.QualifiedItemId == EscapeRopeId;
            bool isFloor  = held.QualifiedItemId == FloorRopeId;
            if (!isEscape && !isFloor) return;

            // Wait one tick before acting to rule out clicks that open a menu.
            if (isEscape)
                Helper.Events.GameLoop.UpdateTicked += OnTickEscape;
            else
                Helper.Events.GameLoop.UpdateTicked += OnTickFloor;
        }

        // ── Deferred handlers ────────────────────────────────────────────────────

        private void OnTickEscape(object? sender, UpdateTickedEventArgs e)
        {
            Helper.Events.GameLoop.UpdateTicked -= OnTickEscape;
            if (!PlayerIsFree()) return;
            UseEscapeRope();
        }

        private void OnTickFloor(object? sender, UpdateTickedEventArgs e)
        {
            Helper.Events.GameLoop.UpdateTicked -= OnTickFloor;
            if (!PlayerIsFree()) return;
            UseFloorRope();
        }

        private static bool PlayerIsFree() =>
            Context.IsPlayerFree
            && Game1.activeClickableMenu == null
            && !Game1.player.hasMenuOpen.Value
            && !Game1.eventUp;

        // ── Escape Rope ──────────────────────────────────────────────────────────

        private void UseEscapeRope()
        {
            if (Game1.currentLocation is not MineShaft and not VolcanoDungeon)
            {
                Monitor.Log("Escape Rope used outside a dungeon, nothing happened.", LogLevel.Trace);
                return;
            }

            Consume();
            PlayEffect();
            Game1.player.FarmerSprite.animateOnce(BuildAnimation(WarpOut));
        }

        private static void WarpOut(Farmer who)
        {
            if (who.currentLocation is MineShaft)
            {
                if (Game1.CurrentMineLevel == 77377)        // Quarry mine entrance
                    Game1.warpFarmer("Mine", 67, 10, false);
                else if (Game1.CurrentMineLevel > 120)      // Skull Cavern entrance
                    Game1.warpFarmer("SkullCave", 3, 4, false);
                else                                        // Regular mine entrance
                    Game1.warpFarmer("Mine", 17, 4, false);
                return;
            }

            if (who.currentLocation is VolcanoDungeon)
                Game1.warpFarmer("IslandNorth", 40, 25, false);
        }

        // ── Floor Rope ───────────────────────────────────────────────────────────

        private void UseFloorRope()
        {
            if (Game1.currentLocation is not MineShaft)
            {
                Monitor.Log("Floor Rope used outside a mine shaft, nothing happened.", LogLevel.Trace);
                return;
            }

            int level = Game1.CurrentMineLevel;

            // No sensible target floor: stay put and keep the item.
            if (level <= 1 || level == 77377 || level == 121)
            {
                Monitor.Log("Floor Rope: already at the top floor, item not consumed.", LogLevel.Trace);
                Game1.addHUDMessage(new HUDMessage(
                    Helper.Translation.Get("floor-rope.no-effect"),
                    HUDMessage.error_type
                ));
                return;
            }

            Consume();
            PlayEffect();
            Game1.player.FarmerSprite.animateOnce(BuildAnimation(WarpUp));
        }

        private static void WarpUp(Farmer who)
        {
            Game1.enterMine(Game1.CurrentMineLevel - 1);
        }

        // ── Shared helpers ───────────────────────────────────────────────────────

        private static void Consume()
        {
            Game1.player.reduceActiveItemByOne();
        }

        private static void PlayEffect()
        {
            Game1.player.jitterStrength = 1f;
            Game1.currentLocation.playSound("warrior");
            Game1.player.faceDirection(2);
            Game1.player.CanMove = false;
            Game1.player.temporarilyInvincible = true;
            Game1.player.temporaryInvincibilityTimer = -2000;
            Game1.changeMusicTrack("none");
        }

        // Raise-arm animation followed by a callback at the last frame.
        private static FarmerSprite.AnimationFrame[] BuildAnimation(
            AnimatedSprite.endOfAnimationBehavior callback)
        {
            return new[]
            {
                new FarmerSprite.AnimationFrame(57, 1000, secondaryArm: false, flip: false),
                new FarmerSprite.AnimationFrame(
                    (short)Game1.player.FarmerSprite.CurrentFrame,
                    0,
                    secondaryArm: false,
                    flip: false,
                    callback,
                    behaviorAtEndOfFrame: true)
            };
        }
    }
}
