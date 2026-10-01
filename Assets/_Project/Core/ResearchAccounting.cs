using System;
using System.Collections.Generic;
using System.Linq;
namespace RPG.Core
{
    public static class ResearchAccounting
    {
        public static SortedDictionary<int,decimal> Accept(IReadOnlyDictionary<int,decimal> eligibleRates,decimal remaining)
        {
            if(remaining<0||eligibleRates.Any(p=>p.Value<0))throw new ArgumentOutOfRangeException();
            var result=new SortedDictionary<int,decimal>();decimal total=eligibleRates.Values.Sum(),accepted=Math.Min(total,remaining),left=accepted;
            var inputs=eligibleRates.Where(p=>p.Value>0).OrderBy(p=>p.Key).ToArray();
            for(int i=0;i<inputs.Length;i++){var entry=inputs[i];decimal work=i==inputs.Length-1?left:accepted*entry.Value/total;left-=work;result.Add(entry.Key,work);}return result;
        }
        // Task-local rule fixtures use explicit resolved context; no live cultural simulation.
        public static bool Eligible(bool functioning,int institutionTier,int requiredTier,bool deepRacial,bool matchingRealm,bool matchingCore)
            =>functioning&&institutionTier>=requiredTier&&(!deepRacial||matchingRealm&&matchingCore);
    }
}
