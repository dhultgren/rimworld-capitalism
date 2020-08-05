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

        private List<ThingTrade> registeredTrades = new List<ThingTrade>();
        private List<TemporaryTrader> temporaryTraders = new List<TemporaryTrader>();
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
            ExpireOldData(registeredTrades);
            ExpireOldData(temporaryTraders);


            var totalInCirculation = new Dictionary<ThingDef, int>();
            var totalTraded = new Dictionary<ThingDef, int>();

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

            foreach (var trader in temporaryTraders)
            {
                foreach (var t in trader.goods)
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
                    var initialModifier = (float)Math.Sqrt(inCirculationWithoutPlayer / (float)inCirculation);
                    var multipliedModifier = 1 + (initialModifier - 1) * Capitalism.Settings.EffectMultiplier;
                    var maxModifier = Capitalism.Settings.MaxSupplyDemandChangePercent / 100f;
                    priceModifiers[pair.Key] = Math.Min(Math.Max(multipliedModifier, 1 / maxModifier), maxModifier);
                }
            }

            CapitalismUtils.LogAndMessage("New price modifiers: " + string.Join(", ", priceModifiers.Select(kv => kv.Key.label + ":" + kv.Value.ToString("F"))));

            return priceModifiers;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksSinceUpdate, "ticksSinceUpdate");
            Scribe_Collections.Look(ref registeredTrades, "registeredTrades", LookMode.Deep);
            Scribe_Collections.Look(ref temporaryTraders, "temporaryTraders", LookMode.Deep);
            if (registeredTrades == null) registeredTrades = new List<ThingTrade>();
            if (temporaryTraders == null) temporaryTraders = new List<TemporaryTrader>();
        }

        public void RegisterTrade(Faction faction, ITrader trader, ThingDef thingDef, int count)
        {
            var timeToForget = trader is Settlement
                ? Capitalism.Settings.RememberSettlementMaxTime
                : trader is Pawn || trader is Caravan
                    ? Capitalism.Settings.RememberCaravanTime
                    : Capitalism.Settings.RememberOrbitalTradersTime;
            registeredTrades.Add(new ThingTrade()
            {
                expiresAtTick = Find.TickManager.TicksGame + timeToForget,
                count = count,
                thingDef = CapitalismUtils.GroupCertainThingDefs(thingDef),
                settlement = trader as Settlement,
                faction = faction
            });
            priceModifiers = null;
        }

        public void RegisterCaravanTrader(Pawn pawn, string name, List<Thing> goods)
        {
            RegisterTemporaryTrader(pawn, name, goods, Capitalism.Settings.RememberCaravanTime);
        }

        public void RegisterOrbitalTrader(string name, List<Thing> goods)
        {
            RegisterTemporaryTrader(null, name, goods, Capitalism.Settings.RememberOrbitalTradersTime);
        }

        private void RegisterTemporaryTrader(Pawn pawn, string name, List<Thing> goods, int forgetTraderAfter)
        {
            if (temporaryTraders.Any(t => t?.pawn == pawn && t?.name == name)) return;

            temporaryTraders.Add(new TemporaryTrader
            {
                expiresAtTick = Find.TickManager.TicksGame + forgetTraderAfter,
                pawn = pawn,
                name = name,
                goods = goods
            });
            CapitalismUtils.LogAndMessage("Registering temporary trader " + temporaryTraders.Last().ToString());
        }

        private void ExpireOldData(IEnumerable<IExpirable> expirableData)
        {
            var expired = expirableData
                       .Where(t => t.HasExpired)
                       .ToList();
            foreach (var t in expired)
            {
                CapitalismUtils.LogAndMessage("Forgetting " + t.ToString());
            }
            if (expired.Any()) priceModifiers = null;

            expirableData = expirableData
                .Where(t => !t.HasExpired)
                .ToList();
        }

        interface IExpirable
        {
            bool HasExpired { get; }
        }

        class ThingTrade : IExposable, IExpirable
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

            public bool HasExpired => Find.TickManager.TicksGame >= expiresAtTick;

            public override string ToString()
            {
                return count + "x " + thingDef.label;
            }
        }

        class TemporaryTrader : IExposable, IExpirable
        {
            public Pawn pawn;
            public int expiresAtTick;
            public string name;
            public List<Thing> goods;

            public void ExposeData()
            {
                Scribe_References.Look(ref pawn, "pawn");
                Scribe_Values.Look(ref expiresAtTick, "expiresAtTick");
                Scribe_Values.Look(ref name, "name");
                Scribe_Collections.Look(ref goods, "goods", LookMode.Reference);
            }

            public override string ToString()
            {
                return pawn?.Label ?? name;
            }

            public bool HasExpired => Find.TickManager.TicksGame >= expiresAtTick;
        }
    }
}
