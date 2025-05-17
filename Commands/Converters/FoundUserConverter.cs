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

        if (user == User.Empty)
        {
            var unboundName = Core.UserService.UnboundPlayerName(input);
            if (unboundName == null)
                throw ctx.Error($"Player {input.User()} not found.");
            throw ctx.Error($"Player {unboundName.User()} is unbound.");
        }

        return new FoundUser(user.CharacterName.Value, user);
    }
}
