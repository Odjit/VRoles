using HarmonyLib;
using ProjectM;


namespace VRoles.Patches;

[HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUpdate))]
public static class InitializationPatch
{
	[HarmonyPostfix]
	public static void OneShot_AfterLoad_InitializationPatch()
	{
		Core.InitializeAfterLoaded();
		Plugin.Harmony.Unpatch(typeof(ServerBootstrapSystem).GetMethod("OnUpdate"), typeof(InitializationPatch).GetMethod("OneShot_AfterLoad_InitializationPatch"));
	}
}
