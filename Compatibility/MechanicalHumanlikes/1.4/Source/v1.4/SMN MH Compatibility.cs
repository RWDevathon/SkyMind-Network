using HarmonyLib;
using System.Reflection;
using Verse;

namespace SkyMind
{
    public class SMN_MH_Compatibility : Mod
    {
        public SMN_MH_Compatibility(ModContentPack content) : base(content)
        {
        }
    }

    [StaticConstructorOnStartup]
    public static class SMN_MH_Compatibility_PostInit
    {
        static SMN_MH_Compatibility_PostInit()
        {
            new Harmony("SMN MH Compatibility").PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}