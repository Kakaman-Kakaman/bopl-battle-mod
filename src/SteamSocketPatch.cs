using System;
using System.IO;
using HarmonyLib;
using Steamworks;
using Steamworks.Data;

namespace MoreMultiPlayer
{
    [HarmonyPatch(typeof(SteamSocket), "OnMessage")]
    public static class SteamSocketPatch
    {
        public static bool Prefix(Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum,
            long recvTime, int channel)
        {
            try
            {
                // Let the original handle all non-multi-start packets
                if (size < 24 || size == 67 || size == 83)
                    return true;

                bool couldBeMultiStart = false;
                for (int n = 2; n <= Constants.MAX_PLAYERS; n++)
                {
                    if (size == NetworkToolsExtensions.GetMultiStartRequestSize(n))
                    {
                        couldBeMultiStart = true;
                        break;
                    }
                }

                if (!couldBeMultiStart)
                    return true;

                byte[] buffer = new byte[size];
                System.Runtime.InteropServices.Marshal.Copy(data, buffer, 0, size);

                MultiStartRequestPacket packet;
                using (var ms = new MemoryStream(buffer))
                using (var reader = new BinaryReader(ms))
                {
                    packet = NetworkToolsExtensions.ReadMultiStartRequest(reader);
                }

                if (packet.nrOfPlayers < 2 || packet.nrOfPlayers > Constants.MAX_PLAYERS)
                    return true;

                Main.Log.LogInfo($"SteamSocketPatch: received MultiStartRequestPacket from {identity.SteamId}, nrOfPlayers={packet.nrOfPlayers}");
                SteamManagerExtended.startParameters = packet;

                // Original OnMessage would have called ForceStartGame after parsing.
                // Since we return false (blocking original), call it ourselves.
                // ForceStartGame and selfRef are static members on CharacterSelectHandler_online.
                var csh = CharacterSelectHandler_online.selfRef;
                if (csh != null)
                {
                    Main.Log.LogInfo("SteamSocketPatch: triggering ForceStartGame");
                    CharacterSelectHandler_online.ForceStartGame(csh.playerColors);
                }
                else
                {
                    Main.Log.LogError("SteamSocketPatch: CharacterSelectHandler_online.selfRef is null, cannot start game");
                }

                return false;
            }
            catch (Exception ex)
            {
                Main.Log.LogError($"SteamSocketPatch.OnMessage error: {ex}");
                return true;
            }
        }
    }
}
