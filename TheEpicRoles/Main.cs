﻿using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;
using System.Net;
using System.IO;
using System;
using System.Reflection;
using UnhollowerBaseLib;
using UnityEngine;
using TheEpicRoles.Modules;

namespace TheEpicRoles {
    [BepInPlugin(Id, "The Epic Roles", VersionString)]
    [BepInProcess("Among Us.exe")]
    public class TheEpicRolesPlugin : BasePlugin
    {
        public const string Id = "me.laicosvk.theepicroles";
        public const string VersionString = "1.2.0";
        public static uint firstKill = 0; //i think this is old and can be removed. i wont do it now since 1.1.1 is just a fix.
        public const string hashPassword = "-1526003550";

        public static System.Version Version = System.Version.Parse(VersionString);

        internal static BepInEx.Logging.ManualLogSource Logger;

        public Harmony Harmony { get; } = new Harmony(Id);
        public static TheEpicRolesPlugin Instance;

        public static int optionsPage = 2;

        public static ConfigEntry<string> DeveloperMode { get; private set; }
        public static ConfigEntry<bool> StreamerMode { get; set; }
        public static ConfigEntry<bool> GhostsSeeTasks { get; set; }
        public static ConfigEntry<bool> GhostsSeeRoles { get; set; }
        public static ConfigEntry<bool> GhostsSeeVotes{ get; set; }
        public static ConfigEntry<bool> ShowRoleSummary { get; set; }
        public static ConfigEntry<bool> ShowLighterDarker { get; set; }
        public static ConfigEntry<bool> ToggleCursor { get; set; }
        public static ConfigEntry<bool> ToggleScreenShake { get; set; }
        public static ConfigEntry<string> StreamerModeReplacementText { get; set; }
        public static ConfigEntry<string> StreamerModeReplacementColor { get; set; }
        public static ConfigEntry<string> Ip { get; set; }
        public static ConfigEntry<ushort> Port { get; set; }
        public static ConfigEntry<string> ShowPopUpVersion { get; set; }

        public static Sprite ModStamp;

        public static IRegionInfo[] defaultRegions;
        public static void UpdateRegions() {
            ServerManager serverManager = DestroyableSingleton<ServerManager>.Instance;
            IRegionInfo[] regions = defaultRegions;

            var CustomRegion = new DnsRegionInfo(Ip.Value, "Custom", StringNames.NoTranslation, Ip.Value, Port.Value, false);
            regions = regions.Concat(new IRegionInfo[] { CustomRegion.Cast<IRegionInfo>() }).ToArray();
            ServerManager.DefaultRegions = regions;
            serverManager.AvailableRegions = regions;
        }

        public override void Load() {
            Logger = Log;
            DeveloperMode = Config.Bind("Custom", "Enable Developer Mode", "false");
            StreamerMode = Config.Bind("Custom", "Enable Streamer Mode", false);
            GhostsSeeTasks = Config.Bind("Custom", "Ghosts See Remaining Tasks", true);
            GhostsSeeRoles = Config.Bind("Custom", "Ghosts See Roles", true);
            GhostsSeeVotes = Config.Bind("Custom", "Ghosts See Votes", true);
            ShowRoleSummary = Config.Bind("Custom", "Show Role Summary", true);
            ShowLighterDarker = Config.Bind("Custom", "Show Lighter / Darker", true);
            ToggleCursor = Config.Bind("Custom", "Better Cursor", true);
            ToggleScreenShake = Config.Bind("Custom", "Screen Shake", false);
            ShowPopUpVersion = Config.Bind("Custom", "Show PopUp", "0");
            StreamerModeReplacementText = Config.Bind("Custom", "Streamer Mode Replacement Text", "\n\nThe Epic Roles");
            StreamerModeReplacementColor = Config.Bind("Custom", "Streamer Mode Replacement Text Hex Color", "#00FFDDFF");
            

            Ip = Config.Bind("Custom", "Custom Server IP", "127.0.0.1");
            Port = Config.Bind("Custom", "Custom Server Port", (ushort)22023);
            defaultRegions = ServerManager.DefaultRegions;

            UpdateRegions();

            GameOptionsData.RecommendedImpostors = GameOptionsData.MaxImpostors = Enumerable.Repeat(3, 16).ToArray(); // Max Imp = Recommended Imp = 3
            GameOptionsData.MinPlayers = Enumerable.Repeat(4, 15).ToArray(); // Min Players = 4

            DeveloperMode = Config.Bind("Custom", "Enable Developer Mode", "0");
            Instance = this;
            CustomOptionHolder.Load();
            CustomColors.Load();

            Harmony.PatchAll();

            if (ToggleCursor.Value) {
                Helpers.enableCursor("init");
            }
        }
        public static Sprite GetModStamp() {
            if (ModStamp) return ModStamp;
            if (TheEpicRolesPlugin.DeveloperMode.Value.GetHashCode().ToString() != TheEpicRolesPlugin.hashPassword)
                return ModStamp = Helpers.loadSpriteFromResources("TheEpicRoles.Resources.ModStamp.png", 150f);
            else return ModStamp = Helpers.loadSpriteFromResources("TheEpicRoles.Resources.DevStamp.png", 150f);
        }
    }

