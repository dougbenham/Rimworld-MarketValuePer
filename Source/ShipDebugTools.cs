using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using Verse;

namespace SellValuePer
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
            var messagesFromBefore = _liveMessages.ToArray();
            var lettersFromBefore = Find.LetterStack.LettersListForReading.ToHashSet();

            var i = 0;
            for (; i < 1000; i++)
            {
                foreach (var ship in Find.CurrentMap.passingShipManager.passingShips.ToArray())
                    ship.Depart();

                IncidentParms incidentParms = new IncidentParms();
                incidentParms.target = Find.CurrentMap;
                incidentParms.traderKind = traderKind;
                IncidentDefOf.OrbitalTraderArrival.Worker.TryExecute(incidentParms);

                foreach (var ship in Find.CurrentMap.passingShipManager.passingShips.OfType<TradeShip>())
                {
                    if (ship.Goods.Any(t => t.GetInnerIfMinified().def == desiredDef && (qualityCategory == null || t.TryGetComp<CompQuality>().Quality == qualityCategory)))
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
        }
    }
}