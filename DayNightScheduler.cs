using Oxide.Core.Plugins;
using Oxide.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("DayNightScheduler", "RogueAssassin", "2.0.0")]
    [Description("Schedules and alters day/night cycle.")]
    public class DayNightScheduler : RustPlugin
    {
        #region Fields and Config

        private bool initialized = false;
        private TOD_Time timeComponent;
        private bool activatedDay = false;

        // Configuration settings
        private int DayLength;
        private int NightLength;
        private int AuthLevelCmds;
        private int AuthLevelFreeze;
        private bool AutoSkipNight;
        private bool AutoSkipDay;
        private bool LogAutoSkipConsole;
        private bool FreezeTimeOnLoad;
        private float TimeToFreeze;
        private string LogLevel;

        #endregion

        #region Config Class and Versioning

        // Define the ConfigData class with versioning
        private class ConfigData
        {
            [JsonProperty(PropertyName = "Version (DO NOT CHANGE)", Order = int.MaxValue)] public VersionNumber Version = new VersionNumber(2, 0, 0); // Plugin version
            [JsonProperty(PropertyName = "DayLength (Length of the day in in-game minutes)")]  public int DayLength = 30;
            [JsonProperty(PropertyName = "NightLength (Length of the night in in-game minutes)")] public int NightLength = 30;
            [JsonProperty(PropertyName = "AuthLevelCmds (Minimum auth level required to run admin commands)")] public int AuthLevelCmds = 1;
            [JsonProperty(PropertyName = "AuthLevelFreeze (Minimum auth level required to freeze/manipulate time)")] public int AuthLevelFreeze = 2;
            [JsonProperty(PropertyName = "AutoSkipNight (Automatically skip the night when enabled)")] public bool AutoSkipNight = false;
            [JsonProperty(PropertyName = "AutoSkipDay (Automatically skip the day when enabled)")] public bool AutoSkipDay = false;
            [JsonProperty(PropertyName = "LogAutoSkipConsole (Log auto-skip events to the console)")] public bool LogAutoSkipConsole = true;
            [JsonProperty(PropertyName = "FreezeTimeOnLoad (Freeze the game time immediately when the plugin loads)")] public bool FreezeTimeOnLoad = false;
            [JsonProperty(PropertyName = "TimeToFreeze (Hour to freeze time at, if freezing is enabled)")] public float TimeToFreeze = 12.0f;
            [JsonProperty(PropertyName = "LogLevel (Logging verbosity: Info, Warning, Error, Debug)")] public string LogLevel = "Info";
        }

        private ConfigData _config;

        #endregion

        #region Configuration Methods

        protected override void LoadDefaultConfig()
        {
            _config = new ConfigData();
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<ConfigData>();
            if (_config == null)
            {
                _config = new ConfigData();
                SaveConfig();
            }
            else if (_config.Version < new VersionNumber(2, 0, 0)) // Check for older versions
            {
                MigrateConfig();
                SaveConfig();
            }

            LoadConfigValues();
        }

        private void MigrateConfig()
        {
            PrintWarning("Outdated config detected. Updating...");
            _config.Version = new VersionNumber(2, 0, 0); // Update version number

            // Additional migration logic if necessary
            // For example, if the config format changes, we could handle that here
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void LoadConfigValues()
        {
            // Load values from the config
            DayLength = Mathf.Max(1, _config.DayLength);
            NightLength = Mathf.Max(1, _config.NightLength);
            AuthLevelCmds = _config.AuthLevelCmds;
            AuthLevelFreeze = _config.AuthLevelFreeze;
            AutoSkipNight = _config.AutoSkipNight;
            AutoSkipDay = _config.AutoSkipDay;
            LogAutoSkipConsole = _config.LogAutoSkipConsole;
            FreezeTimeOnLoad = _config.FreezeTimeOnLoad;
            TimeToFreeze = _config.TimeToFreeze;
            LogLevel = _config.LogLevel.ToLower(); // Ensure log level is lowercase for consistency
        }

        #endregion

        #region Oxide Hooks

        void Loaded()
        {
            RegisterPermissions();
            OnServerInitialized();
        }

        void Unload()
        {
            if (timeComponent == null || !initialized) return;
            timeComponent.OnSunrise -= OnSunrise;
            timeComponent.OnSunset -= OnSunset;
            timeComponent.OnDay -= OnDay;
            timeComponent.OnHour -= OnHour;
        }

        void OnServerInitialized()
        {
            if (TOD_Sky.Instance == null)
            {
                timer.Once(1, OnServerInitialized);
                return;
            }

            timeComponent = TOD_Sky.Instance.Components.Time;
            if (timeComponent == null)
            {
                PrintWarning("Could not fetch time component. Plugin disabled.");
                return;
            }

            SetTimeComponent();

            if (FreezeTimeOnLoad)
                HandleTimeFreeze();

            initialized = true;
        }

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"NoPermission", "You do not have permission to use this command."},
                {"TimeFrozen", "The game time has been frozen."},
                {"TimeUnfrozen", "The game time has been unfrozen."},
                {"DayAlreadyActive", "Day is already active."},
                {"NightAlreadyActive", "Night is already active."},
                {"TodHeader", "-------- Time Of Day Settings --------"},
                {"CurrentTimeOfDay", "Current Time: {0} hours"},
                {"SunriseHour", "Sunrise Hour: {0}:{1:00}"},
                {"SunsetHour", "Sunset Hour: {0}:{1:00}"},
                {"DayLength", "Day Length: {0} minutes"},
                {"NightLength", "Night Length: {0} minutes"},
                {"HelpHeader", "-------- Available Commands --------"},
                {"HelpTod", "/tod - Show current time and settings"},
                {"HelpSetDayLength", "/daynight.daylength <minutes> - Set the day length"},
                {"HelpSetNightLength", "/daynight.nightlength <minutes> - Set the night length"},
                {"HelpFreeze", "/tod freeze - Freeze the time at the current point"},
                {"HelpUnfreeze", "/tod unfreeze - Unfreeze the time and resume the cycle" }
            }, this);
        }

        private string GetMsg(string key, string userid = null) => lang.GetMessage(key, this, userid);

        #endregion

        #region Methods for Time Component

        private void SetTimeComponent()
        {
            timeComponent.ProgressTime = true;
            timeComponent.UseTimeCurve = false;
            timeComponent.OnSunrise += OnSunrise;
            timeComponent.OnSunset += OnSunset;
            timeComponent.OnDay += OnDay;
            timeComponent.OnHour += OnHour;
        }

        private void HandleTimeFreeze()
        {
            if (!initialized) return;
            timeComponent.ProgressTime = false;
            ConVar.Env.time = TimeToFreeze;
            LogDebug($"Time frozen to {TimeToFreeze} on load.");
        }

        private void SetCycle(bool isDaytime)
        {
            if (isDaytime)
            {
                if (AutoSkipDay && !AutoSkipNight)
                {
                    TOD_Sky.Instance.Cycle.Hour = TOD_Sky.Instance.SunsetTime;
                    LogAutoSkip("Daytime autoskipped");
                    OnSunset();
                    return;
                }

                timeComponent.DayLengthInMinutes = DayLength * (24.0f / (TOD_Sky.Instance.SunsetTime - TOD_Sky.Instance.SunriseTime));
                if (!activatedDay)
                    Interface.CallHook("OnTimeSunrise");
                activatedDay = true;
            }
            else
            {
                if (AutoSkipNight)
                {
                    float timeToAdd = (24 - TOD_Sky.Instance.Cycle.Hour) + TOD_Sky.Instance.SunriseTime;
                    TOD_Sky.Instance.Cycle.Hour += timeToAdd;
                    LogAutoSkip("Nighttime autoskipped");
                    OnSunrise();
                    return;
                }

                timeComponent.DayLengthInMinutes = NightLength * (24.0f / (24.0f - (TOD_Sky.Instance.SunsetTime - TOD_Sky.Instance.SunriseTime)));
                if (activatedDay)
                    Interface.CallHook("OnTimeSunset");
                activatedDay = false;
            }
        }

        private void OnDay()
        {
            if (!initialized) return;
        }

        private void OnHour()
        {
            if (!initialized) return;

            if (IsDaytime() && !activatedDay)
            {
                SetCycle(true);
                return;
            }

            if (!IsDaytime() && activatedDay)
            {
                SetCycle(false);
                return;
            }
        }

        private bool IsDaytime() => TOD_Sky.Instance.Cycle.Hour > TOD_Sky.Instance.SunriseTime && TOD_Sky.Instance.Cycle.Hour < TOD_Sky.Instance.SunsetTime;

        private void OnSunrise()
        {
            SetCycle(true);
        }

        private void OnSunset()
        {
            SetCycle(false);
        }

        #endregion

        #region Permissions

        private void RegisterPermissions()
        {
            permission.RegisterPermission("daynight.use", this);
        }

        private bool HasPermission(BasePlayer player) => permission.UserHasPermission(player.UserIDString, "daynight.use");

        private bool CheckPermission(BasePlayer player, int requiredLevel)
        {
            if (player != null && player.Connection.authLevel < requiredLevel)
            {
                SendReply(player, GetMsg("NoPermission", player.UserIDString));
                return false;
            }
            return true;
        }

        #endregion

        #region Commands

        [ChatCommand("tod")]
        private void Command_Tod(BasePlayer player, string command, string[] args)
        {
            string timeDisplay = GetTimeDisplay(TOD_Sky.Instance.Cycle.Hour);
            SendReply(player, $"{GetMsg("TodHeader")}\n{GetMsg("CurrentTimeOfDay", player.UserIDString)} {timeDisplay}");
        }

        [ConsoleCommand("tod")]
        private void ConsoleCommand_Tod(ConsoleSystem.Arg args)
        {
            string timeDisplay = GetTimeDisplay(TOD_Sky.Instance.Cycle.Hour);
            Puts($"{GetMsg("TodHeader")}\n{GetMsg("CurrentTimeOfDay", null)} {timeDisplay}");
        }

        [ChatCommand("daynight.daylength")]
        private void Command_SetDayLength(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player)) return;
            if (args.Length == 0 || !int.TryParse(args[0], out int length)) return;
            _config.DayLength = Mathf.Max(1, length);
            SaveConfig();
            SendReply(player, $"Day length set to {_config.DayLength} minutes.");
        }

        [ConsoleCommand("daynight.daylength")]
        private void ConsoleCommand_SetDayLength(ConsoleSystem.Arg args)
        {
            if (args.Args.Length == 0 || !int.TryParse(args.GetString(0), out int length)) return;
            _config.DayLength = Mathf.Max(1, length);
            SaveConfig();
            Puts($"Day length set to {_config.DayLength} minutes.");
        }

        [ChatCommand("daynight.nightlength")]
        private void Command_SetNightLength(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player)) return;
            if (args.Length == 0 || !int.TryParse(args[0], out int length)) return;
            _config.NightLength = Mathf.Max(1, length);
            SaveConfig();
            SendReply(player, $"Night length set to {_config.NightLength} minutes.");
        }

        [ConsoleCommand("daynight.nightlength")]
        private void ConsoleCommand_SetNightLength(ConsoleSystem.Arg args)
        {
            if (args.Args.Length == 0 || !int.TryParse(args.GetString(0), out int length)) return;
            _config.NightLength = Mathf.Max(1, length);
            SaveConfig();
            Puts($"Night length set to {_config.NightLength} minutes.");
        }

        [ChatCommand("tod.freeze")]
        private void Command_FreezeTime(BasePlayer player, string command, string[] args)
        {
            if (!CheckPermission(player, AuthLevelFreeze)) return;
            timeComponent.ProgressTime = false;
            SendReply(player, GetMsg("TimeFrozen"));
        }

        [ConsoleCommand("tod.freeze")]
        private void ConsoleCommand_FreezeTime(ConsoleSystem.Arg args)
        {
            timeComponent.ProgressTime = false;
            Puts(GetMsg("TimeFrozen"));
        }

        [ChatCommand("tod.unfreeze")]
        private void Command_UnfreezeTime(BasePlayer player, string command, string[] args)
        {
            if (!CheckPermission(player, AuthLevelFreeze)) return;
            timeComponent.ProgressTime = true;
            SendReply(player, GetMsg("TimeUnfrozen"));
        }

        [ConsoleCommand("tod.unfreeze")]
        private void ConsoleCommand_UnfreezeTime(ConsoleSystem.Arg args)
        {
            timeComponent.ProgressTime = true;
            Puts(GetMsg("TimeUnfrozen"));
        }

        #endregion

        #region Helper Methods

        private void LogAutoSkip(string message)
        {
            if (LogAutoSkipConsole)
                Puts(message);
        }

        private string GetTimeDisplay(float hour)
        {
            int hours = Mathf.FloorToInt(hour);
            int minutes = Mathf.FloorToInt((hour - hours) * 60);
            return $"{hours} hours {minutes} minutes"; // Always show hours and minutes
        }

        #endregion

        #region Logging

        private void LogDebug(string message)
        {
            if (ShouldLog("debug"))
                Puts(message);
        }

        private void LogInfo(string message)
        {
            if (ShouldLog("info"))
                Puts(message);
        }

        private void LogWarning(string message)
        {
            if (ShouldLog("warning"))
                Puts(message);
        }

        private void LogError(string message)
        {
            if (ShouldLog("error"))
                Puts(message);
        }

        private bool ShouldLog(string level)
        {
            var levels = new[] { "error", "warning", "info", "debug" };
            int currentLevel = Array.IndexOf(levels, LogLevel);
            int checkLevel = Array.IndexOf(levels, level);
            return checkLevel <= currentLevel;
        }

        #endregion
    }
}
