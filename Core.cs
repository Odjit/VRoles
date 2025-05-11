using BepInEx.Logging;
using VRoles.Services;
using Unity.Entities;

namespace VRoles;

internal static class Core
{
    static World server;
    public static World Server => GetServer();

    static World GetServer()
    {
        server ??= GetWorld("Server") ?? throw new System.Exception("There is no Server world (yet). Did you install a server mod on the client?");
        return server;
    }

	public static EntityManager EntityManager { get; } = Server.EntityManager;
    public static RoleService RoleService { get; private set; }

    static UserService userService;
    public static UserService UserService => GetUserService();

    static UserService GetUserService()
    {
        userService ??= new UserService();
        return userService;
    }

	public static ManualLogSource Log { get; } = Plugin.LogInstance;

	internal static void InitializeAfterLoaded()
	{
		if (_hasInitialized) return;
        RoleService = new RoleService();

        _hasInitialized = true;
		Log.LogInfo($"VRoles initialized");
    }
	private static bool _hasInitialized = false;

	private static World GetWorld(string name)
	{
		foreach (var world in World.s_AllWorlds)
		{
			if (world.Name == name)
			{
				return world;
			}
		}

		return null;
    }
}
