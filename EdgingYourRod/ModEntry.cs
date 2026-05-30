using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;

namespace EdgingYourRod
{
    public class ModEntry : Mod
    {
        private ModConfig Config;
        private DateTime? _maxPowerReachedTime = null;
        private bool _wasMaxPower = false;
        private int _staminaDrainedSeconds = 0;

        public override void Entry(IModHelper helper)
        {
            this.Config = helper.ReadConfig<ModConfig>();
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu == null) return;

            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.Config)
            );

            configMenu.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => this.Config.DelaySeconds,
                setValue: value => this.Config.DelaySeconds = value,
                name: () => this.Helper.Translation.Get("config.delay.name"),
                tooltip: () => this.Helper.Translation.Get("config.delay.desc"),
                min: 1
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                getValue: () => this.Config.InfiniteHoldWithStamina,
                setValue: value => this.Config.InfiniteHoldWithStamina = value,
                name: () => this.Helper.Translation.Get("config.infinite.name"),
                tooltip: () => this.Helper.Translation.Get("config.infinite.desc")
            );
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            // 【这里修正了！】isTimingCast 代表玩家此时正按住按键、蓄力条正在波动的状态
            if (Game1.player?.CurrentTool is FishingRod rod && rod.isTimingCast)
            {
                // 当蓄力值接近或达到最大值 1.0 时
                if (rod.castingPower >= 0.98f)
                {
                    if (!_wasMaxPower)
                    {
                        _wasMaxPower = true;
                        _maxPowerReachedTime = DateTime.Now;
                        _staminaDrainedSeconds = 0;
                    }

                    if (this.Config.InfiniteHoldWithStamina)
                    {
                        TimeSpan elapsed = DateTime.Now - _maxPowerReachedTime.Value;
                        int currentElapsedSeconds = (int)elapsed.TotalSeconds;

                        if (currentElapsedSeconds > _staminaDrainedSeconds)
                        {
                            int secondsToDrain = currentElapsedSeconds - _staminaDrainedSeconds;
                            float drainAmount = (Game1.player.MaxStamina * 0.10f) * secondsToDrain;

                            Game1.player.Stamina = Math.Max(0, Game1.player.Stamina - drainAmount);
                            _staminaDrainedSeconds = currentElapsedSeconds;
                        }

                        if (Game1.player.Stamina > 0)
                        {
                            rod.castingPower = 1.0f;
                        }
                    }
                    else
                    {
                        TimeSpan elapsed = DateTime.Now - _maxPowerReachedTime.Value;
                        if (elapsed.TotalSeconds < this.Config.DelaySeconds)
                        {
                            rod.castingPower = 1.0f;
                        }
                    }
                }
                else
                {
                    if (rod.castingPower < 0.95f)
                    {
                        _wasMaxPower = false;
                    }
                }
            }
            else
            {
                _wasMaxPower = false;
                _staminaDrainedSeconds = 0;
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