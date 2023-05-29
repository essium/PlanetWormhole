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
        private const string version = "1.0.10";

        private static List<LocalPlanet> planetWormhole;
        private static Cosmic globalWormhole;
        private static ManualLogSource logger;

        private static ConfigEntry<bool> enableInterstellar;
        private Harmony harmony;

        [HarmonyPostfix, HarmonyPatch(typeof(GameData), "GameTick")]
        private static void _postfix_GameData_GameTick(GameData __instance,
            long time)
        {
            PerformanceMonitor.BeginSample(ECpuWorkEntry.Belt);
            while(planetWormhole.Count < __instance.factoryCount)
            {
                planetWormhole.Add(new LocalPlanet());
            }
            globalWormhole.SetData(__instance);
            globalWormhole.BeforeLocal();
            for (int i = (int)(time % PERIOD); i < __instance.factoryCount; i+=PERIOD)
            {
                planetWormhole[i].SetFactory(__instance.factories[i]);
                planetWormhole[i].SetCosmic(globalWormhole);
                ThreadPool.QueueUserWorkItem(planetWormhole[i].PatchPlanet);
            }
            for(int i = (int)(time % PERIOD); i < __instance.factoryCount; i += PERIOD)
            {
                planetWormhole[i].completeSignal.WaitOne();
            }
            globalWormhole.AfterLocal();
            PerformanceMonitor.EndSample(ECpuWorkEntry.Belt);
        }
        [HarmonyPrefix, HarmonyPatch(typeof(ProductionStatistics), "GameTick")]
        private static void _postfix_ProductionStatistics_GameTick(ProductionStatistics __instance,
            long time)
        {
            for (int i = 0; i < __instance.gameData.factoryCount; i++)
            {
                if (planetWormhole.Count > i)
                {
                    if (planetWormhole[i].consumedProliferator > 0)
                    {
                        __instance.factoryStatPool[i].consumeRegister[PROLIFERATOR_MK3] += planetWormhole[i].consumedProliferator;
                        planetWormhole[i].consumedProliferator = 0;
                    }
                }
            }
        }

        /*
        [HarmonyTranspiler, HarmonyPatch(typeof(UIBuildMenu), "SetCurrentCategory")]
        private static IEnumerable<CodeInstruction> _tranpiler_UIBuildMenu_SetCurrentCategory(IEnumerable<CodeInstruction> instructions)
        {
            return instructions;
        }
        */

        public void Start()
        {
            BindConfig();
            harmony = new Harmony(package + ":" + version);
            harmony.PatchAll(typeof(PlanetWormhole));
        }

        public void OnDestroy()
        {
            harmony.UnpatchAll();
            BepInEx.Logging.Logger.Sources.Remove(logger);
        }

        private void BindConfig()
        {
            enableInterstellar = Config.Bind("Config", "EnableInterstellar", true, "enable auto interstellar transportation");
        }

        public static bool EnableInterstellar()
        {
            return enableInterstellar.Value;
        }

        static PlanetWormhole()
        {
            logger = BepInEx.Logging.Logger.CreateLogSource(plugin);
            planetWormhole = new List<LocalPlanet>();
            globalWormhole = new Cosmic();
        }

        public static void LogInfo(string msg)
        {
            logger.LogInfo(msg);
        }
    }
}
