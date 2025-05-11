using VampireCommandFramework;

namespace VRoles.Commands.Converters;

public record FoundRole(string Name)
{
    public string Formatted => Name.Role();
}

internal class FoundRoleConverter : CommandArgumentConverter<FoundRole>
{
    public override FoundRole Parse(ICommandContext ctx, string input)
    {
        var role = Core.RoleService.MatchRole(input);

        if (role == null) throw ctx.Error($"Role {input.Role()} not found.");

        return new FoundRole(role);
    }
}
