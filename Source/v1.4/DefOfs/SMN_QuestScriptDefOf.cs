using RimWorld;

namespace SkyMind
{
    [DefOf]
    public static class SMN_QuestScriptDefOf
    {
        static SMN_QuestScriptDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(SMN_QuestScriptDefOf));
        }

        [MayRequireRoyalty]
        public static QuestScriptDef ProblemCauser;
    }
}
