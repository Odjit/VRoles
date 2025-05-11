using VRoles.Commands.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VampireCommandFramework;
using ProjectM.Network;

namespace VRoles.Commands;

[CommandGroup("role")]
class RoleCommands
{
    [Command("create", adminOnly: true)]
    public static void CreateRole(ChatCommandContext ctx, string role)
    {
        if (Core.RoleService.CreateRole(role))
        {
            ctx.Reply($"Role {role.Role()} created.");
        }
        else
        {
            ctx.Reply($"Role {role.Role()} already exists.");
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
        ctx.Reply($"Your roles are:" + String.Join(", ", Core.RoleService.GetRoles(ctx.User.PlatformId).OrderByDescending(r => r).Select(x => x.Role()))); 
    }   

    [Command("listcommands", shortHand: "lc")]
    public static void ListCommands(ChatCommandContext ctx, FoundRole role)
    {
        ctx.PaginatedReply($"Commands in the role {role.Formatted}\n" +
                           String.Join("\n", Core.RoleService.GetCommandsForRole(role.Name).OrderBy(c => c).Select(c => c.Command())));
    }

    [Command("add", adminOnly: true)]
    public static void AssignRole(ChatCommandContext ctx, FoundUser player, FoundRole role)
    {
        if (Core.RoleService.AssignRoleToPlatformId(role.Name, player.User.PlatformId))
        {
            ctx.Reply($"Role {role.Formatted} assigned to {player.Formatted}.");
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
        }
        else
        {
            ctx.Reply($"Role {role.Formatted} does not exist.");
        }
    }

    [Command("allow", adminOnly: true)]
    public static void AddCommandToRole(ChatCommandContext ctx, FoundRole role, FoundCommand command)
    {
        Core.RoleService.AssignCommandToRole(role.Name, command.Name);
        ctx.Reply($"Command {command.Formatted} added to role {role.Formatted}.");
    }

    [Command("disallow", adminOnly: true)]
    public static void RemoveCommandFromRole(ChatCommandContext ctx, FoundRole role, FoundCommand command)
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
