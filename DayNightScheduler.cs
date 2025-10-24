using Oxide.Core.Plugins;
using Oxide.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("DayNightScheduler", "RogueAssassin", "1.7.0")]
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

        #endregion

        #region Configuration Class

        public class Configuration
        {
            public VersionNumber Version = new VersionNumber(1, 7, 0); // Version Number set
            public int DayLength = 30;
            public int NightLength = 30;
            public int AuthLevelCmds = 1;
            public int AuthLevelFreeze = 2;
            public bool AutoSkipNight = false;
            public bool AutoSkipDay = false;
            public bool LogAutoSkipConsole = true;
            public bool FreezeTimeOnLoad = false;
            public float TimeToFreeze = 12.0f;
        }

        private Configuration config;

        #endregion

        #region Oxide Hooks

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file.");
            config = new Configuration();
            SaveConfig();
        }

        private void SaveConfig()
        {
            Config.WriteObject(config, true);
        }

        private void LoadConfigValues()
        {
            if (config.Version == null || config.Version < new VersionNumber(1, 7, 0))
            {
                PrintWarning("Old configuration detected, migrating...");
                config.Version = new VersionNumber(1, 5, 0);
                SaveConfig();
            }
			
            // Validate and load config values
            DayLength = Mathf.Max(1, config.DayLength);
            NightLength = Mathf.Max(1, config.NightLength);
            AuthLevelCmds = config.AuthLevelCmds;
            AuthLevelFreeze = config.AuthLevelFreeze;
            AutoSkipNight = config.AutoSkipNight;
            AutoSkipDay = config.AutoSkipDay;
            LogAutoSkipConsole = config.LogAutoSkipConsole;
            FreezeTimeOnLoad = config.FreezeTimeOnLoad;
            TimeToFreeze = config.TimeToFreeze;
        }

        private void InitConfig()
        {
            config = Config.ReadObject<Configuration>();
            if (config == null)
            {
                LoadDefaultConfig();
            }
            LoadConfigValues();
        }

        void Loaded()
        {
            InitConfig();
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

            if (config.FreezeTimeOnLoad)
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

        private bool CheckPermission(ConsoleSystem.Arg arg, int requiredLevel)
        {
            if (arg.Connection != null && arg.Connection.authLevel < requiredLevel)
            {
                SendReply(arg, GetMsg("NoPermission", arg.Connection.userid.ToString()));
                return false;
            }
            return true;
        }

        #endregion

        #region Console Commands

        [ConsoleCommand("daynight.daylength")]
        private void ConsoleDayLength(ConsoleSystem.Arg arg)
        {
            if (!initialized || !CheckPermission(arg, AuthLevelCmds)) return;

            if (arg.Args == null || arg.Args.Length < 1)
            {
                SendReply(arg, $"Current 'dayLength' is {DayLength}");
                return;
            }

            if (!int.TryParse(arg.Args[0], out int newDayLength) || newDayLength < 1)
            {
                SendReply(arg, "Invalid day length. Must be a number greater than 0.");
                return;
            }

            DayLength = newDayLength;
            config.DayLength = newDayLength;
            SaveConfig();

            SendReply(arg, $"Day length set to {DayLength} minutes.");
            SetCycle(true);
        }

        [ConsoleCommand("daynight.nightlength")]
        private void ConsoleNightLength(ConsoleSystem.Arg arg)
        {
            if (!initialized || !CheckPermission(arg, AuthLevelCmds)) return;

            if (arg.Args == null || arg.Args.Length < 1)
            {
                SendReply(arg, $"Current 'nightLength' is {NightLength}");
                return;
            }

            if (!int.TryParse(arg.Args[0], out int newNightLength) || newNightLength < 1)
            {
                SendReply(arg, "Invalid night length. Must be a number greater than 0.");
                return;
            }

            NightLength = newNightLength;
            config.NightLength = newNightLength;
            SaveConfig();

            SendReply(arg, $"Night length set to {NightLength} minutes.");
            SetCycle(false);
        }

        #endregion

        #region Logging

        private void LogAutoSkip(string message)
        {
            if (LogAutoSkipConsole)
                Puts(message);
        }

        private void LogDebug(string message)
        {
            if (config.LogAutoSkipConsole) // Enable detailed logging with the config setting
                Puts(message);
        }

        #endregion
    }
}
