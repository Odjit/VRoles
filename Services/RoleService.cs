using HarmonyLib;
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
            var chatCtx = (ChatCommandContext)ctx;
            var commandName = GetCommandName(command, method);

            if (ctx.IsAdmin)
            {
                if (RoleService.InExecute && command.AdminOnly)
                    Core.Log.LogInfo($"{chatCtx.Event.User.CharacterName}({chatCtx.Event.User.PlatformId}) is admin, skipping role check");
                return true;
            }

            if (roleService.allowedAdminCommands.Contains(commandName))
            {
                if (RoleService.InExecute)
                    Core.Log.LogInfo($"{chatCtx.Event.User.CharacterName}({chatCtx.Event.User.PlatformId}) is allowed to use {commandName} as everyone is allowed to use this admin command");
                return true;
            }

            if (roleService.assignedRoles.TryGetValue(chatCtx.User.PlatformId, out var roles) &&
                roles.Any(r => roleService.rolesToCommands[r].Contains(commandName)))
            {
                if (RoleService.InExecute)
                {
                    var allowedRoles = roles.Where(r => roleService.rolesToCommands[r].Contains(commandName));
                    Core.Log.LogInfo($"{chatCtx.Event.User.CharacterName}({chatCtx.Event.User.PlatformId}) is allowed to use {commandName} as they have the roles {string.Join(", ", allowedRoles)} that allow it");
                }
                return true;
            }

            if (command.AdminOnly)
            {
                if (RoleService.InExecute)
                    Core.Log.LogInfo($"{chatCtx.Event.User.CharacterName}({chatCtx.Event.User.PlatformId}) is not allowed to use {commandName} as it is admin only");
                return false;
            }

            if (roleService.disallowedNonadminCommands.Contains(commandName))
            {
                if (RoleService.InExecute)
                    Core.Log.LogInfo($"{chatCtx.Event.User.CharacterName}({chatCtx.Event.User.PlatformId}) is not allowed to use {commandName} as it is disallowed for nonadmins");
                return false;
            }

            return true;
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

        HookUpForSeeingIfCheckingPermission();

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

    static bool inExecuteCommandWithArgs = false;
    static bool inHelpCmd = false;
    internal static bool InExecute => inExecuteCommandWithArgs && !inHelpCmd;

    static void EnterExecuteCommandWithArgs()
    {
        inExecuteCommandWithArgs = true;
    }

    static void ExitExecuteCommandWithArgs()
    {
        inExecuteCommandWithArgs = false;
    }

    static void EnterHelpCommand()
    {
        inHelpCmd = true;
    }

    static void ExitHelpCommand()
    {
        inHelpCmd = false;
    }
    static void HookUpForSeeingIfCheckingPermission()
    {
        var executeCommandWithArgsMethod = AccessTools.Method(typeof(CommandRegistry), "ExecuteCommandWithArgs");
        if (executeCommandWithArgsMethod == null)
        {
            // PreCommand Overloading changes in VCF
            inExecuteCommandWithArgs = true;
            return;
        }

        var prefixExecute = new HarmonyMethod(typeof(RoleService), nameof(EnterExecuteCommandWithArgs));
        var postfixExecute = new HarmonyMethod(typeof(RoleService), nameof(ExitExecuteCommandWithArgs));
        Plugin.Harmony.Patch(executeCommandWithArgsMethod, prefix: prefixExecute, postfix: postfixExecute);

        var prefixHelp = new HarmonyMethod(typeof(RoleService), nameof(EnterHelpCommand));
        var postfixHelp = new HarmonyMethod(typeof(RoleService), nameof(ExitHelpCommand));

        // Use reflection to get the internal static property AssemblyCommandMap
        var assemblyCommandMapProp = typeof(CommandRegistry).GetProperty(
            "AssemblyCommandMap",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        if (assemblyCommandMapProp == null)
            return;

        var assemblyCommandMap = assemblyCommandMapProp.GetValue(null) as System.Collections.IDictionary;
        if (assemblyCommandMap == null)
            return;

        foreach (System.Collections.DictionaryEntry asmEntry in assemblyCommandMap)
        {
            // Only process the VampireCommandFramework assembly
            if (asmEntry.Key is Assembly asm && asm.GetName().Name == "VampireCommandFramework")
            {
                var commandDict = asmEntry.Value as System.Collections.IDictionary;
                if (commandDict == null)
                    continue;

                foreach (System.Collections.DictionaryEntry cmdEntry in commandDict)
                {
                    var metadata = cmdEntry.Key;
                    var commandList = cmdEntry.Value as System.Collections.IEnumerable;
                    if (commandList == null)
                        continue;

                    // Check if any command string is ".help" or ".help-all"
                    bool isHelp = false;
                    foreach (var cmd in commandList)
                    {
                        if (cmd is string s && (s == ".help" || s == ".help-all"))
                        {
                            isHelp = true;
                            break;
                        }
                    }
                    if (!isHelp)
                        continue;

                    // Get the MethodInfo from the metadata (reflection, since it's an internal record)
                    var methodProp = metadata.GetType().GetProperty("Method");
                    if (methodProp == null)
                        continue;

                    var methodInfo = methodProp.GetValue(metadata) as MethodInfo;
                    if (methodInfo == null)
                        continue;

                    // Patch the help command method
                    Plugin.Harmony.Patch(methodInfo, prefix: prefixHelp, postfix: postfixHelp);
                }
            }
        }
    }
}
