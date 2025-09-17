using System;
using System.Collections.Generic;
using System.Reflection;
using VampireCommandFramework;

namespace VRoles.Commands.Converters;

public record FoundCommand(string Name, bool adminOnly)
{
    public string Formatted => Name.Command();
}

internal class FoundCommandConverter : CommandArgumentConverter<FoundCommand>
{
    public override FoundCommand Parse(ICommandContext ctx, string input)
    {
        var (cmd, adminOnly) = FindCommandByName(input);

        if (cmd == null) throw ctx.Error($"Command {input.Command()} not found.");

        return new FoundCommand(cmd, adminOnly);
    }

    static (string, bool) FindCommandByName(string commandName)
    {
        // Parse the input - handle both formats: "CommandName" and "Group.CommandName" and "Assembly.Group.CommandName"
        var parts = commandName.Split('.');
        string inputAssembly = null;
        string inputGroup = null;
        var inputCommand = commandName;

        if (parts.Length == 3)  // Assembly.Group.Command
        {
            inputAssembly = parts[0];
            inputGroup = parts[1];
            inputCommand = parts[2];
        }
        else if (parts.Length == 2)  // Group.Command or Assembly.Command
        {
            // We'll check both possibilities
            inputGroup = parts[0];
            inputCommand = parts[1];
        }
        else if (parts.Length == 1)  // Just Command
        {
            inputCommand = parts[0];
        }

        // Get the AssemblyCommandMap using reflection
        var registryType = typeof(CommandRegistry);
        var assemblyMapField = registryType.GetProperty("AssemblyCommandMap",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Static |
            System.Reflection.BindingFlags.Public);

        if (assemblyMapField == null)
        {
            return (null, false); // Can't find the field
        }

        var assemblyMap = assemblyMapField.GetValue(null);

        // Get the dictionary's entries for iteration
        var dictType = assemblyMap.GetType();
        var entriesMethod = dictType.GetMethod("GetEnumerator");
        var enumerator = entriesMethod.Invoke(assemblyMap, null);
        var enumType = enumerator.GetType();
        var moveNextMethod = enumType.GetMethod("MoveNext");
        var currentProperty = enumType.GetProperty("Current");

        while ((bool)moveNextMethod.Invoke(enumerator, null))
        {
            var entry = currentProperty.GetValue(enumerator);
            var kvpType = entry.GetType();

            // Get the KeyValuePair properties
            var keyProperty = kvpType.GetProperty("Key");    // Assembly or string
            var valueProperty = kvpType.GetProperty("Value"); // Dictionary<CommandMetadata, List<string>>

            var key = keyProperty.GetValue(entry);
            var commandDict = valueProperty.GetValue(entry);

            // Handle both Assembly objects (old format) and string assembly names (new format)
            string assemblyName;
            if (key is Assembly assembly)
            {
                // Old format: Assembly object
                assemblyName = assembly.GetName().Name;
            }
            else if (key is string assemblyNameString)
            {
                // New format: string assembly name
                assemblyName = assemblyNameString;
            }
            else
            {
                continue; // Unknown format, skip
            }

            // Check if assembly name matches if specified
            if (inputAssembly != null &&
                !assemblyName.Equals(inputAssembly, StringComparison.InvariantCultureIgnoreCase))
            {
                continue;
            }

            // Iterate through the inner dictionary
            var innerDictType = commandDict.GetType();
            var innerEntriesMethod = innerDictType.GetMethod("GetEnumerator");
            var innerEnumerator = innerEntriesMethod.Invoke(commandDict, null);
            var innerEnumType = innerEnumerator.GetType();
            var innerMoveNextMethod = innerEnumType.GetMethod("MoveNext");
            var innerCurrentProperty = innerEnumType.GetProperty("Current");

            while ((bool)innerMoveNextMethod.Invoke(innerEnumerator, null))
            {
                var innerEntry = innerCurrentProperty.GetValue(innerEnumerator);
                var innerKvpType = innerEntry.GetType();

                // Get the KeyValuePair properties
                var innerKeyProperty = innerKvpType.GetProperty("Key");    // CommandMetadata
                var innerValueProperty = innerKvpType.GetProperty("Value"); // List<string>

                var metadata = innerKeyProperty.GetValue(innerEntry);
                var keys = (IEnumerable<string>)innerValueProperty.GetValue(innerEntry);

                // Get the CommandAttribute and GroupAttribute using reflection
                var metadataType = metadata.GetType();
                var attrProperty = metadataType.GetProperty("Attribute");
                var groupAttrProperty = metadataType.GetProperty("GroupAttribute");
                var methodProperty = metadataType.GetProperty("Method");

                var commandAttr = attrProperty.GetValue(metadata);
                var groupAttr = groupAttrProperty.GetValue(metadata);
                var method = (MethodInfo)methodProperty.GetValue(metadata);

                // Get command name and shorthand
                var commandAttrType = commandAttr.GetType();
                var nameProperty = commandAttrType.GetProperty("Name");
                var shortHandProperty = commandAttrType.GetProperty("ShortHand");
                var adminOnlyProperty = commandAttrType.GetProperty("AdminOnly");

                var extractedCommandName = (string)nameProperty.GetValue(commandAttr);
                var commandShortHand = (string)shortHandProperty.GetValue(commandAttr);
                var adminOnly = (bool)adminOnlyProperty.GetValue(commandAttr);

                string groupName = null;
                string groupShortHand = null;

                if (groupAttr != null)
                {
                    var groupAttrType = groupAttr.GetType();
                    var groupNameProperty = groupAttrType.GetProperty("Name");
                    var groupShortHandProperty = groupAttrType.GetProperty("ShortHand");

                    groupName = (string)groupNameProperty.GetValue(groupAttr);
                    groupShortHand = (string)groupShortHandProperty.GetValue(groupAttr);
                }

                // Check for matches
                var matches = false;

                // Case 1: Command only
                if ((inputGroup == null || assemblyName.Equals(inputGroup, StringComparison.InvariantCultureIgnoreCase)) && groupName == null)
                {
                    if (inputCommand.Equals(extractedCommandName, StringComparison.InvariantCultureIgnoreCase) ||
                        (commandShortHand != null && inputCommand.Equals(commandShortHand, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        matches = true;
                    }
                }
                // Case 2: Group and Command
                else if (inputGroup != null && groupName != null)
                {
                    var groupMatches = inputGroup.Equals(groupName, StringComparison.InvariantCultureIgnoreCase) ||
                                        (groupShortHand != null && inputGroup.Equals(groupShortHand, StringComparison.InvariantCultureIgnoreCase));

                    var cmdMatches = inputCommand.Equals(extractedCommandName, StringComparison.InvariantCultureIgnoreCase) ||
                                      (commandShortHand != null && inputCommand.Equals(commandShortHand, StringComparison.InvariantCultureIgnoreCase));

                    if (groupMatches && cmdMatches)
                    {
                        matches = true;
                    }
                }

                if (matches)
                {
                    // Return the proper command name in the format "Assembly.Group.Command"
                    var properName = assemblyName;

                    if (groupName != null)
                    {
                        properName += "." + groupName;
                    }

                    properName += "." + extractedCommandName;
                    return (properName, adminOnly);
                }
            }
        }

        return (null, false); // No match found
    }
}
