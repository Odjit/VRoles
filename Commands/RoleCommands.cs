using ProjectM;
using System;
using System.IO;
using System.Linq;
using Unity.Collections;
using VampireCommandFramework;
using VRoles.Commands.Converters;

namespace VRoles.Commands;

[CommandGroup("role")]
class RoleCommands
{
    [Command("create", adminOnly: true)]
    public static void CreateRole(ChatCommandContext ctx, string role)
    {
        string sanitizedRoleName = string.Join("_", role.Split(Path.GetInvalidFileNameChars()));
        if (Core.RoleService.CreateRole(sanitizedRoleName))
        {
            ctx.Reply($"Role {sanitizedRoleName.Role()} created.");
        }
        else
        {
            ctx.Reply($"Role {sanitizedRoleName.Role()} already exists.");
        }
    }

    [Command("delete", adminOnly: true)]
    public static void DeleteRole(ChatCommandContext ctx, FoundRole role)
    {
        if (Core.RoleService.DeleteRole(role.Name))
        {
            ctx.Reply($"Role {role.Formatted} deleted.");
        }
        else
        {
            ctx.Reply($"Role {role.Formatted} does not exist.");
        }
    }

    [Command("list", adminOnly: true)]
    public static void ListRoles(ChatCommandContext ctx)
    {
        ctx.PaginatedReply("Roles:\n" + String.Join("\n", Core.RoleService.GetRoles().OrderBy(r => r)
            .Select(x => x.Role() + ": " + String.Join(", ", Core.RoleService.GetPlayersWithRole(x).OrderBy(u => u).Select(u => u.User())))));
    }

    [Command("list", adminOnly: true)]
    public static void ListRoles(ChatCommandContext ctx, FoundUser player)
    {
        ctx.Reply($"Roles assigned to {player.Formatted}: " + String.Join(", ", Core.RoleService.GetRoles(player.User.PlatformId).OrderBy(r => r).Select(r => r.Role())));
    }

    [Command("mine")]
    public static void ListMyRoles(ChatCommandContext ctx)
    {
        var roles = Core.RoleService.GetRoles(ctx.User.PlatformId);
        if (roles.Any())
        {
            ctx.Reply($"Your roles are:" + String.Join(", ", roles.OrderByDescending(r => r).Select(x => x.Role())));
        }
        else
        {
            ctx.Reply("You have not been assigned any roles.");
        }
    }

    [Command("listcommands", shortHand: "lc")]
    public static void ListCommands(ChatCommandContext ctx, FoundRole role = null)
    {
        if (role != null)
        {
            ctx.PaginatedReply($"Commands in the role {role.Formatted}\n" +
                               String.Join("\n", Core.RoleService.GetCommandsForRole(role.Name).OrderBy(c => c).Select(c => c.Command())));
        }
        else
        {
            ctx.PaginatedReply("Commands available to everyone:\n" +
                               String.Join("\n", Core.RoleService.GetAllowedAdminCommands().OrderBy(c => c).Select(c => c.Command())));
            ctx.PaginatedReply("Commands disallowed to nonadmins or roles granting it:\n" +
                               String.Join("\n", Core.RoleService.GetDisallowedNonAdminCommands().OrderBy(c => c).Select(c => c.Command())));
        }
    }

    [Command("add", adminOnly: true)]
    public static void AssignRole(ChatCommandContext ctx, FoundUser player, FoundRole role)
    {
        if (Core.RoleService.AssignRoleToPlatformId(role.Name, player.User.PlatformId))
        {
            ctx.Reply($"Role {role.Formatted} assigned to {player.Formatted}.");
            if (player.User.PlatformId != ctx.User.PlatformId)
            {
                var message = new FixedString512Bytes($"You have been assigned the role {role.Formatted}.");
                ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, player.User, ref message);
            }
        }
        else
        {
            ctx.Reply($"Player {player.Formatted} already has the role {role.Formatted}");
        }
    }

    [Command("remove", adminOnly: true)]
    public static void RemoveRole(ChatCommandContext ctx, FoundUser player, FoundRole role)
    {
        if (Core.RoleService.RemoveRoleFromPlatformId(role.Name, player.User.PlatformId))
        {
            ctx.Reply($"Role {role.Formatted} removed from {player.Formatted}.");

            if (player.User.PlatformId != ctx.User.PlatformId)
            {
                var message = new FixedString512Bytes($"You have had the role {role.Formatted} removed.");
                ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, player.User, ref message);
            }
        }
        else
        {
            ctx.Reply($"Role {role.Formatted} does not exist.");
        }
    }

    [Command("allow", adminOnly: true)]
    public static void AddCommandToRole(ChatCommandContext ctx, FoundCommand command, FoundRole role=null)
    {
        if (role == null)
        {
            Core.RoleService.AllowCommand(command);
            ctx.Reply($"Command {command.Formatted} added to everyone.");
        }
        else
        {
            Core.RoleService.AssignCommandToRole(role.Name, command.Name);
            ctx.Reply($"Command {command.Formatted} added to role {role.Formatted}.");
        }
    }

    [Command("disallow", adminOnly: true)]
    public static void RemoveCommandFromRole(ChatCommandContext ctx, FoundCommand command, FoundRole role=null)
    {
        if (role == null)
        {
            Core.RoleService.DisallowCommand(command);
            ctx.Reply($"Command {command.Formatted} removed from everyone who isn't admin or has it via a role.");
        }
        else
        {
            if (Core.RoleService.RemoveCommandFromRole(role.Name, command.Name))
            {
                ctx.Reply($"Removed {command.Formatted} from role {role.Formatted}");
                return;
            }
            else
            {
                ctx.Reply($"Command {command.Formatted} not found in role {role.Formatted}.");
            }
        }
    }

    [Command("allowgroup", adminOnly: true)]
    public static void AllowGroup(ChatCommandContext ctx, FoundGroup group, FoundRole role = null)
    {
        foreach (var command in group.Commands)
        {
            if (role == null)
            {
                Core.RoleService.AllowCommand(command);
            }
            else
            {
                Core.RoleService.AssignCommandToRole(role.Name, command.Name);
            }
        }
        ctx.Reply($"Allowed all commands in group {group.Formatted}{(role != null ? $" for role {role.Formatted}" : " for everyone")}.");
    }

    [Command("disallowgroup", adminOnly: true)]
    public static void DisallowGroup(ChatCommandContext ctx, FoundGroup group, FoundRole role = null)
    {
        foreach (var command in group.Commands)
        {
            if (role == null)
            {
                Core.RoleService.DisallowCommand(command);
            }
            else
            {
                Core.RoleService.RemoveCommandFromRole(role.Name, command.Name);
            }
        }
        ctx.Reply($"Disallowed all commands in group {group.Formatted}{(role != null ? $" for role {role.Formatted}" : " for everyone")}.");
    }
}
