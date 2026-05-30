using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;

namespace EdgingYourRod
{
    public class ModEntry : Mod
    {
        private static ModConfig Config;
        private static DateTime? _maxPowerReachedTime = null;
        private static bool _wasMaxPower = false;

        private static float _originalTimerSpeed = -0.001f;

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

            var harmony = new Harmony(this.ModManifest.UniqueID);

            harmony.Patch(
                original: AccessTools.Method(typeof(FishingRod), nameof(FishingRod.tickUpdate)),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(Prefix_FishingRodTickUpdate))
            );
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu == null) return;

            configMenu.Register(
                mod: this.ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(Config)
            );

            configMenu.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => Config.DelaySeconds,
                setValue: value => Config.DelaySeconds = value,
                name: () => this.Helper.Translation.Get("config.delay.name"),
                tooltip: () => this.Helper.Translation.Get("config.delay.desc"),
                min: 1
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                getValue: () => Config.InfiniteHoldWithStamina,
                setValue: value => Config.InfiniteHoldWithStamina = value,
                name: () => this.Helper.Translation.Get("config.infinite.name"),
                tooltip: () => this.Helper.Translation.Get("config.infinite.desc")
            );
        }

        private static void Prefix_FishingRodTickUpdate(FishingRod __instance, Farmer who)
        {
            if (__instance.isTimingCast)
            {
                if (__instance.castingPower >= 0.99f && !_wasMaxPower)
                {
                    _wasMaxPower = true;
                    _maxPowerReachedTime = DateTime.Now;

                    float currentSpeed = __instance.castingTimerSpeed;
                    _originalTimerSpeed = currentSpeed != 0f ? -Math.Abs(currentSpeed) : -0.001f;
                }

                if (_wasMaxPower)
                {
                    bool shouldLock = true;

                    if (Config.InfiniteHoldWithStamina)
                    {
                        float drainPerTick = (who.MaxStamina * 0.10f) / 60f;

                        who.Stamina -= drainPerTick;

                        if (who.Stamina <= 0)
                        {
                            who.Stamina = 0; 
                            shouldLock = false;
                        }
                    }
                    else
                    {
                        TimeSpan elapsed = DateTime.Now - _maxPowerReachedTime.Value;
                        if (elapsed.TotalSeconds >= Config.DelaySeconds)
                        {
                            shouldLock = false;
                        }
                    }

                    if (shouldLock)
                    {
                        __instance.castingPower = 1.0f;
                        __instance.castingTimerSpeed = 0f;
                    }
                    else
                    {
                        _wasMaxPower = false;
                        __instance.castingTimerSpeed = _originalTimerSpeed;
                    }
                }
            }
            else
            {
                _wasMaxPower = false;

                if (__instance.castingTimerSpeed == 0f)
                {
                    __instance.castingTimerSpeed = Math.Abs(_originalTimerSpeed);
                }
            }
        }
    }

    public interface IGenericModConfigMenuApi
    {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string> tooltip = null, string fieldId = null);
        void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string> tooltip = null, int? min = null, int? max = null, int? interval = null, Func<int, string> formatVal = null, string fieldId = null);
    }
}