using Verse;
using Rimefeller;

namespace SkyMind
{
    // This ThingComp attaches to the Rimefeller Resource Console and will continually call its Used() method to signify it is actively manned when SkyMind connected to a Core.
    public class SkyMindRimefellerConsole : ThingComp
    {
        private Building_ResourceConsole resourceConsole;

        private CompSkyMind compSkyMind;

        private Building_ResourceConsole ResourceConsole
        {
            get
            {
                if (resourceConsole == null)
                {
                    resourceConsole = (Building_ResourceConsole)parent;
                }
                return resourceConsole;
            }
        }

        private CompSkyMind SkyMindConnection
        {
            get
            {
                if (compSkyMind == null)
                {
                    compSkyMind = parent.GetComp<CompSkyMind>();
                }
                return compSkyMind;
            }
        }

        // The parent should have a ticker type of normal, meaning it will call this Comp each tick.
        public override void CompTick()
        {
            // If the console is functional, connected to the network, and there exists a SkyMind Core of some kind, call Used.
            if (ResourceConsole.WorkingNow && SkyMindConnection.connected && SMN_Utils.gameComp.GetSkyMindCloudCapacity() > 0)
            {
                ResourceConsole.Used();
            }
        }
    }
}