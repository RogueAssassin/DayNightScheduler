using Oxide.Core.Plugins;
using Oxide.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("DayNightScheduler", "RogueAssassin", "1.5.0")]
    [Description("Allows players to vote to skip the night with configurable settings")]
    public class DayNightScheduler : RustPlugin
    {
        #region Configuration & Fields

        private Configuration config;
        private bool isNight = false;
        private float voteEndTime;
        private HashSet<BasePlayer> votedPlayers = new HashSet<BasePlayer>();
        private Dictionary<BasePlayer, float> playerVoteCooldowns = new Dictionary<BasePlayer, float>();
        private bool initialized = false;
        private DateTime lastVoteTime;

        #endregion

        #region Configuration Class

        public class Configuration
        {
            public VersionNumber Version = new VersionNumber(1, 5, 0);
            public int DayDurationMinutes = 20;
            public int NightDurationMinutes = 10;
            public float VoteCooldown = 30f;
            public float VoteDuration = 60f;
            public int RequiredVotes = 50; // Percentage of players needed to skip night
            public bool EnableVoteToSkipNight = true;
            public bool AutoSkipNight = false;
        }

        #endregion

        #region Config Handling

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
            if (config.Version == null || config.Version < new VersionNumber(1, 5, 0))
            {
                PrintWarning("Old configuration detected, migrating...");
                config.Version = new VersionNumber(1, 5, 0);
                SaveConfig();
            }
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
            LoadVariables();
        }

        private void LoadVariables()
        {
            // Assign config values to local variables
            // (same as before, no changes needed here)
        }

        #endregion

        #region Oxide Hooks

        void OnServerInitialized()
        {
            timer.Every(10f, CheckDayNightCycle);
        }

        #endregion

        #region Time Management

        private void CheckDayNightCycle()
        {
            if (isNight && TimeOfDay() > config.NightDurationMinutes * 60f)
            {
                EndNight();
            }
            else if (!isNight && TimeOfDay() > config.DayDurationMinutes * 60f)
            {
                StartNight();
            }

            if (config.AutoSkipNight && isNight && TimeOfDay() > config.NightDurationMinutes * 60f)
            {
                EndNight();
                SendChatMessage("Night has been skipped automatically.");
            }
        }

        private void StartNight()
        {
            isNight = true;
            voteEndTime = Time.realtimeSinceStartup + config.VoteDuration;
            SendChatMessage("Night has begun. You have " + config.VoteDuration + " seconds to vote to skip the night.");
        }

        private void EndNight()
        {
            isNight = false;
            votedPlayers.Clear();
            SendChatMessage("Night has ended.");
        }

        private void SendChatMessage(string message)
        {
            PrintToChat(message);
        }

        private float TimeOfDay()
        {
            return TOD_Sky.Instance.Cycle.Hour * 60f; // Time of day in minutes
        }

        #endregion

        #region Voting System

        [ChatCommand("votenight")]
        private void VoteToSkipNight(BasePlayer player, string command, string[] args)
        {
            if (!config.EnableVoteToSkipNight) return;

            if (isNight && Time.realtimeSinceStartup < voteEndTime)
            {
                if (playerVoteCooldowns.ContainsKey(player) && Time.realtimeSinceStartup - playerVoteCooldowns[player] < config.VoteCooldown)
                {
                    SendChatMessage(player.displayName + ", you must wait before voting again.");
                    return;
                }

                if (votedPlayers.Contains(player))
                {
                    SendChatMessage(player.displayName + ", you have already voted.");
                    return;
                }

                votedPlayers.Add(player);
                playerVoteCooldowns[player] = Time.realtimeSinceStartup;

                int totalPlayers = covalence.Players.All.Count();
                int votesNeeded = (int)(totalPlayers * (config.RequiredVotes / 100f));

                SendChatMessage(player.displayName + " has voted to skip the night!");

                if (votedPlayers.Count >= votesNeeded)
                {
                    EndNight();
                    SendChatMessage("Night skipped. The server has moved to daytime!");
                }
            }
            else if (!isNight)
            {
                SendChatMessage("It is not night time right now.");
            }
            else
            {
                SendChatMessage("Voting time has ended for this night.");
            }
        }

        #endregion

        #region Permissions

        private void RegisterPermissions()
        {
            permission.RegisterPermission("daynightscheduler.vote", this);
        }

        private bool HasPermission(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);

        #endregion
    }
}
