using VampireCommandFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace VRoles.Commands.Converters
{
    public record FoundGroup(string Name, List<FoundCommand> Commands)
    {
        public string Formatted => Name.Group();
    }

    public class FoundGroupConverter : CommandArgumentConverter<FoundGroup>
    {
        public override FoundGroup Parse(ICommandContext ctx, string input)
        {
            var (groupName, commands) = FindGroupByName(input);

            if (groupName == null || !commands.Any())
                throw ctx.Error($"No group found with name '{input}'.");

            return new FoundGroup(groupName, commands);
        }

        static (string, List<FoundCommand>) FindGroupByName(string groupName)
        {
            // Parse the input - handle both formats: "GroupName" and "Assembly.GroupName"
            var parts = groupName.Split('.');
            string inputAssembly = null;
            string inputGroup = groupName;

            if (parts.Length == 2)  // Assembly.Group
            {
                inputAssembly = parts[0];
                inputGroup = parts[1];
            }

            // Result variables
            string matchedGroupFullName = null;
            var groupCommands = new List<FoundCommand>();

            // Get the AssemblyCommandMap using reflection
            var registryType = typeof(CommandRegistry);
            var assemblyMapField = registryType.GetProperty("AssemblyCommandMap",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.Public);

            if (assemblyMapField == null)
            {
                return (null, groupCommands); // Can't find the field
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
                var keyProperty = kvpType.GetProperty("Key");    // Assembly
                var valueProperty = kvpType.GetProperty("Value"); // Dictionary<CommandMetadata, List<string>>

                var assembly = (Assembly)keyProperty.GetValue(entry);
                var assemblyName = assembly.GetName().Name;
                var commandDict = valueProperty.GetValue(entry);

                // Check if assembly name matches if specified
                if (inputAssembly != null &&
                    !assemblyName.Equals(inputAssembly, StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                // Track groups we find for this assembly
                var foundGroups = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

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

                    // Skip if no group attribute
                    if (groupAttr == null)
                    {
                        continue;
                    }

                    // Get command name and adminOnly flag
                    var commandAttrType = commandAttr.GetType();
                    var nameProperty = commandAttrType.GetProperty("Name");
                    var adminOnlyProperty = commandAttrType.GetProperty("AdminOnly");

                    var extractedCommandName = (string)nameProperty.GetValue(commandAttr);
                    var adminOnly = (bool)adminOnlyProperty.GetValue(commandAttr);

                    // Get group name
                    var groupAttrType = groupAttr.GetType();
                    var groupNameProperty = groupAttrType.GetProperty("Name");
                    var groupShortHandProperty = groupAttrType.GetProperty("ShortHand");

                    var extractedGroupName = (string)groupNameProperty.GetValue(groupAttr);
                    var groupShortHand = (string)groupShortHandProperty.GetValue(groupAttr);

                    // Check if this group matches our search
                    bool isMatch = extractedGroupName.Equals(inputGroup, StringComparison.InvariantCultureIgnoreCase) ||
                                  (groupShortHand != null && groupShortHand.Equals(inputGroup, StringComparison.InvariantCultureIgnoreCase));

                    if (isMatch)
                    {
                        // Format the full group name
                        var fullGroupName = $"{assemblyName}.{extractedGroupName}";

                        // Set the matched group name if not already set
                        if (matchedGroupFullName == null)
                        {
                            matchedGroupFullName = fullGroupName;
                        }

                        // Add this command to our result list
                        var fullCommandName = $"{assemblyName}.{extractedGroupName}.{extractedCommandName}";
                        groupCommands.Add(new FoundCommand(fullCommandName, adminOnly));
                    }

                    // Track all groups for this assembly (for future use)
                    if (!foundGroups.ContainsKey(extractedGroupName))
                    {
                        foundGroups[extractedGroupName] = $"{assemblyName}.{extractedGroupName}";
                    }
                }
            }

            return (matchedGroupFullName, groupCommands);
        }
    }
}
