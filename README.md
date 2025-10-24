DayNightScheduler Plugin for Rust

A Rust plugin that allows players to vote to skip the night. Configurable settings for vote duration, cooldown, and percentage of players required to skip the night. Optionally, the night can be automatically skipped after a certain duration.

Features

Vote to Skip Night: Players can vote to skip the night, with configurable settings.

Auto Skip Night: Optionally automatically skip the night after a set duration.

Cooldowns and Voting Limits: Players have a cooldown period between votes to prevent spamming.

Configurable Day and Night Durations: Set how long day and night last in minutes.

Permissions Support: Control who can vote with permissions.

Installation

Download the DayNightScheduler.cs file from this repository.

Place the DayNightScheduler.cs file in the oxide/plugins directory on your Rust server.

Restart or reload the server to load the plugin.

Configuration

The configuration is stored in a JSON file located in oxide/config/DayNightScheduler.json. On the first load, the plugin will create this file with default values.

Configuration Options:
{
  "Version": "1.5.0",
  "DayDurationMinutes": 20,
  "NightDurationMinutes": 10,
  "VoteCooldown": 30.0,
  "VoteDuration": 60.0,
  "RequiredVotes": 50,
  "EnableVoteToSkipNight": true,
  "AutoSkipNight": false
}


Version: Plugin version.

DayDurationMinutes: The duration of the day in minutes (default: 20 minutes).

NightDurationMinutes: The duration of the night in minutes (default: 10 minutes).

VoteCooldown: Time (in seconds) players must wait before voting again (default: 30 seconds).

VoteDuration: Time (in seconds) players have to vote to skip the night (default: 60 seconds).

RequiredVotes: The percentage of players required to vote to skip the night (default: 50%).

EnableVoteToSkipNight: Whether voting to skip the night is enabled (default: true).

AutoSkipNight: If enabled, the night will be skipped automatically after the set night duration (default: false).

Commands
/votenight

Usage: /votenight

Players can use this command to vote to skip the night.

Only available during the night phase, and voting lasts for the duration specified in the config (VoteDuration).

Players can vote once per night, with a cooldown period between votes (VoteCooldown).

When the required percentage of votes is reached, the night will be skipped immediately.

Permissions

daynightscheduler.vote: Grants permission to vote to skip the night. By default, this permission is available to all players, but you can configure it to be restricted.

To assign this permission to a player, use the following command:

oxide.grant user <player_name> daynightscheduler.vote

Plugin Features
1. Night Cycle Management

Automatically handles day/night transitions based on configurable durations.

Sends messages to players when the night begins, when the night ends, and when they vote to skip the night.

2. Voting System

Players can vote to skip the night by typing /votenight in the chat.

Each player can vote only once per night, and there is a cooldown period (VoteCooldown) before they can vote again.

Voting ends after the specified duration (VoteDuration) or as soon as the required number of votes is reached.

If enough votes are cast, the night is skipped, and the server transitions to day.

3. Auto Skip Night (Optional)

If enabled, the server will automatically skip the night after the specified duration (NightDurationMinutes), without needing any votes.

Troubleshooting

Voting Not Working: Ensure the server time is correctly synced, and players are trying to vote during the night phase.

Permissions Issues: Ensure the daynightscheduler.vote permission is correctly assigned to players.

Night Not Skipping Automatically: Make sure the AutoSkipNight option is enabled in the config and that the night duration is set correctly.

Version History
v1.5.0

Initial release with configurable day/night cycle, voting system, and cooldowns.

Credits

Developer: RogueAssassin

License

This project is licensed under the MIT License - see the LICENSE
 file for details.