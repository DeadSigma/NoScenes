using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using HMLLibrary;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Main : Mod
{
    private const string HarmonyId = "El_Neuman.NoScenes";
    private Harmony harmony;

    // true - локация не загружается
    // false - локация загружается
    // По умолчанию все 38 локаций отключены
    internal static readonly Dictionary<string, bool> SkipIslands =
        new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
    {
        { "15#Landmark_Raft#Floating raft", true }, // Дрейфующий заброшенный плот - вариант 1
        { "16#Landmark_Raft#Floating raft", true }, // Дрейфующий заброшенный плот - вариант 2
        { "17#Landmark_Raft#Floating raft", true }, // Дрейфующий заброшенный плот - вариант 3
        { "18#Landmark_Raft#Floating raft", true }, // Дрейфующий заброшенный плот - вариант 4
        { "19#Landmark_Radar#Big radio tower", true }, // Радиовышка - первая сюжетная локация
        { "21#Landmark_Raft#Floating raft", true }, // Дрейфующий заброшенный плот - вариант 5

        { "28#Landmark_Big#OG", true }, // Большой тропический остров - вариант OG
        { "29#Landmark_Big#Cresent", true }, // Большой тропический остров - вариант Cresent
        { "30#Landmark_Big#Twin peak", true }, // Большой тропический остров - вариант Twin peak
        { "31#Landmark_Pilot#", true }, // Редкий остров с разбившимся самолётом
        { "32#Landmark_Big#", true }, // Большой тропический остров - вариант без названия
        { "33#Landmark_Boat#", true }, // Редкий остров с кораблекрушением

        { "34#Landmark_Small#1", true }, // Маленький тропический остров - вариант 1
        { "35#Landmark_Small#2", true }, // Маленький тропический остров - вариант 2
        { "36#Landmark_Small#3", true }, // Маленький тропический остров - вариант 3
        { "37#Landmark_Small#4", true }, // Маленький тропический остров - вариант 4
        { "38#Landmark_Small#5", true }, // Маленький тропический остров - вариант 5
        { "39#Landmark_Small#6", true }, // Маленький тропический остров - вариант 6
        { "40#Landmark_Small#7", true }, // Маленький тропический остров - вариант 7
        { "41#Landmark_Small#8", true }, // Маленький тропический остров - вариант 8
        { "42#Landmark_Small#9", true }, // Маленький тропический остров - вариант 9
        { "43#Landmark_Small#10", true }, // Маленький тропический остров - вариант 10

        { "44#Landmark_Vasagatan", true }, // Вазагатан - заброшенная яхта с сюжетными записями
        { "45#Landmark_BalboaIsland", true }, // Балбоа - лесной остров с медведями и радиостанциями
        { "46#Landmark_BalboaSmall#1", true }, // Лесной остров Балбоа - вариант 1, сосны и берёзы
        { "47#Landmark_BalboaSmall#2", true }, // Лесной остров Балбоа - вариант 2, пчёлы и медведи
        { "48#Landmark_BalboaSmall#3", true }, // Лесной остров Балбоа - вариант 3, лесной биом
        { "49#Landmark_CaravanIsland#RealDeal", true }, // Караван-таун - пустынный город из трейлеров
        { "50#Landmark_Tangaroa#", true }, // Тангароа - футуристический город под куполом
        { "51#Landmark_CaravanSmall#1", true }, // Пустынный остров Каравана - вариант 1
        { "52#Landmark_CaravanSmall#2", true }, // Пустынный остров Каравана - вариант 2
        { "53#Landmark_CaravanSmall#3", true }, // Пустынный остров Каравана - вариант 3
        { "54#Landmark_VarunaPoint#", true }, // Варуна-Пойнт - затопленные недостроенные высотки
        { "55#Landmark_Temperance#", true }, // Темперанс - полярная станция, снег и белые медведи
        { "56#Landmark_Utopia#", true }, // Утопия - финальное плавучее поселение
        { "57#Landmark_TemperanceSmall#1", true }, // Арктический остров - вариант 1, снег и льдины
        { "58#Landmark_TemperanceSmall#2", true }, // Арктический остров - вариант 2, снег и льдины
        { "59#Landmark_TemperanceSmall#3", true } // Арктический остров - вариант 3, снег и льдины
    };

    public void Start()
    {
        harmony = new Harmony(HarmonyId);
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        Debug.Log("[NoScenes] Selective landmark loading enabled");
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
    private static readonly FieldInfo TotalField =
        AccessTools.Field(typeof(SceneLoader), "scenesToLoadCount");

    private static readonly FieldInfo LoadedField =
        AccessTools.Field(typeof(SceneLoader), "loadedScenesCount");

    private static readonly MethodInfo ActivateMethod =
        AccessTools.Method(typeof(SceneLoader), "OnSceneActivate");

    private static bool Prefix(SceneLoader __instance)
    {
        if (TotalField == null || LoadedField == null || ActivateMethod == null)
        {
            Debug.LogError("[NoScenes] SceneLoader structure changed - original loading preserved");
            return true;
        }

        int originalCount = (int)TotalField.GetValue(__instance);
        var selectedScenes = new List<string>();

        for (int index = 2; index < originalCount + 2; index++)
        {
            string name = GetSceneName(index);
            if (string.IsNullOrEmpty(name))
                continue;

            bool skip;
            if (!Main.SkipIslands.TryGetValue(name, out skip))
            {
                // Неизвестные сцены не загружаются до добавления в список
                Debug.LogWarning("[NoScenes] Unlisted scene skipped: " + name);
                skip = true;
            }

            if (!skip)
                selectedScenes.Add(name);
        }

        LoadedField.SetValue(__instance, 0);
        TotalField.SetValue(__instance, selectedScenes.Count);

        if (selectedScenes.Count == 0)
        {
            __instance.atStartLoadSetting = SceneLoader.LoadSetting.DoNotLoad;
            Raft_Network.IsAllLandmarksLoaded = true;
            Debug.Log("[NoScenes] All landmark scenes skipped");
            return false;
        }

        __instance.atStartLoadSetting = SceneLoader.LoadSetting.LoadAll;
        Raft_Network.IsAllLandmarksLoaded = false;

        int started = 0;
        int completed = 0;

        foreach (string selected in selectedScenes)
        {
            string sceneName = selected;
            AsyncOperation operation;

            try
            {
                operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            }
            catch (Exception ex)
            {
                Debug.LogError("[NoScenes] Cannot load " + sceneName + ": " + ex);
                continue;
            }

            if (operation == null)
            {
                Debug.LogError("[NoScenes] LoadSceneAsync returned null: " + sceneName);
                continue;
            }

            started++;
            __instance.asyncOperations.Add(operation);
            Debug.Log("[NoScenes] Loading " + sceneName);

            operation.completed += delegate (AsyncOperation handle)
            {
                try
                {
                    ActivateMethod.Invoke(__instance, new object[]
                    {
                        SceneManager.GetSceneByName(sceneName)
                    });

                    Debug.Log("[NoScenes] Finished " + sceneName);
                }
                catch (Exception ex)
                {
                    Debug.LogError("[NoScenes] Scene activation failed for " + sceneName + ": " + ex);
                }
                finally
                {
                    completed++;
                    __instance.asyncOperations.Remove(operation);

                    if (__instance.asyncOperations.Count == 0)
                        Resources.UnloadUnusedAssets();

                    if (completed >= (int)TotalField.GetValue(__instance) &&
                        __instance.asyncOperations.Count == 0 &&
                        !Raft_Network.IsAllLandmarksLoaded)
                    {
                        Debug.LogWarning("[NoScenes] Landmark loading finished with activation warnings");
                        Raft_Network.IsAllLandmarksLoaded = true;
                    }
                }
            };
        }

        // Невыполненные запросы не должны учитываться в прогрессе
        TotalField.SetValue(__instance, started);

        if (started == 0)
            Raft_Network.IsAllLandmarksLoaded = true;

        Debug.Log("[NoScenes] Selected scenes: " + started + " / " + originalCount);
        return false;
    }

    private static string GetSceneName(int buildIndex)
    {
        string scenePath = SceneUtility.GetScenePathByBuildIndex(buildIndex);
        if (string.IsNullOrEmpty(scenePath))
            return null;

        int start = scenePath.LastIndexOf('/') + 1;
        int dot = scenePath.LastIndexOf('.');
        if (dot < start)
            dot = scenePath.Length;

        return scenePath.Substring(start, dot - start);
    }
}
