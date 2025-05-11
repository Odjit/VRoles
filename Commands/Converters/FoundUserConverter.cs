using ProjectM.Network;
using VampireCommandFramework;

namespace VRoles.Commands.Converters;

public record FoundUser(string Name, User User)
{
    public string Formatted => Name.User();
}

internal class FoundUserConverter : CommandArgumentConverter<FoundUser>
{
    public override FoundUser Parse(ICommandContext ctx, string input)
    {
        var user = Core.UserService.GetUser(input);

        if (user == User.Empty) throw ctx.Error($"Player {input.User()} not found.");

        return new FoundUser(user.CharacterName.Value, user);
    }
}
