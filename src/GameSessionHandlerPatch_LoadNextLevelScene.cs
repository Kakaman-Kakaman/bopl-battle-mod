using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace MoreMultiPlayer
{
    [HarmonyPatch(typeof(GameSessionHandler), "LoadNextLevelScene")]
    public static class GameSessionHandlerPatch_LoadNextLevelScene
    {
        private static readonly FieldInfo fromFieldA =
            AccessTools.Field(typeof(SteamManager), nameof(SteamManager.startParameters));

        private static readonly FieldInfo fromFieldB =
            AccessTools.Field(typeof(StartRequestPacket), nameof(StartRequestPacket.currentLevel));

        private static readonly FieldInfo toFieldA =
            AccessTools.Field(typeof(SteamManagerExtended), nameof(SteamManagerExtended.startParameters));

        private static readonly FieldInfo toFieldB =
            AccessTools.Field(typeof(MultiStartRequestPacket), nameof(MultiStartRequestPacket.currentLevel));

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return Main.PatchFieldLoad(fromFieldA, fromFieldB, toFieldA, toFieldB, instructions);
        }
    }
}
