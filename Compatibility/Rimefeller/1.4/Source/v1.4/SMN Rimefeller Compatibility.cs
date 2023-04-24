using HarmonyLib;
using System.Reflection;
using Verse;

namespace SkyMind
{
    public class SMN_Rimefeller_Compatibility : Mod
    {
        public SMN_Rimefeller_Compatibility(ModContentPack content) : base(content)
        {
            new Harmony("SMN Rimefeller Compatibility").PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}