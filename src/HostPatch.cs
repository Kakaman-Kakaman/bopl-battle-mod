using System;
using System.Reflection;
using HarmonyLib;

namespace MoreMultiPlayer
{
    [HarmonyPatch(typeof(Host), "ProcessNetworkPackets")]
    public static class HostPatch_ProcessNetworkPackets
    {
        public static bool Prefix(Host __instance)
        {
            return true;
        }
    }

    [HarmonyPatch(typeof(Host), "Update")]
    public static class HostPatch_Update
    {
        public static void Prefix(Host __instance) { }
    }

    [HarmonyPatch(typeof(Host), "Start")]
    public static class HostPatch_Start
    {
        public static void Postfix(Host __instance)
        {
            Main.Log.LogInfo("Host.Start called");
        }
    }

    [HarmonyPatch(typeof(Host), "ReInit")]
    public static class HostPatch_ReInit
    {
        public static void Postfix(Host __instance)
        {
            Main.Log.LogInfo("Host.ReInit called");
        }
    }

    [HarmonyPatch(typeof(Host), "Init")]
    public static class HostPatch_Init
    {
        public static void Postfix(Host __instance)
        {
            Main.Log.LogInfo("Host.Init called");
        }
    }

    public static class Constants
    {
        public static int MAX_PLAYERS = 8;
        public static string version = "2.5.1";
    }
}
