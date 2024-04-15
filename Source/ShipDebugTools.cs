using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;

namespace SellValuePer
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
                    var thingOptions = new List<DebugMenuOption>();
                    foreach (var desiredDef in DefDatabase<ThingDef>.AllDefs.Where(def => traderKind.WillTrade(def)).OrderBy(def => def.defName))
                    {
                        thingOptions.Add(new DebugMenuOption(desiredDef.defName, DebugMenuOptionMode.Action, delegate()
                        {
                            if (desiredDef.HasComp(typeof(CompQuality)))
                            {
                                var qualityOptions = new List<DebugMenuOption>();
                                qualityOptions.Add(new DebugMenuOption("Any", DebugMenuOptionMode.Action, delegate()
                                {
                                    SpawnShipsUntil(traderKind, desiredDef, null);
                                }));
                                foreach (var desiredQuality in Enum.GetValues(typeof(QualityCategory)).Cast<QualityCategory>())
                                {
                                    if (desiredQuality == QualityCategory.Legendary) // traders can't generate legendaries
                                        continue;

                                    qualityOptions.Add(new DebugMenuOption(desiredQuality.ToString(), DebugMenuOptionMode.Action, delegate()
                                    {
                                        SpawnShipsUntil(traderKind, desiredDef, desiredQuality);
                                    }));
                                }

                                Find.WindowStack.Add(new Dialog_DebugOptionListLister(qualityOptions));
                            }
                            else
                                SpawnShipsUntil(traderKind, desiredDef, null);
                        }));
                    }

                    Find.WindowStack.Add(new Dialog_DebugOptionListLister(thingOptions));
                }));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(orbitalOptions));
        }

        private static void SpawnShipsUntil(TraderKindDef traderKind, ThingDef desiredDef, QualityCategory? qualityCategory)
        {
	        foreach (var s in Find.CurrentMap.passingShipManager.passingShips.ToArray())
		        s.Depart();
            
	        IncidentParms incidentParms = new IncidentParms();
	        incidentParms.target = Find.CurrentMap;
	        incidentParms.traderKind = traderKind;
	        IncidentDefOf.OrbitalTraderArrival.Worker.TryExecute(incidentParms);

	        var ship = Find.CurrentMap.passingShipManager.passingShips.OfType<TradeShip>().First();
	        
	        var thing = ThingMaker.MakeThing(desiredDef, GenStuff.RandomStuffFor(desiredDef));
	        var compQuality = thing.TryGetComp<CompQuality>();
            if (compQuality != null && qualityCategory != null)
            {
	            compQuality.SetQuality(qualityCategory.Value, ArtGenerationContext.Colony);
            }
            if (thing.def.Minifiable)
            {
	            thing = thing.MakeMinified();
            }
            if (thing.def.CanHaveFaction)
            {
	            if (thing.def.building != null && thing.def.building.isInsectCocoon)
	            {
		            thing.SetFaction(Faction.OfInsects);
	            }
	            else
	            {
		            thing.SetFaction(Faction.OfPlayerSilentFail);
	            }
            }
            thing.stackCount = desiredDef.stackLimit;
            ship.GetDirectlyHeldThings().TryAdd(thing);

            /*var i = 0;
            while (true)
            {
	            if (ship.Goods.Any(t => t.GetInnerIfMinified().def == desiredDef && (qualityCategory == null || t.TryGetComp<CompQuality>().Quality == qualityCategory)))
	            {
		            Messages.Message($"Found good ship after {i + 1} spawns.", MessageTypeDefOf.SituationResolved);
		            return;
	            }

	            if (i++ >= 1000)
		            break;

	            ship.GetDirectlyHeldThings().ClearAndDestroyContentsOrPassToWorld();
	            ship.GenerateThings();
            }

            Messages.Message($"Couldn't find ship after {i + 1} spawns.", MessageTypeDefOf.NegativeEvent);*/
        }
    }
}