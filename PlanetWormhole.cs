using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using PlanetWormhole.Data;
using System.Collections.Generic;

namespace PlanetWormhole
{
    [BepInPlugin(package, plugin, version)]
    public class PlanetWormhole: BaseUnityPlugin
    {
        private const string package = "essium.DSP.PlanetWormhole";
        private const string plugin = "PlanetWormhole";
        private const string version = "1.0.2";

        private static ConfigEntry<bool> enable;
        private static List<Wormhole> wormholes;
        private static ManualLogSource logger;

        private Harmony harmony;
        public void Start()
        {
            enable = Config.Bind("Config", "Enable", true, "whether enable plugin");
            harmony = new Harmony(package + ":" + version);
            harmony.PatchAll(typeof(PlanetWormhole));
        }

        public void OnDestroy()
        {
            harmony.UnpatchAll();
            BepInEx.Logging.Logger.Sources.Remove(logger);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameData), "GameTick")]
        private static void PatchGameTick(PlanetFactory __instance,
            long time,
            ref int ___factoryCount,
            ref PlanetFactory[] ___factories)
        {
            if (!enable.Value)
            {
                return;
            }
            while(wormholes.Count < ___factoryCount)
            {
                wormholes.Add(new Wormhole());
            }
            for (int i = 0; i < ___factoryCount; i++)
            {
                if (time % 30 == i % 30)
                {
                    wormholes[i].Patch(___factories[i]);
                }
            }
        }

        static PlanetWormhole()
        {
            logger = BepInEx.Logging.Logger.CreateLogSource(plugin);
            wormholes = new List<Wormhole>();
        }

        public static void LogInfo(string msg)
        {
            logger.LogInfo(msg);
        }
    }
}
