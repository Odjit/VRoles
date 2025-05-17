using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using VampireCommandFramework;
using VCF.Core.Basics;
using VRoles.Commands.Converters;

namespace VRoles.Services;
class RoleService
{

    static readonly string CONFIG_PATH = Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME);
    static readonly string ROLES_PATH = Path.Combine(CONFIG_PATH, "Roles");
    static readonly string ASSIGNED_ROLES_PATH = Path.Combine(CONFIG_PATH, "user.roles");
    static readonly string ALLOWED_ADMIN_COMMANDS_PATH = Path.Combine(CONFIG_PATH, "allowedAdminCommands.txt");
    static readonly string DISALLOWED_NONADMIN_COMMANDS_PATH = Path.Combine(CONFIG_PATH, "disallowedNonadminCommands.txt");


    class RoleMiddleware : CommandMiddleware
    {
        RoleService roleService;
        public RoleMiddleware(RoleService roleService)
        {
            this.roleService = roleService;
        }

        public override bool CanExecute(ICommandContext ctx, CommandAttribute command, MethodInfo method)
        {
            if (ctx.IsAdmin) return true;

            var chatCtx = (ChatCommandContext)ctx;
            var commandName = GetCommandName(command, method);

            if (roleService.allowedAdminCommands.Contains(commandName)) return true;

            if (roleService.assignedRoles.TryGetValue(chatCtx.User.PlatformId, out var roles) &&
                roles.Any(r => roleService.rolesToCommands[r].Contains(commandName)))
                return true;

            return !command.AdminOnly && !roleService.disallowedNonadminCommands.Contains(commandName);
        }
    }


    Dictionary<string, HashSet<string>> rolesToCommands = new Dictionary<string, HashSet<string>>(StringComparer.InvariantCultureIgnoreCase);
    Dictionary<ulong, List<string>> assignedRoles = [];
    HashSet<string> allowedAdminCommands = [];
    HashSet<string> disallowedNonadminCommands = [];

    static string GetCommandName(CommandAttribute command, MethodInfo method)
    {
        var commandName = method.DeclaringType.Assembly.GetName().Name;

        if (method.DeclaringType.IsDefined(typeof(CommandGroupAttribute)))
        {
            var attribute = (CommandGroupAttribute)Attribute.GetCustomAttribute(method.DeclaringType, typeof(CommandGroupAttribute), false);
            commandName += "." + attribute.Name;
        }

        commandName += "." + command.Name;
        return commandName;
    }

    public RoleService()
    {
        foreach (var middleware in CommandRegistry.Middlewares)
        {
            if (middleware.GetType() == typeof(BasicAdminCheck))
            {
                CommandRegistry.Middlewares.Remove(middleware);
                break;
            }
        }

        var roleMiddleware = new RoleMiddleware(this);
        CommandRegistry.Middlewares.Add(roleMiddleware);

        LoadSettings();
    }

    void LoadSettings()
    {
        // Clear existing data
        rolesToCommands.Clear();
        assignedRoles.Clear();

        // Load roles and their commands
        if (Directory.Exists(ROLES_PATH))
        {
            foreach (var file in Directory.GetFiles(ROLES_PATH, "*.txt"))
            {
                var roleName = Path.GetFileNameWithoutExtension(file);
                var commands = new HashSet<string>();
                rolesToCommands[roleName] = commands;

                if (File.Exists(file))
                {
                    try
                    {
                        var lines = File.ReadAllLines(file);
                        foreach (var line in lines)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                commands.Add(line.Trim());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading role file {file}: {ex.Message}");
                    }
                }
            }
        }

        // Load role assignments
        if (File.Exists(ASSIGNED_ROLES_PATH))
        {
            try
            {
                var lines = File.ReadAllLines(ASSIGNED_ROLES_PATH);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split(':', 2);
                    if (parts.Length != 2) continue;

                    if (ulong.TryParse(parts[0], out var platformId))
                    {
                        var roles = parts[1].Split(',').Select(r => r.Trim()).Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
                        if (roles.Count > 0)
                        {
                            assignedRoles[platformId] = roles;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading role assignments: {ex.Message}");
            }
        }

        // Load allowed admin commands
        if (File.Exists(ALLOWED_ADMIN_COMMANDS_PATH))
        {
            try
            {
                var lines = File.ReadAllLines(ALLOWED_ADMIN_COMMANDS_PATH);
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        allowedAdminCommands.Add(line.Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading allowed admin commands: {ex.Message}");
            }
        }

        // Load disallowed non-admin commands
        if (File.Exists(DISALLOWED_NONADMIN_COMMANDS_PATH))
        {
            try
            {
                var lines = File.ReadAllLines(DISALLOWED_NONADMIN_COMMANDS_PATH);
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        disallowedNonadminCommands.Add(line.Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading disallowed non-admin commands: {ex.Message}");
            }
        }
    }

    void SaveRoleAssignments()
    {
        if (!Directory.Exists(CONFIG_PATH))
            Directory.CreateDirectory(CONFIG_PATH);
        File.WriteAllText(ASSIGNED_ROLES_PATH,
            String.Join("\n", assignedRoles.OrderBy(x => x.Key).Select(x => x.Key.ToString() + ":" + String.Join(", ", x.Value.OrderBy(r => r)))));
    }

    void SaveRole(string roleName)
    {
        if (!Directory.Exists(ROLES_PATH))
            Directory.CreateDirectory(ROLES_PATH);
        File.WriteAllText(Path.Join(ROLES_PATH, roleName + ".txt"),
            String.Join("\n", rolesToCommands[roleName].OrderBy(c => c)));
    }

    void SaveAllowedAdminCommands()
    {
        if (!Directory.Exists(CONFIG_PATH))
            Directory.CreateDirectory(CONFIG_PATH);
        File.WriteAllText(ALLOWED_ADMIN_COMMANDS_PATH,
            String.Join("\n", allowedAdminCommands.OrderBy(c => c)));
    }

    void SaveDisallowedNonAdminCommands()
    {
        if (!Directory.Exists(CONFIG_PATH))
            Directory.CreateDirectory(CONFIG_PATH);
        File.WriteAllText(DISALLOWED_NONADMIN_COMMANDS_PATH,
            String.Join("\n", disallowedNonadminCommands.OrderBy(c => c)));
    }

    public string MatchRole(string roleName)
    {
        foreach(var role in rolesToCommands.Keys)
        {
            if (role.Equals(roleName, StringComparison.InvariantCultureIgnoreCase))
            {
                return role;
            }
        }
        return null;
    }

    public bool CreateRole(string roleName)
    {
        if (rolesToCommands.ContainsKey(roleName)) return false;

        rolesToCommands[roleName] = [];
        SaveRole(roleName);
        return true;
    }

    public bool DeleteRole(string roleName)
    {
        if (!rolesToCommands.Remove(roleName)) return false;

        foreach (var roles in assignedRoles.Values)
            roles.Remove(roleName);
        var roleFilePath = Path.Combine(ROLES_PATH, roleName + ".txt");
        if (File.Exists(roleFilePath)) File.Delete(roleFilePath);
        return true;
    }

    public IEnumerable<string> GetRoles()
    {
        return rolesToCommands.Keys;
    }

    public IEnumerable<string> GetPlayersWithRole(string roleName)
    {
        return assignedRoles.Where(x => x.Value.Contains(roleName)).Select(x => Core.UserService.GetUser(x.Key).CharacterName.Value);
    }

    public IEnumerable<string> GetRoles(ulong platformId)
    {
        if (!assignedRoles.TryGetValue(platformId, out var roles)) return [];

        return roles;
    }

    public IEnumerable<string> GetCommandsForRole(string roleName)
    {
        return rolesToCommands[roleName];
    }

    public void AllowCommand(FoundCommand command)
    {
        if (command.adminOnly)
        {
            allowedAdminCommands.Add(command.Name);
            SaveAllowedAdminCommands();
        }
        else
        {
            if(disallowedNonadminCommands.Remove(command.Name))
                SaveDisallowedNonAdminCommands();
        }
    }

    public void DisallowCommand(FoundCommand command)
    {
        if (command.adminOnly)
        {
            if (allowedAdminCommands.Remove(command.Name))
                SaveAllowedAdminCommands();
        }
        else
        {
            disallowedNonadminCommands.Add(command.Name);
            SaveDisallowedNonAdminCommands();
        }
    }

    public IEnumerable<string> GetAllowedAdminCommands()
    {
        return allowedAdminCommands;
    }

    public IEnumerable<string> GetDisallowedNonAdminCommands()
    {
        return disallowedNonadminCommands;
    }

    public void AssignCommandToRole(string roleName, string commandName)
    {
        // Check if the role exists
        if (!rolesToCommands.TryGetValue(roleName, out var commands))
        {
            return;
        }

        commands.Add(commandName);
        SaveRole(roleName);
    }

    public bool RemoveCommandFromRole(string roleName, string commandName)
    {
        // Check if the role exists
        if (!rolesToCommands.TryGetValue(roleName, out var commands))
        {
            return false;
        }

        var result = commands.Remove(commandName);
        if (result) SaveRole(roleName);
        return result;
    }

    public bool AssignRoleToPlatformId(string roleName, ulong platformId)
    {
        if (!rolesToCommands.ContainsKey(roleName)) return false;

        if (!assignedRoles.TryGetValue(platformId, out var roles))
        {
            assignedRoles[platformId] = roles = [];
        }

        if (roles.Contains(roleName)) return false;

        roles.Add(roleName);
        SaveRoleAssignments();
        return true;
    }

    public bool RemoveRoleFromPlatformId(string roleName, ulong platformId)
    {
        if (!assignedRoles.TryGetValue(platformId, out var roles)) return false;
        var result = roles.Remove(roleName);
        if (result) SaveRoleAssignments();
        return result;
    }
}
