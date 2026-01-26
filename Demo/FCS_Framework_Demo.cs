using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ConfigurationManager;
using FCSAPI;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Collections;
using UnityEngine;
namespace FCSSets;

[BepInPlugin("FCS_FrameworkMOD_Demo", "FCS_FrameworkMOD_Demo", "0.0.5")]
//[BepInDependency("NuclearOptionFCSFrameworkMOD", BepInDependency.DependencyFlags.HardDependency)]
public class FCSSets : BaseUnityPlugin
{
    private static string ConfigPath => Path.Combine(Paths.PluginPath, "CustomFCS", "FlightControlConfig.json");
    private static Dictionary<string, FlightControlParam> LoadedParams = new();
    private bool done = false;

    private ConfigEntry<KeyboardShortcut> ShowCounter { get; set; }


    void Update()
    {
        if (done && !(Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.L))) return;
        var api = FCSAPI.FCSPatch_API.Instance;
        var VEUapi = FCSAPI.FCSPatch_API.VEU_Instance;
        Logger.LogInfo($"API assembly seen: {typeof(FCSAPI.FCSPatch_API).Assembly.FullName}");
        if (api != null)
        {
            if (!api.IsReady()) return;
            Logger.LogInfo($"FCSPatch API Ready");
            LoadConfig();

            api.SetFCS_Global(LoadedParams["CI22"], AircraftType.CI22);
            api.SetFCS_Global(LoadedParams["TA30"], AircraftType.TA30);
            api.SetFCS_Global(LoadedParams["A19"], AircraftType.A19);
            api.SetFCS_Global(LoadedParams["FS12"], AircraftType.FS12);
            api.SetFCS_Global(LoadedParams["FS20"], AircraftType.FS20);
            api.SetFCS_Global(LoadedParams["KR67"], AircraftType.KR67);
            api.SetFCS_Global(LoadedParams["EW25"], AircraftType.EW25);
            api.SetFCS_Global(LoadedParams["SFB81"], AircraftType.SFB81);
            //VEUapi.SetVectoringMaxAirSpeed_Global(AircraftType.FS12, api.);
            //VEUapi.SetVectoringMaxAirSpeed_Global(AircraftType.KR67, 9999f);
            done = true;
        }
        else
        {
            Logger.LogInfo($"Waiting for FCPatch API");
        }

    }
    public class ModifyHelper : MonoBehaviour
    {
        private Aircraft playerAircraft;
        public void Init(Aircraft ac)
        {
            playerAircraft = ac;
        }
        void Update()
        {
            if (!(Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.L))) return;
            var api = FCSAPI.FCSPatch_API.Instance;
            LoadConfig();
            if (playerAircraft.definition.code == "CI-22")
            {
                api.SetFCS(LoadedParams["CI22"], playerAircraft.cockpit.gameObject);
            }
            else if (playerAircraft.definition.code == "FS-12")
            {
                api.SetFCS(LoadedParams["FS12"], playerAircraft.cockpit.gameObject);
            }
            else if (playerAircraft.definition.code == "KR-67")
            {
                api.SetFCS(LoadedParams["KR67"], playerAircraft.cockpit.gameObject);
            }
            else if (playerAircraft.definition.code == "T/A-30")
            {
                api.SetFCS(LoadedParams["TA30"], playerAircraft.cockpit.gameObject);
            }
            else if (playerAircraft.definition.code == "SFB-81")
            {
                api.SetFCS(LoadedParams["SFB81"], playerAircraft.cockpit.gameObject);
            }
            else if (playerAircraft.definition.code == "FS-20")
            {
                api.SetFCS(LoadedParams["FS20"], playerAircraft.cockpit.gameObject);
            }
            else if (playerAircraft.definition.code == "EW-25")
            {
                api.SetFCS(LoadedParams["EW25"], playerAircraft.cockpit.gameObject);
            }
            else if (playerAircraft.definition.code == "A-19")
            {
                api.SetFCS(LoadedParams["A19"], playerAircraft.cockpit.gameObject);
            }
        }
    }

    [HarmonyPatch(typeof(PilotPlayerState))]
    [HarmonyPatch("FixedUpdateState")]
    [HarmonyPatch(new Type[] { typeof(Pilot) })]
    public static class PilotPlayerState_FixedUpdateState_Patch
    {
        public static void Prefix(Pilot pilot)
        {
            if (GameManager.gameState != GameState.SinglePlayer && GameManager.gameState != GameState.Multiplayer) { return; }
            if (pilot.aircraft != null && pilot.aircraft.cockpit.GetComponent<ModifyHelper>() == null)
            {
                var ret = pilot.aircraft.cockpit.gameObject.AddComponent<ModifyHelper>();
                ret.Init(pilot.aircraft);

            }
        }
    }


    private static void LoadConfig()
    {
        var api = FCSAPI.FCSPatch_API.Instance;
        try
        {
            if (!File.Exists(ConfigPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
                var defaultConfig = new Dictionary<string, FlightControlParam>
                {
                    ["CI22"] = api.GetDefaultFCS(AircraftType.CI22),
                    ["TA30"] = api.GetDefaultFCS(AircraftType.TA30),
                    ["A19"] = api.GetDefaultFCS(AircraftType.A19),
                    ["FS12"] = api.GetDefaultFCS(AircraftType.FS12),
                    ["FS20"] = api.GetDefaultFCS(AircraftType.FS20),
                    ["KR67"] = api.GetDefaultFCS(AircraftType.KR67),
                    ["EW25"] = api.GetDefaultFCS(AircraftType.EW25),
                    ["SFB81"] = api.GetDefaultFCS(AircraftType.SFB81),
                };

                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(defaultConfig, Newtonsoft.Json.Formatting.Indented));
                Debug.Log($"[FlightControlMod] Generated default profile: {ConfigPath}");
            }

            string json = File.ReadAllText(ConfigPath);
            LoadedParams = JsonConvert.DeserializeObject<Dictionary<string, FlightControlParam>>(json);
            Debug.Log($"[FlightControlMod] Loaded {LoadedParams.Count} set(s) of FCS Parameters。");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FlightControlMod] Failed to loaded profile: {ex}");
        }
    }

}

