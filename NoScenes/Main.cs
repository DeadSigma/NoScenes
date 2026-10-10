using HarmonyLib;
using HMLLibrary;
using System.Reflection;
using UnityEngine;

public class Main : Mod
{
    private const string HarmonyId = "El.NoScenes";
    private Harmony harmony;

    public void Start()
    {
        harmony = new Harmony(HarmonyId);
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        Debug.Log("[NoScenes] Loaded");
    }

    public void OnModUnload()
    {
        harmony?.UnpatchAll(HarmonyId);
        harmony = null;

        Destroy(gameObject);
    }
}

[HarmonyPatch(typeof(SceneLoader), "LoadScenes")]
internal static class SceneLoader_LoadScenes_Patch
{
    private static bool Prefix(SceneLoader __instance)
    {
        __instance.atStartLoadSetting = SceneLoader.LoadSetting.DoNotLoad;

        __instance.asyncOperations.Clear();
        __instance.loadedLandmarks.Clear();
        __instance.loadedLandmarkConnections.Clear();

        Raft_Network.IsAllLandmarksLoaded = true;

        Debug.Log("[NoScenes] Landmark scene loading skipped");

        return false;
    }
}
