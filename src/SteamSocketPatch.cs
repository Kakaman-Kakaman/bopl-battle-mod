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
                if (size < 24 || size == 67 || size == 83)
                {
                    return true;
                }

                int expectedSize = NetworkToolsExtensions.GetMultiStartRequestSize(Constants.MAX_PLAYERS);

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
                {
                    return true;
                }

                byte[] buffer = new byte[size];
                System.Runtime.InteropServices.Marshal.Copy(data, buffer, 0, size);

                using (MemoryStream ms = new MemoryStream(buffer))
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    MultiStartRequestPacket packet = NetworkToolsExtensions.ReadMultiStartRequest(reader);

                    if (packet.nrOfPlayers < 2 || packet.nrOfPlayers > Constants.MAX_PLAYERS)
                    {
                        return true;
                    }

                    Main.Log.LogInfo(
                        $"SteamSocketPatch: Received MultiStartRequestPacket from {identity.SteamId}, nrOfPlayers={packet.nrOfPlayers}");

                    SteamManagerExtended.startParameters = packet;
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
