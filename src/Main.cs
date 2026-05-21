using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace MoreMultiPlayer
{
    [BepInPlugin("com.MorePlayersTeam.MorePlayers", "MorePlayers", "1.0.0")]
    public class Main : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        private Harmony harmony;
        private bool isVisible = true;
        private ConfigEntry<int> maxPlayers;

        private static IEnumerable<CodeInstruction> SteamManagerCreateFriendLobbyPatch(
            IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.LoadsConstant(4))
                {
                    Log.LogMessage($"Found create lobby instruction to patch from 4 to {Constants.MAX_PLAYERS}");
                    yield return new CodeInstruction(OpCodes.Ldc_I4, Constants.MAX_PLAYERS);
                    continue;
                }

                yield return instruction;
            }
        }

        public static IEnumerable<CodeInstruction> PatchFieldLoad(FieldInfo fromA, FieldInfo fromB, FieldInfo toA,
            FieldInfo toB, IEnumerable<CodeInstruction> instructions)
        {
            bool patched = false;

            var enumerator = instructions.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var instruction = enumerator.Current;

                if (instruction.LoadsField(fromA, true))
                {
                    if (!enumerator.MoveNext())
                    {
                        Log.LogError($"Expected to find next instruction after {fromA} load instruction");
                        yield return instruction;
                    }

                    var nextInstruction = enumerator.Current;

                    if (!nextInstruction.LoadsField(fromB))
                    {
                        Log.LogInfo($"Candidate patch instruction is not loading {fromB} field: {nextInstruction.opcode}:{nextInstruction.operand}");
                        yield return instruction;
                        yield return nextInstruction;
                        continue;
                    }

                    Log.LogInfo($"Found {fromA} and {fromB} field load instructions to patch");

                    yield return new CodeInstruction(instruction.opcode, toA);
                    yield return new CodeInstruction(nextInstruction.opcode, toB);

                    patched = true;
                }
                else
                {
                    yield return instruction;
                }
            }

            if (!patched)
            {
                Log.LogError("Failed to patch GameSessionHandler.LoadNextLevelScene");
            }
        }

        void OnGUI()
        {
            var players = PlayerHandler.Get().NumberOfPlayers();
            var playerInfoList = PlayerHandler.Get().PlayerList();

            GUI.color = new Color(0, 0, 0, 0.7f);

            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleLeft;
            style.padding = new RectOffset(10, 10, 5, 5);

            if (GUI.Button(new Rect(50, 135 + playerInfoList.Count * 30, 150, 40), "Toggle Visibility"))
            {
                isVisible = !isVisible;
            }

            GUI.color = Color.white;

            foreach (var player in SteamManager.instance.connectedPlayers)
            {
                if (player.hasAvatar)
                {
                    float yPosition = 10;
                    float xPosition = 90;
                    float width = 82;
                    float height = 82;
                    float spacing = 50;

                    GUI.DrawTexture(new Rect(xPosition, yPosition, width, height), player.avatar);

                    xPosition += spacing;
                }
                else
                {
                    Main.Log.LogWarning($"{player.steamName}, does not have an avatar.");
                }
            }

            if (isVisible)
            {
                GUI.Box(new Rect(20, 90, 640, 40 + playerInfoList.Count * 30), GUIContent.none);

                GUIStyle headerStyle = new GUIStyle(style);
                headerStyle.fontStyle = FontStyle.Bold;

                GUI.Label(new Rect(25, 95, 300, 30), $"MoreBopl Leaderboard \n{players} Player(s)", headerStyle);

                for (int i = 0; i < playerInfoList.Count; i++)
                {
                    string userColor = playerInfoList[i].Color.ToString().Replace("Slime (UnityEngine.Material)", "");
                    string fixedUserColor = char.ToUpper(userColor[0]) + userColor.Substring(1);
                    string causeOfDeath = playerInfoList[i].CauseOfDeath.ToString();

                    if (causeOfDeath == "NotDeadYet")
                    {
                        causeOfDeath = "Alive";
                    }

                    float yPosition = 130 + i * 30;

                    GUI.Label(new Rect(70, yPosition, 600, 30), $"{fixedUserColor}: Kills: {playerInfoList[i].Kills}, Deaths: {playerInfoList[i].Deaths}, Cause of Death: {causeOfDeath}", style);
                }
            }
        }

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo("Logger Loaded");

            maxPlayers = Config.Bind("General", "MaxPlayers", 8, "The maximum number of players allowed in a lobby.");
            Constants.MAX_PLAYERS = maxPlayers.Value;

            Host.recordReplay = false;
            Logger.LogInfo("Disabled replay recording");

            harmony = new Harmony("com.MorePlayersTeam.MorePlayers");

            harmony.PatchAll();

            var targetMethod =
                typeof(SteamManager).GetMethod("CreateFriendLobby", BindingFlags.Instance | BindingFlags.Public);
            if (targetMethod == null)
            {
                Logger.LogError("Failed to find SteamManager::CreateFriendLobby!");
                return;
            }

            var stateMachineAttr = targetMethod.GetCustomAttribute<System.Runtime.CompilerServices.AsyncStateMachineAttribute>();
            var moveNextMethod =
                stateMachineAttr.StateMachineType.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance);
            var startTranspiler = typeof(Main).GetMethod(nameof(SteamManagerCreateFriendLobbyPatch),
                BindingFlags.Static | BindingFlags.NonPublic);

            var patcher = harmony.CreateProcessor(moveNextMethod);
            patcher.AddTranspiler(startTranspiler);
            patcher.Patch();

            Logger.LogInfo($"More players acquired! Max players: {Constants.MAX_PLAYERS}");
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }

        private void Update()
        {
            if (SteamClient.Name == "Noob" || !SteamClient.IsValid)
            {
                Main.Log.LogFatal("PIRATED GAME DETECTED, PLEASE INSTALL THE FULL GAME!");
                Application.OpenURL("https://store.steampowered.com/app/1686940/Bopl_Battle/");
                Application.Quit();
            }
        }
    }

    [HarmonyPatch(typeof(printText))]
    [HarmonyPatch("Awake")]
    public static class PatchVersion
    {
        public static void Prefix()
        {
            Main.Log.LogInfo($"Found version {Constants.version}");
            Constants.version = $"{Constants.version} -More Players Modded";
            Main.Log.LogInfo($"Patched to version {Constants.version}");
        }
    }
}
