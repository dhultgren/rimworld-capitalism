using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace Capitalism
{
    // World component that keeps track of recent trades to calculate price modifiers based on world economy
    public class CapitalismWorldComponent : WorldComponent
    {
        private static readonly int UpdateFrequency = 500;
        private static readonly int MaxTradeAge = 2000;

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

        public void RegisterTrade(Settlement settlement, ThingDef thingDef, int count)
        {
            registeredTrades.Add(new ThingTrade()
            {
                expiresAtTick = Find.TickManager.TicksGame + MaxTradeAge,
                count = count,
                thingDef = thingDef,
                settlement = settlement
            });
            priceModifiers = null;
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
            Messages.Message("Regen price modifiers", MessageTypeDefOf.NeutralEvent);
            var totalInCirculation = new Dictionary<ThingDef, int>();
            var totalTraded = new Dictionary<ThingDef, int>();

            UpdateRegisteredTrades();

            foreach (var settlement in Find.WorldObjects.Settlements)
            {
                var inStock = settlement.trader.StockListForReading;
                foreach (var t in inStock)
                {
                    if (!totalInCirculation.ContainsKey(t.def)) totalInCirculation[t.def] = 0;
                    totalInCirculation[t.def] += t.stackCount;
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
                    var inCirculationWithoutPlayer = inCirculation - boughtByPlayer;
                    priceModifiers[pair.Key] = inCirculation / (float)inCirculationWithoutPlayer;
                }
            }

            foreach (var settlement in Find.WorldObjects.Settlements)
            {
                var trader = settlement.trader;
                var prop = trader.GetType().GetField("everGeneratedStock", BindingFlags.NonPublic | BindingFlags.Instance);
                prop.SetValue(trader, false);
            }

            return priceModifiers;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksSinceUpdate, "ticksSinceUpdate");
            Scribe_Collections.Look(ref registeredTrades, "registeredTrades", LookMode.Deep);
            if (registeredTrades == null) registeredTrades = new List<ThingTrade>();
        }

        private void UpdateRegisteredTrades()
        {
            var expiredTrades = registeredTrades
                    .Where(t => t.expiresAtTick <= Find.TickManager.TicksGame)
                    .ToList();
            foreach (var t in expiredTrades)
            {
                Messages.Message("Forgetting trade " + t.count + "x " + t.thingDef.label, MessageTypeDefOf.NeutralEvent);
            }
            if (expiredTrades.Any()) priceModifiers = null;

            registeredTrades = registeredTrades
                .Where(t => t.expiresAtTick > Find.TickManager.TicksGame)
                .ToList();
        }

        class ThingTrade : IExposable
        {
            public ThingDef thingDef;
            public int count;
            public int expiresAtTick;
            public Settlement settlement;

            public void ExposeData()
            {
                Scribe_Defs.Look(ref thingDef, "thingDef");
                Scribe_Values.Look(ref count, "count");
                Scribe_Values.Look(ref expiresAtTick, "expiresAtTick");
                Scribe_References.Look(ref settlement, "settlement");
            }
        }
    }
}
