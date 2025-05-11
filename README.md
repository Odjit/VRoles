![](logo.png)
# V Roles for V Rising
V Roles is a permissions mod for V Rising. It allows you to create roles and assign commands for permitted use per role.
   - **Note:** Until BepInEx is updated for 1.1, please do not use the thunderstore version. Get the correct testing version https://wiki.vrisingmods.com/user/game_update.html.

---

Thanks to the V Rising modding and server communities for ideas and requests!
Feel free to reach out to me on discord (odjit) if you have any questions or need help with the mod.

[V Rising Modding Discord](https://vrisingmods.com/discord)

## Commands

### Role Commands
- `.role create (role)`
  - Creates a new role. If the role already exists, it will notify you. **Admin only.**
- `.role delete (role)`
  - Deletes the specified role. If it doesn't exist, it will notify you. **Admin only.**
- `.role list`
  - Lists all roles and which users are assigned to each. **Admin only.**
- `.role list (player)`
  - Lists all roles assigned to a specific player. **Admin only.**
- `.role listcommands (role)` or `.role lc (role)`
  - Lists all commands allowed by the specified role.
- `.role add (player) (role)`
  - Assigns a role to a player. Will notify if the player already has the role. **Admin only.**
- `.role remove (player) (role)`
  - Removes a role from a player. Will notify if the role wasn't assigned. **Admin only.**
- `.role allow (role) (command)`
  - Adds permission for a command to a role. **Admin only.**
  - Add a subgroup command by joining with a `.` For example: To add `.clan kick` to a role, input `clan.kick`
  - If a command is used by more than one, it will add the first it finds, or you can specify by adding the mod name first. For example: `kindredcommands.give`
- `.role disallow (role) (command)`
  - Removes a command from a role's permissions. **Admin only.**
- `.role mine`
  - List your rolls.

	
## Eventual To-Do/Possible features
- Come find out in the V Rising Modding Discord!

This mod is licensed under the AGPL-3.0 license.