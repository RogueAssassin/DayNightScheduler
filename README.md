# DayNightScheduler

### **Author:** RogueAssassin  
### **Version:** 1.7.0

The **DayNightScheduler** plugin for **Rust** allows server administrators to schedule and manipulate the in-game day/night cycle. With this plugin, you can configure the duration of day and night, skip day/night cycles automatically, freeze time on server load, and more.

---

## **Features**

- **Customizable Day & Night Length:** Adjust the length of day and night in minutes.
- **Automatic Day/Night Skipping:** Automatically skip night or day based on server settings.
- **Time Freezing:** Option to freeze the time on server load.
- **Permissions System:** Different levels of permission for various commands.
- **Localization Support:** Easy translation to other languages via the `lang` system.

---

## **Configuration**

The plugin comes with a configurable JSON file. Here are the configurable options:

| **Option**                | **Description**                                           | **Default Value** |
|---------------------------|-----------------------------------------------------------|-------------------|
| `DayLength`                | Length of day in minutes                                  | 30                |
| `NightLength`              | Length of night in minutes                                | 30                |
| `AuthLevelCmds`            | The required authorization level for `daynight.*` commands (0 = No permission, 1 = Moderator, 2 = Admin) | 1                 |
| `AuthLevelFreeze`          | The required authorization level to use time freeze commands | 2                 |
| `AutoSkipNight`            | Automatically skip night (only if `AutoSkipDay` is false) | false             |
| `AutoSkipDay`              | Automatically skip day                                    | false             |
| `LogAutoSkipConsole`       | Log auto-skip actions to console                          | true              |
| `FreezeTimeOnLoad`         | Freeze the time when the server is loaded                 | false             |
| `TimeToFreeze`             | The time in hours to freeze the game time on server load  | 12.0              |

---

## **Commands**

### **Available Console Commands**

- `/tod`  
  Displays the current time of day and other related settings.

- `daynight.daylength <minutes>`  
  Set the length of the day cycle in minutes.

- `daynight.nightlength <minutes>`  
  Set the length of the night cycle in minutes.

### **Permissions**

- `daynight.use`  
  Permission required to use the `/tod` command and interact with time settings.

- `daynight.freeze`  
  Permission to freeze/unfreeze the time in the game.

---

## **Installation**

1. **Download the Plugin:**  
   Download the `DayNightScheduler` plugin file.

2. **Place the Plugin:**  
   Upload the `.cs` file to the **oxide/plugins** folder on your Rust server.

3. **Config File:**  
   On the first load of the plugin, a configuration file will be generated in the **oxide/config** folder (`DayNightScheduler.json`).

4. **Reload Plugins:**  
   Reload the plugin using the Oxide command:
oxide.reload DayNightScheduler

markdown
Copy code

---

## **Usage**

### **Configuration**

After installation, the plugin will automatically generate a configuration file (`DayNightScheduler.json`) in the **oxide/config** directory. You can customize the day and night length, time freezing settings, and other options through this file.

- **Day/Night Length:** The default day and night lengths are 30 minutes each. You can change these to suit your server's needs by modifying `DayLength` and `NightLength` in the config file.

- **Auto-Skip:** Enable `AutoSkipDay` or `AutoSkipNight` to automatically skip either the day or night cycle based on server conditions.

- **Freeze Time on Load:** If `FreezeTimeOnLoad` is set to true, the time will freeze at the specified `TimeToFreeze` on server startup.

### **Console Commands**

You can use the following console commands to adjust the day and night lengths:

- **Set Day Length:**
daynight.daylength <minutes>

pgsql
Copy code
Sets the day length (in minutes) to the value you specify.

- **Set Night Length:**
daynight.nightlength <minutes>

yaml
Copy code
Sets the night length (in minutes) to the value you specify.

### **Permissions**

Permissions control who can execute certain commands:

- **`daynight.use`**: Allows the player to use the `/tod` command and view the current time settings.
- **`daynight.freeze`**: Allows the player to freeze/unfreeze the time.

You can manage these permissions through the Oxide permission system.

---

## **Logging**

The plugin supports logging of automatic day/night skips and time freezes to the server console. You can toggle this feature with the `LogAutoSkipConsole` setting in the configuration file.

---

## **Changelog**

### **Version 1.7.0 (Current Version)**
- Initial release with full day/night cycle management, time freezing, and auto-skip options.

---

## **Troubleshooting**

1. **Time Not Updating:**  
 Ensure that the plugin has successfully loaded and that the `TOD_Sky` component is present in the game.

2. **Permissions Issues:**  
 Ensure you have the correct permissions set for the commands you wish to use.

3. **Auto-Skip Not Working:**  
 Double-check that both `AutoSkipDay` and `AutoSkipNight` are configured as desired in the configuration file.

---

## **License**

This plugin is licensed under the **MIT License**. Feel free to modify and distribute it as long as you adhere to the terms of the license.

---
