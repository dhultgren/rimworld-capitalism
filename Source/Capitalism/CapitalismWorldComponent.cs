using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Capitalism
{
    // World component that keeps track of recent trades to calculate price modifiers based on world economy
    public class CapitalismWorldComponent : WorldComponent
    {
        private static readonly int UpdateFrequency = 500;
        private static readonly int MaxTradeAge = 30000;

        private List<ThingTrade> registeredTrades = new List<ThingTrade>();
        private int ticksSinceUpdate = 0;

        private Dictionary<ThingDef, float> priceModifiers;
        public Dictionary<ThingDef, float> PriceModifiers
        {
            get
            {
                if (priceModifiers == null) priceModifiers = GeneratePriceModifiers();
                return priceModifiers;
            }
        }

        public CapitalismWorldComponent(World world) : base(world) { }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            ticksSinceUpdate++;
            if (ticksSinceUpdate >= UpdateFrequency)
            {
                ticksSinceUpdate = 0;
            }
        }

        public void ForgetTradesFor(Settlement settlement)
        {
            registeredTrades = registeredTrades
                .Where(t => t.settlement != settlement)
                .ToList();
            priceModifiers = null;
        }

        public Dictionary<ThingDef, float> GeneratePriceModifiers()
        {
            var totalInCirculation = new Dictionary<ThingDef, int>();
            var totalTraded = new Dictionary<ThingDef, int>();

            UpdateRegisteredTrades();

            foreach (var settlement in Find.WorldObjects.Settlements)
            {
                var inStock = settlement.trader.StockListForReading;
                foreach (var t in inStock)
                {
                    var def = CapitalismUtils.GroupCertainThingDefs(t.def);
                    if (!totalInCirculation.ContainsKey(def)) totalInCirculation[def] = 0;
                    totalInCirculation[def] += t.stackCount;
                }
            }

            foreach (var t in registeredTrades)
            {
                if (!totalTraded.ContainsKey(t.thingDef)) totalTraded[t.thingDef] = 0;
                totalTraded[t.thingDef] += t.count;
            }

            var priceModifiers = new Dictionary<ThingDef, float>();
            foreach(var pair in totalInCirculation)
            {
                if (totalTraded.ContainsKey(pair.Key))
                {
                    var inCirculation = pair.Value;
                    var boughtByPlayer = totalTraded[pair.Key];
                    var inCirculationWithoutPlayer = inCirculation + boughtByPlayer;
                    var preClampModifier = (float)Math.Sqrt(inCirculationWithoutPlayer / (float)inCirculation);
                    var maxModifier = Capitalism.Settings.MaxSupplyDemandChangePercent / 100f;
                    priceModifiers[pair.Key] = Math.Min(Math.Max(preClampModifier, 1 / maxModifier), maxModifier);
                }
            }

            Messages.Message("New price modifiers: " + string.Join(", ", priceModifiers.Select(kv => kv.Key.label + ":" + kv.Value.ToString("F"))), MessageTypeDefOf.NeutralEvent);

            return priceModifiers;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksSinceUpdate, "ticksSinceUpdate");
            Scribe_Collections.Look(ref registeredTrades, "registeredTrades", LookMode.Deep);
            if (registeredTrades == null) registeredTrades = new List<ThingTrade>();
        }

        public void RegisterTrade(Faction faction, Settlement settlement, ThingDef thingDef, int count)
        {
            registeredTrades.Add(new ThingTrade()
            {
                expiresAtTick = Find.TickManager.TicksGame + MaxTradeAge,
                count = count,
                thingDef = CapitalismUtils.GroupCertainThingDefs(thingDef),
                settlement = settlement,
                faction = faction
            });
            priceModifiers = null;
        }

        private void UpdateRegisteredTrades()
        {
            var expiredTrades = registeredTrades
                    .Where(t => t.HasExpired)
                    .ToList();
            foreach (var t in expiredTrades)
            {
                Messages.Message("Forgetting trade " + t.count + "x " + t.thingDef.label, MessageTypeDefOf.NeutralEvent);
            }
            if (expiredTrades.Any()) priceModifiers = null;

            registeredTrades = registeredTrades
                .Where(t => !t.HasExpired)
                .ToList();
        }

        class ThingTrade : IExposable
        {
            public ThingDef thingDef;
            public int count;
            public int expiresAtTick;
            public Settlement settlement;
            public Faction faction;

            public void ExposeData()
            {
                Scribe_Defs.Look(ref thingDef, "thingDef");
                Scribe_Values.Look(ref count, "count");
                Scribe_Values.Look(ref expiresAtTick, "expiresAtTick");
                Scribe_References.Look(ref settlement, "settlement");
                Scribe_References.Look(ref faction, "faction");
            }

            public bool HasExpired => expiresAtTick >= Find.TickManager.TicksGame;
        }
    }
}
