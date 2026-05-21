using System;
using System.Runtime.InteropServices;
using HarmonyLib;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

namespace MoreMultiPlayer
{
    public static class SteamManagerExtended
    {
        public static MultiStartRequestPacket startParameters;

        internal static void SendToAll(byte[] data, SteamManager sm)
        {
            var players = sm.connectedPlayers;
            if (players == null || players.Count == 0) return;

            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                var ptr = handle.AddrOfPinnedObject();
                foreach (var sc in players)
                {
                    try { sc.Connection.SendMessage(ptr, data.Length); }
                    catch (Exception ex) { Main.Log.LogError($"SendToAll: error sending to {sc.steamName}: {ex.Message}"); }
                }
            }
            finally { handle.Free(); }
        }

        private static MultiStartRequestPacket BuildPacket(SteamManager sm, PlayerInit hostPlayer, byte currentLevel)
        {
            var connected = sm.connectedPlayers;
            int n = connected.Count + 1; // +1 for host

            var packet = new MultiStartRequestPacket();
            packet.seqNum = sm.nextStartGameSeq;
            sm.nextStartGameSeq++;
            packet.seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            packet.nrOfPlayers = (byte)n;
            packet.nrOfAbilites = 3;
            packet.currentLevel = currentLevel;
            packet.frameBufferSize = (byte)sm.startFrameBuffer;
            packet.isDemoMask = 0;
            packet.p_ids     = new ulong[n];
            packet.p_colors  = new byte[n];
            packet.p_teams   = new byte[n];
            packet.p_ability1s = new byte[n];
            packet.p_ability2s = new byte[n];
            packet.p_ability3s = new byte[n];

            // Host at index 0
            packet.p_ids[0]     = SteamClient.SteamId;
            packet.p_colors[0]  = (byte)hostPlayer.color;
            packet.p_teams[0]   = (byte)hostPlayer.team;
            packet.p_ability1s[0] = (byte)hostPlayer.ability0;
            packet.p_ability2s[0] = (byte)hostPlayer.ability1;
            packet.p_ability3s[0] = (byte)hostPlayer.ability2;

            // Clients at indices 1..n
            for (int i = 0; i < connected.Count; i++)
            {
                var sc = connected[i];
                packet.p_ids[i + 1]     = sc.id;
                packet.p_colors[i + 1]  = (byte)sc.lobby_color;
                packet.p_teams[i + 1]   = sc.lobby_team;
                packet.p_ability1s[i + 1] = sc.lobby_ability1;
                packet.p_ability2s[i + 1] = sc.lobby_ability2;
                packet.p_ability3s[i + 1] = sc.lobby_ability3;
                if (!sc.ownsFullGame) packet.isDemoMask |= 1UL << (i + 1);
            }

            return packet;
        }

        [HarmonyPatch(typeof(SteamManager), "HostGame")]
        public static class SteamManager_HostGame_Patch
        {
            public static bool Prefix(SteamManager __instance, PlayerInit hostPlayer)
            {
                // Only intercept for 5+ total players; ≤4 is handled by original
                if (__instance.connectedPlayers.Count < 4) return true;

                try
                {
                    var packet = BuildPacket(__instance, hostPlayer, 0);
                    startParameters = packet;
                    var encoded = NetworkToolsExtensions.EncodeMultiStartRequest(packet);
                    Main.Log.LogInfo($"HostGame: sending MultiStartRequestPacket nrOfPlayers={packet.nrOfPlayers} size={encoded.Length}");
                    SendToAll(encoded, __instance);
                    __instance.StartHostedGame();
                    return false;
                }
                catch (Exception ex)
                {
                    Main.Log.LogError($"HostGame patch error: {ex}");
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(SteamManager), "HostNextLevel")]
        public static class SteamManager_HostNextLevel_Patch
        {
            public static bool Prefix(SteamManager __instance, Player hostPlayer, NamedSpriteList abilityIcons)
            {
                // Only intercept if we have an active extended-player game
                if (startParameters.nrOfPlayers == 0) return true;

                try
                {
                    var packet = startParameters;
                    packet.seqNum = __instance.nextStartGameSeq;
                    __instance.nextStartGameSeq++;
                    packet.seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                    packet.currentLevel = (byte)(packet.currentLevel + 1);
                    startParameters = packet;

                    var encoded = NetworkToolsExtensions.EncodeMultiStartRequest(packet);
                    Main.Log.LogInfo($"HostNextLevel: sending MultiStartRequestPacket nrOfPlayers={packet.nrOfPlayers} level={packet.currentLevel}");
                    SendToAll(encoded, __instance);
                    __instance.StartHostedGame();
                    return false;
                }
                catch (Exception ex)
                {
                    Main.Log.LogError($"HostNextLevel patch error: {ex}");
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(SteamManager), "InitNetworkClient")]
        public static class SteamManager_InitNetworkClient_Patch
        {
            public static void Postfix()
            {
                startParameters = new MultiStartRequestPacket();
            }
        }
    }
}
