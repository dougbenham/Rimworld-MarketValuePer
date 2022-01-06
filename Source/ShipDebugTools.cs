using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MarketValuePer
{
    public static class ShipDebugTools
    {
        private static readonly List<Message> _liveMessages = AccessTools.StaticFieldRefAccess<List<Message>>(typeof(Messages), "liveMessages");

        private static void ResetMessages(Message[] messagesFromBefore)
        {
            _liveMessages.Clear();
            _liveMessages.AddRange(messagesFromBefore);
        }

        private static void ResetLetters(HashSet<Letter> lettersToKeep)
        {
            foreach (var letter in Find.LetterStack.LettersListForReading.ToArray())
            {
                if (!lettersToKeep.Contains(letter))
                    Find.LetterStack.RemoveLetter(letter);
            }
        }

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
                            var messagesFromBefore = _liveMessages.ToArray();
                            var lettersFromBefore = Find.LetterStack.LettersListForReading.ToHashSet();

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
                                        ResetMessages(messagesFromBefore);
                                        ResetLetters(lettersFromBefore);
                                        Messages.Message($"Found good ship after {i + 1} spawns.", MessageTypeDefOf.SituationResolved);
                                        return;
                                    }
                                }
                            }
                            
                            ResetMessages(messagesFromBefore);
                            ResetLetters(lettersFromBefore);
                            Messages.Message($"Couldn't find ship after {i + 1} spawns.", MessageTypeDefOf.NegativeEvent);
                        }));
                    }

                    Find.WindowStack.Add(new Dialog_DebugOptionListLister(thingOptions));
                }));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(orbitalOptions));
        }
    }
}