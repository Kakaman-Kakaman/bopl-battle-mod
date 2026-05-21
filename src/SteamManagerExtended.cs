using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using Steamworks;
using Steamworks.Data;

namespace MoreMultiPlayer
{
    public static class SteamManagerExtended
    {
        public static MultiStartRequestPacket startParameters;

        private static SocketManager FindHostSocket()
        {
            if (SteamManager.instance == null) return null;
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
            foreach (var field in typeof(SteamManager).GetFields(flags))
            {
                try
                {
                    var val = field.IsStatic ? field.GetValue(null) : field.GetValue(SteamManager.instance);
                    if (val is SocketManager mgr) return mgr;
                }
                catch { }
            }
            return null;
        }

        private static void SendToAll(byte[] data)
        {
            var socket = FindHostSocket();
            if (socket == null)
            {
                Main.Log.LogError("SendToAll: could not find host SocketManager");
                return;
            }
            // socket.Connected only contains CLIENT connections; host is not in the list
            foreach (var conn in socket.Connected)
            {
                var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                try
                {
                    conn.SendMessage(handle.AddrOfPinnedObject(), data.Length);
                }
                catch (Exception ex)
                {
                    Main.Log.LogError($"SendToAll: error sending: {ex.Message}");
                }
                finally
                {
                    handle.Free();
                }
            }
        }

        [HarmonyPatch(typeof(SteamManager), "HostGame")]
        public static class SteamManager_HostGame_Patch
        {
            public static bool Prefix(SteamManager __instance, int seed, byte[] p_colors, byte[] p_teams,
                byte[] p_ability1s, byte[] p_ability2s, byte[] p_ability3s, byte nrOfAbilities,
                List<SteamId> connectedPlayers, byte frameBufferSize, bool[] isDemoArr, int seqNum, byte currentLevel)
            {
                try
                {
                    byte nrOfPlayers = (byte)connectedPlayers.Count;

                    MultiStartRequestPacket packet = new MultiStartRequestPacket();
                    packet.seqNum = seqNum;
                    packet.seed = seed;
                    packet.nrOfPlayers = nrOfPlayers;
                    packet.nrOfAbilites = nrOfAbilities;
                    packet.currentLevel = currentLevel;
                    packet.frameBufferSize = frameBufferSize;

                    ulong isDemoMask = 0;
                    for (int i = 0; i < isDemoArr.Length && i < 64; i++)
                        if (isDemoArr[i]) isDemoMask |= 1UL << i;
                    packet.isDemoMask = isDemoMask;

                    packet.p_ids = new ulong[nrOfPlayers];
                    packet.p_colors = new byte[nrOfPlayers];
                    packet.p_teams = new byte[nrOfPlayers];
                    packet.p_ability1s = new byte[nrOfPlayers];
                    packet.p_ability2s = new byte[nrOfPlayers];
                    packet.p_ability3s = new byte[nrOfPlayers];

                    for (int i = 0; i < nrOfPlayers; i++)
                    {
                        packet.p_ids[i] = connectedPlayers[i];
                        packet.p_colors[i] = p_colors[i];
                        packet.p_teams[i] = p_teams[i];
                        packet.p_ability1s[i] = p_ability1s[i];
                        packet.p_ability2s[i] = p_ability2s[i];
                        packet.p_ability3s[i] = p_ability3s[i];
                    }

                    startParameters = packet;

                    var encoded = NetworkToolsExtensions.EncodeMultiStartRequest(packet);
                    Main.Log.LogInfo($"HostGame: sending MultiStartRequestPacket, nrOfPlayers={nrOfPlayers}, size={encoded.Length}");

                    SendToAll(encoded);
                    return false;
                }
                catch (Exception ex)
                {
                    Main.Log.LogError($"SteamManager_HostGame_Patch error: {ex}");
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(SteamManager), "HostNextLevel")]
        public static class SteamManager_HostNextLevel_Patch
        {
            public static bool Prefix(SteamManager __instance, int seed, byte[] p_colors, byte[] p_teams,
                byte[] p_ability1s, byte[] p_ability2s, byte[] p_ability3s, byte nrOfAbilities,
                List<SteamId> connectedPlayers, byte frameBufferSize, bool[] isDemoArr, int seqNum, byte currentLevel)
            {
                return SteamManager_HostGame_Patch.Prefix(__instance, seed, p_colors, p_teams, p_ability1s,
                    p_ability2s, p_ability3s, nrOfAbilities, connectedPlayers, frameBufferSize, isDemoArr, seqNum,
                    currentLevel);
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
