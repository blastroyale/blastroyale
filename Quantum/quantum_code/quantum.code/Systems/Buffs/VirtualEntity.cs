using System.Collections.Generic;
using System.Linq;
using Photon.Deterministic;
using Quantum;

namespace BuffSystem
{
    /// <summary>
    /// Used to calculate buffs.
    /// Does not require a frame or anything quantum specific, it's a virtual
    /// representation of a buff entity that in the future can also have an actual
    /// entity representation.
    /// </summary>
    public class BuffVirtualEntity
    {
        public HashSet<BuffConfig> ActiveBuffs = new HashSet<BuffConfig>();
        public Dictionary<BuffStat, BuffedStat> Stats = new Dictionary<BuffStat, BuffedStat>();

        public bool HasBuff(BuffConfig id) => ActiveBuffs.Contains(id);

        public FP GetStat(BuffStat stat)
        {
            Stats.TryGetValue(stat, out var s);
            return s.FinalValue;
        }
        
        public void AddBuff(BuffConfig spec)
        {
            if (ActiveBuffs.Contains(spec))
            {
                return;
            }
            ActiveBuffs.Add(spec);
            foreach (var modifier in spec.Modifiers)
            {
                if (!Stats.TryGetValue(modifier.Stat, out var statValue))
                {
                    statValue = new BuffedStat();
                    Stats[modifier.Stat] = statValue;
                }

                var stat = Stats[modifier.Stat];
                switch (modifier.Op)
                {
                    case BuffOperator.ADD:
                        stat.Additives += modifier.Value;
                        break;
                    case BuffOperator.MULT:
                        stat.Multiplicatives += modifier.Value;
                        break;
                    case BuffOperator.PCT:
                        stat.Percentages += modifier.Value;
                        break;
                }
                stat.FinalValue =
                    (stat.Additives *
                        (1 + stat.Multiplicatives)) *
                    (1 + stat.Percentages);
                Stats[modifier.Stat] = stat;
            }
        }

        public override string ToString()
        {
            var stats = Stats.Select(kp => $"({kp.Value.FinalValue.AsInt} {kp.Key})");
            return @$"<VirtualBuffEntity Stats={string.Join(",", stats)} Buffs={string.Join(",", ActiveBuffs.Select(b=> b.Id.ToString()))}";
        }
    }
}
