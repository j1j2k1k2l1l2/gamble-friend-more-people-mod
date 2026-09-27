using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Mirror;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnlimitedPlayers
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class UnlimitedPlayersPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "local.gwyf.unlimitedplayers";
        public const string PluginName = "Unlimited Players";
        public const string PluginVersion = "1.0.0";

        internal static ManualLogSource Log;
        internal static ConfigEntry<int> MaxPlayers;

        private void Awake()
        {
            Log = Logger;
            MaxPlayers = Config.Bind(
                "General",
                "MaxPlayers",
                32,
                new ConfigDescription(
                    "Maximum number of players allowed in the online lobby and server.",
                    new AcceptableValueRange<int>(2, 250)));

            Harmony.CreateAndPatchAll(typeof(Patches), PluginGuid);
            SceneManager.sceneLoaded += OnSceneLoaded;

            Log.LogInfo($"{PluginName} {PluginVersion} loaded. MaxPlayers = {MaxPlayers.Value}");
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SyncLobbySettingsAsset();
            SyncNetworkManager();
        }

        // LobbySettings.maxPlayers is a ScriptableObject asset (baked to 6).
        // Steam lobby creation and rich presence read it; keep it in sync with
        // the config so every consumer sees the raised limit.
        private static void SyncLobbySettingsAsset()
        {
            LobbySettings settings = Resources.Load<LobbySettings>("LobbySettings");
            if (settings != null && settings.maxPlayers != MaxPlayers.Value)
            {
                Log.LogInfo($"LobbySettings.maxPlayers: {settings.maxPlayers} -> {MaxPlayers.Value}");
                settings.maxPlayers = MaxPlayers.Value;
            }
        }

        // FizzySteamworks reads NetworkManager.singleton.maxConnections inside
        // ServerStart, which can run before our SetupServer patch sets it.
        private static void SyncNetworkManager()
        {
            NetworkManager manager = NetworkManager.singleton;
            if (manager != null && manager.maxConnections != MaxPlayers.Value)
            {
                Log.LogInfo($"NetworkManager.maxConnections: {manager.maxConnections} -> {MaxPlayers.Value}");
                manager.maxConnections = MaxPlayers.Value;
            }
        }
    }

    [HarmonyPatch]
    internal static class Patches
    {
        // Single choke point used by both StartServer and StartHost.
        // SetupServer passes NetworkManager.maxConnections into NetworkServer.Listen,
        // which stores the static cap Mirror enforces in ServerAccept.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(NetworkManager), "SetupServer")]
        private static void SetupServerPrefix(NetworkManager __instance)
        {
            __instance.maxConnections = UnlimitedPlayersPlugin.MaxPlayers.Value;
        }

        // Mirror's static accept cap.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(NetworkServer), nameof(NetworkServer.Listen))]
        private static void ListenPrefix(ref int maxConns)
        {
            if (maxConns < UnlimitedPlayersPlugin.MaxPlayers.Value)
            {
                UnlimitedPlayersPlugin.Log.LogInfo($"NetworkServer.Listen: {maxConns} -> {UnlimitedPlayersPlugin.MaxPlayers.Value}");
                maxConns = UnlimitedPlayersPlugin.MaxPlayers.Value;
            }
        }

        // The Steam lobby itself rejects joiners once it is full.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SteamMatchmaking), nameof(SteamMatchmaking.CreateLobby))]
        private static void CreateLobbyPrefix(ref int cMaxMembers)
        {
            if (cMaxMembers < UnlimitedPlayersPlugin.MaxPlayers.Value)
            {
                UnlimitedPlayersPlugin.Log.LogInfo($"Steam CreateLobby: {cMaxMembers} -> {UnlimitedPlayersPlugin.MaxPlayers.Value}");
                cMaxMembers = UnlimitedPlayersPlugin.MaxPlayers.Value;
            }
        }
    }
}
