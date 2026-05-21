using HarmonyLib;

namespace MoreMultiPlayer
{
    // Sync our extended startParameters back into the game's StartRequestPacket before
    // LoadNextLevelScene reads from it, so the game sees correct seed/level values.
    [HarmonyPatch(typeof(GameSessionHandler), "LoadNextLevelScene")]
    public static class GameSessionHandlerPatch_LoadNextLevelScene
    {
        public static void Prefix()
        {
            if (SteamManagerExtended.startParameters.nrOfPlayers == 0) return;

            // startParameters is a static field on SteamManager
            var sp = SteamManager.startParameters;
            sp.seed = (uint)SteamManagerExtended.startParameters.seed;
            sp.currentLevel = SteamManagerExtended.startParameters.currentLevel;
            sp.seqNum = (ushort)SteamManagerExtended.startParameters.seqNum;
            sp.nrOfPlayers = SteamManagerExtended.startParameters.nrOfPlayers;
            sp.frameBufferSize = SteamManagerExtended.startParameters.frameBufferSize;
            SteamManager.startParameters = sp;
        }
    }
}