    // Deactivate bans, since I always leave my local testing game and ban myself
    [HarmonyPatch(typeof(StatsManager), nameof(StatsManager.AmBanned), MethodType.Getter)]
    public static class AmBannedPatch
    {
        public static void Postfix(out bool __result)
        {
            __result = false;
        }
    }
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Awake))]
    public static class ChatControllerAwakePatch {
        private static void Prefix() {
            if (!EOSManager.Instance.IsMinor()) {
                SaveManager.chatModeType = 1;
                SaveManager.isGuest = false;
            }
        }
    }
    
    // Debugging tools
    [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
    public static class DebugManager
    {
        private static readonly System.Random random = new System.Random((int)DateTime.Now.Ticks);
        private static List<PlayerControl> bots = new List<PlayerControl>();

        public static void Postfix(KeyboardJoystick __instance)
        {
            if (TheEpicRolesPlugin.DeveloperMode.Value.GetHashCode().ToString() != TheEpicRolesPlugin.hashPassword)
                return;

            // Spawn dummys
            if (Input.GetKeyDown(KeyCode.F)) {
                var playerControl = UnityEngine.Object.Instantiate(AmongUsClient.Instance.PlayerPrefab);
                var i = playerControl.PlayerId = (byte) GameData.Instance.GetAvailableId();

                bots.Add(playerControl);
                GameData.Instance.AddPlayer(playerControl);
                AmongUsClient.Instance.Spawn(playerControl, -2, InnerNet.SpawnFlags.None);
                
                playerControl.transform.position = PlayerControl.LocalPlayer.transform.position;
                playerControl.GetComponent<DummyBehaviour>().enabled = true;
                playerControl.NetTransform.enabled = false;
                playerControl.SetName(RandomString(10));
                playerControl.SetColor((byte) random.Next(Palette.PlayerColors.Length));
                GameData.Instance.RpcSetTasks(playerControl.PlayerId, new byte[0]);
            }

            // Terminate round
            if (Input.GetKeyDown(KeyCode.L))
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ForceEnd, Hazel.SendOption.Reliable, -1);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.forceEnd();
            }

            // get/remove ventbutton
            if (Input.GetKeyDown(KeyCode.V))
            {
                if (PlayerControl.LocalPlayer.Data.Role.CanVent)
                    PlayerControl.LocalPlayer.Data.Role.CanVent = false;
                else PlayerControl.LocalPlayer.Data.Role.CanVent = true;
            }

            // increase report distance
            if (Input.GetKeyDown(KeyCode.U))
            {
                PlayerControl.LocalPlayer.MaxReportDistance += 1;
            }

            // decrease report distance
            if (Input.GetKeyDown(KeyCode.J))
            {
                PlayerControl.LocalPlayer.MaxReportDistance -= 1;
            }
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
