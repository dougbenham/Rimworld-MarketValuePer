using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MarketValuePer
{
    public static class ShipDebugTools
    {
        [DebugAction("Spawning", "Spawn Ships Until X", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnShipsUntil()
        {
            var orbitalOptions = new List<DebugMenuOption>();
            foreach (var traderKind in DefDatabase<TraderKindDef>.AllDefs.Where(def => def.orbital))
            {
                orbitalOptions.Add(new DebugMenuOption(traderKind.label, DebugMenuOptionMode.Action, delegate()
                {
                    List<DebugMenuOption> thingOptions = new List<DebugMenuOption>();
                    foreach (var desiredDef in DefDatabase<ThingDef>.AllDefs.Where(def => traderKind.WillTrade(def)).OrderBy(def => def.defName))
                    {
                        thingOptions.Add(new DebugMenuOption(desiredDef.defName, DebugMenuOptionMode.Action, delegate()
                        {
                            var i = 0;
                            for (; i < 500; i++)
                            {
                                foreach (var ship in Find.CurrentMap.passingShipManager.passingShips.ToArray())
                                    ship.Depart();

                                IncidentParms incidentParms = new IncidentParms();
                                incidentParms.target = Find.CurrentMap;
                                incidentParms.traderKind = traderKind;
                                IncidentDefOf.OrbitalTraderArrival.Worker.TryExecute(incidentParms);

                                foreach (var ship in Find.CurrentMap.passingShipManager.passingShips.OfType<TradeShip>())
                                {
                                    if (ship.Goods.Any(t => t.def == desiredDef))
                                    {
                                        Log.Message($"Found good ship after {i + 1} spawns.");
                                        return;
                                    }
                                }
                            }
                            Log.Error($"Couldn't find ship after {i + 1} spawns.");
                        }));
                    }

                    Find.WindowStack.Add(new Dialog_DebugOptionListLister(thingOptions));
                }));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(orbitalOptions));
        }
    }
}