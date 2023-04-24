using HarmonyLib;
using System.Reflection;
using Verse;

namespace SkyMind
{
    public class SMN_Rimatomics_Compatibility : Mod
    {
        public SMN_Rimatomics_Compatibility(ModContentPack content) : base(content)
        {
        }
    }

    [StaticConstructorOnStartup]
    public static class SMN_Rimatomics_Compatibility_PostInit
    {
        static SMN_Rimatomics_Compatibility_PostInit()
        {
            new Harmony("SMN Rimatomics Compatibility").PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}