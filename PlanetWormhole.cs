using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using PlanetWormhole.Data;
using System.Collections.Generic;
using System.Threading;
using static PlanetWormhole.Constants;

namespace PlanetWormhole
{
    [BepInPlugin(package, plugin, version)]
    public class PlanetWormhole: BaseUnityPlugin
    {
        private const string package = "essium.DSP.PlanetWormhole";
        private const string plugin = "PlanetWormhole";
        private const string version = "1.0.5";

        private static List<PlanetThreadObject> planetWormhole;
        private static Wormhole globalWormhole;
        private static ManualLogSource logger;

        private Harmony harmony;
        public void Start()
        {
            harmony = new Harmony(package + ":" + version);
            harmony.PatchAll(typeof(PlanetWormhole));
        }

        public void OnDestroy()
        {
            harmony.UnpatchAll();
            BepInEx.Logging.Logger.Sources.Remove(logger);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameData), "GameTick")]
        private static void _postfix_GameData_GameTick(GameData __instance,
            long time)
        {
            if (GameMain.isPaused)
            {
                return;
            }
            while(planetWormhole.Count < __instance.factoryCount)
            {
                planetWormhole.Add(new PlanetThreadObject());
            }
            for (int i = 0; i < __instance.factoryCount; i++)
            {
                planetWormhole[i].SetFactory(__instance.factories[i]);
                ThreadPool.QueueUserWorkItem(Wormhole.PatchPlanet, planetWormhole[i]);
            }
        }
        [HarmonyPrefix, HarmonyPatch(typeof(ProductionStatistics), "GameTick")]
        private static void _postfix_ProductionStatistics_GameTick(ProductionStatistics __instance,
            long time)
        {
            for (int i = 0; i < __instance.gameData.factoryCount; i++)
            {
                if (planetWormhole.Count > i)
                {
                    if (planetWormhole[i].wormhole.consumedProliferator > 0)
                    {
                        __instance.factoryStatPool[i].consumeRegister[PROLIFERATOR_MK3] += planetWormhole[i].wormhole.consumedProliferator;
                        planetWormhole[i].wormhole.consumedProliferator = 0;
                    }
                }
            }
        }

        static PlanetWormhole()
        {
            logger = BepInEx.Logging.Logger.CreateLogSource(plugin);
            planetWormhole = new List<PlanetThreadObject>();
            globalWormhole = new Wormhole();
        }

        public static void LogInfo(string msg)
        {
            logger.LogInfo(msg);
        }
    }
}
