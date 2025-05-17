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
- `.role create (role)` or `.role c (role)`
  - Creates a new role. If the role already exists, it will notify you. **Admin only.**
- `.role delete (role)` or `.role d (role)`
  - Deletes the specified role. If it doesn't exist, it will notify you. **Admin only.**
- `.role list` or `.role l`
  - Lists all roles and which users are assigned to each. **Admin only.**
- `.role list (player)` or `.role l (player)`
  - Lists all roles assigned to a specific player. **Admin only.**
- `.role mine` or `.role my`
  - Lists your current roles.
- `.role listcommands [role]` or `.role lc [role]`
  - Lists all commands allowed by the specified role.
  - If no role is specified, shows commands available to everyone and commands disallowed to non-admins.
- `.role add (player) (role)` or `.role a (player) (role)`
  - Assigns a role to a player. Will notify if the player already has the role. **Admin only.**
- `.role remove (player) (role)` or `.role r (player) (role)`
  - Removes a role from a player. Will notify if the role wasn't assigned. **Admin only.**
- `.role allow (command) [role]` or `.role ac (command) [role]`
  - Adds permission for a command to a role. **Admin only.**
  - If no role is specified, allows the command for everyone.
  - Add a subgroup command by joining with a `.` For example: To add `.clan kick` to a role, input `clan.kick`
  - If a command is used by more than one, it will add the first it finds, or you can specify by adding the mod name first. For example: `kindredcommands.give`
- `.role disallow (command) [role]` or `.role dc (command) [role]`
  - Removes a command from a role's permissions. **Admin only.**
  - If no role is specified, disallows the command for everyone who isn't admin or doesn't have it via a role.
- `.role allowgroup (group) [role]` or `.role ag (group) [role]`
  - Allows all commands in the specified command group for a role. **Admin only.**
  - If no role is specified, allows all commands in the group for everyone.
- `.role disallowgroup (group) [role]` or `.role dg (group) [role]`
  - Disallows all commands in the specified command group for a role. **Admin only.**
  - If no role is specified, disallows all commands in the group for everyone who isn't admin or doesn't have them via a role.
	
## Eventual To-Do/Possible features
- Come find out in the V Rising Modding Discord!

## Credits
- As always, thanks to [Deca](https://github.com/decaprime/) for [VampireCommandFramework](https://github.com/decaprime/VampireCommandFramework). Additionally for use of his CommandContextExtensions and formatting.

This mod is licensed under the AGPL-3.0 license.