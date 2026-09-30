using System.Linq;
using NUnit.Framework;
using RPG.Core;
namespace RPG.Tests
{
    public class CityFoundationsAcceptanceTests
    {
        static void Damage(PersistentCharacter c,int hp,int armor)=>c.SetBattleResult(new UnitState(new UnitId(1),Side.West,c.Profile,new GridPosition(1,1),Facing.East,hp,armor,hp==0?UnitStatus.Dead:UnitStatus.Active));
        [Test] public void SourcePreviewUsesActualProductionWithoutMutationOrBacklogRegional()
        {
            var w=new CrossroadsScenario(foundations:true);var f=w.Foundations;f.Capture(14,Side.East);
            CityFoundationsTests.Cycle(w);CityFoundationsTests.Cycle(w);
            string hash=w.CaptureSave().checksum;var disconnected=f.PreviewSources(w).Single(s=>s.Node==16);
            Assert.That(disconnected.Output,Is.EqualTo(30));Assert.That(disconnected.Regional,Is.Zero);Assert.That(disconnected.Status,Does.Contain("interrupted"));Assert.That(w.CaptureSave().checksum,Is.EqualTo(hash));
            f.Capture(14,Side.West);hash=w.CaptureSave().checksum;var preview=f.PreviewSources(w);
            Assert.That(preview.Single(s=>s.Node==16).Regional,Is.EqualTo(10));Assert.That(preview.Where(s=>s.City==1).Sum(s=>s.Regional),Is.EqualTo(40));Assert.That(w.CaptureSave().checksum,Is.EqualTo(hash));
            CityFoundationsTests.Cycle(w);Assert.That(f.City(Side.West).Regional,Is.EqualTo(preview.Where(s=>s.City==1).Sum(s=>s.Regional)));Assert.That(w.West.Gold,Is.EqualTo(390));
        }
        [Test] public void ChangedReferenceYieldChangesProductionWithoutInventingMoreRegional()
        {
            var old=CityFoundations.Baseline[0];try{
                CityFoundations.Baseline[0]=60;var w=new CrossroadsScenario(foundations:true);CityFoundationsTests.Cycle(w);
                Assert.That(w.West.Gold,Is.EqualTo(360));Assert.That(w.Foundations.City(Side.West).Regional,Is.EqualTo(40));
            }finally{CityFoundations.Baseline[0]=old;}
        }
        [Test] public void FullFoodChainStopsProductionUntilActualSupplyCreatesRoom()
        {
            var w=new CrossroadsScenario(foundations:true);w.West.KeepFood=90;var f=w.Foundations;
            f.Location(14).Stock[1]=24;f.Location(17).Stock[1]=18;
            CityFoundationsTests.Cycle(w);Assert.That(f.City(Side.West).Regional,Is.EqualTo(30));Assert.That(f.Location(17).Stock[1],Is.EqualTo(18));Assert.That(f.Location(14).Stock[1],Is.EqualTo(24));Assert.That(w.West.KeepFood,Is.EqualTo(84));
            var restored=w.CaptureSave().Restore();CityFoundationsTests.Cycle(w);CityFoundationsTests.Cycle(restored);
            Assert.That(f.City(Side.West).Regional,Is.EqualTo(40));Assert.That(w.CaptureSave().checksum,Is.EqualTo(restored.CaptureSave().checksum));
        }
        [Test] public void ForgeQuotedIdentityDoesNotTransferToRecruitAndExtraDamageDoesNotIncreaseQuote()
        {
            var w=new CrossroadsScenario(foundations:true);var f=w.Foundations;f.City(Side.West).Forge=true;
            var dead=w.West.Formation.Members[1];var survivor=w.West.Formation.Members[2];Damage(dead,20,0);Damage(survivor,20,1);
            Assert.That(f.OrderRepair(w,Side.West),Is.True);Assert.That(dead.Hp,Is.EqualTo(20));Assert.That(survivor.Hp,Is.EqualTo(20));
            Assert.That(f.West.Repair.Quotes[survivor.CharacterId],Is.EqualTo(8));Damage(dead,0,0);Damage(survivor,20,0);
            Assert.That(w.Recruit(Side.West,UnitProfileId.HumanWarriorTI),Is.True);CityFoundationsTests.End(w);
            var recruit=w.West.Formation.Members.Last();Assert.That(recruit.CharacterId,Does.Contain("recruit"));Damage(recruit,30,0);
            var restored=w.CaptureSave().Restore();CityFoundationsTests.Cycle(w);CityFoundationsTests.Cycle(restored);
            Assert.That(dead.Status,Is.EqualTo(PersistentCharacterStatus.Dead));Assert.That(dead.Armor,Is.Zero);Assert.That(survivor.Armor,Is.EqualTo(8));Assert.That(recruit.Armor,Is.Zero);Assert.That(f.West.Repair,Is.Null);
            Assert.That(w.CaptureSave().checksum,Is.EqualTo(restored.CaptureSave().checksum));CityFoundationsTests.Cycle(w);Assert.That(survivor.Armor,Is.EqualTo(8));
        }
        static CrossroadsScenario TrainingWorld()
        {
            var w=new CrossroadsScenario(foundations:true,combined:true);Damage(w.West.Formation.Members[1],0,0);
            w.West.Formation.AddRecruit(new PersistentCharacter("duel-West-recruit-1",UnitProfile.FireMageTI,hp:7,personalXp:20));w.West.NextRecruit=2;w.Foundations.City(Side.West).MageTower=2;
            var t=w.Foundations.West.Research.Single(p=>p.Tech==CityTech.ElementalDrills);t.Paid=true;t.Work=6;t.Provenance.Add(1,6);return w;
        }
        [Test] public void PaidTrainingCancellationAndCommanderlessPauseSurviveReload()
        {
            var w=TrainingWorld();var f=w.Foundations;const string id="duel-West-recruit-1";
            Assert.That(f.Train(w,Side.West,id,true),Is.True);Assert.That(w.West.Gold,Is.EqualTo(225));w=w.CaptureSave().Restore();f=w.Foundations;
            Assert.That(f.CancelTraining(w,Side.West),Is.True);Assert.That(w.West.Gold,Is.EqualTo(225));Assert.That(w.West.Formation.Members.Last().Hp,Is.EqualTo(7));
            Assert.That(f.Train(w,Side.West,id,true),Is.True);Damage(w.West.Formation.Commander,0,0);w.West.Formation.RefreshCommanderState();
            var loaded=w.CaptureSave().Restore();for(int i=0;i<3;i++){CityFoundationsTests.Cycle(w);CityFoundationsTests.Cycle(loaded);}
            Assert.That(f.West.Training,Is.Not.Null);Assert.That(w.West.Formation.Members.Last().Profile,Is.EqualTo(UnitProfile.FireMageTI));Assert.That(w.CaptureSave().checksum,Is.EqualTo(loaded.CaptureSave().checksum));Assert.That(w.Recruit(Side.West,UnitProfileId.FireMageTI),Is.False);
        }
        [TestCase(DeepCapability.RecruitmentWing,0,0,1,2)]
        [TestCase(DeepCapability.Research,0,2,2,2)]
        [TestCase(DeepCapability.Training,1,1,1,1)]
        [TestCase(DeepCapability.Healing,0,1,2,2)]
        [TestCase(DeepCapability.ForgeMagic,1,1,1,1)]
        [TestCase(DeepCapability.CulturalSiege,1,1,1,1)]
        [TestCase(DeepCapability.Walls,0,0,0,0)]
        [TestCase(DeepCapability.SpecialSite,0,0,0,0)]
        public void DeepAccountingRowsAreAccountingOnly(DeepCapability capability,int one,int two,int three,int four)
        {Assert.That(Enumerable.Range(1,4).Select(t=>DeepAccounting.Load(capability,t)),Is.EqualTo(new[]{one,two,three,four}));}

        [Test] public void InstituteReplacementDoesNotStackAndUnavailableCityKeepsPhysicalLoad()
        {
            // Explicit context fixture: establish III/boosters/research, then use ordinary paid construction.
            var w=new CrossroadsScenario(foundations:true);var f=w.Foundations;var c=f.City(Side.West);
            c.Development=3;f.Location(14).Depot=f.Location(14).Assay=true;
            var t=f.West.Research.Single(p=>p.Tech==CityTech.ResearchMethod);t.Paid=true;t.Work=12;t.Provenance[1]=12;
            Assert.That(f.QueueProject(w,Side.West,1,CityProjectKind.ResearchInstitute),Is.True);
            Assert.That(c.ReservedLoad,Is.EqualTo(2));c.Functioning=false;CityFoundationsTests.Cycle(w);CityFoundationsTests.Cycle(w);
            Assert.That(c.Active.Steps,Is.Zero);Assert.That(c.ReservedLoad,Is.EqualTo(2));c.Functioning=true;
            for(int i=0;i<8&&!c.Institute;i++)CityFoundationsTests.Cycle(w);
            Assert.That(c.Institute,Is.True);Assert.That(c.PhysicalLoad,Is.EqualTo(2));Assert.That(c.ReservedLoad,Is.Zero);
            var gold=w.West.Gold;f.QueueProject(w,Side.West,1,CityProjectKind.ResearchInstitute);
            Assert.That(c.Active,Is.Null);Assert.That(w.West.Gold,Is.EqualTo(gold));Assert.That(c.PhysicalLoad,Is.EqualTo(2));
            c.Functioning=false;CityFoundationsTests.Cycle(w);Assert.That(c.PhysicalLoad,Is.EqualTo(2));Assert.That(c.DeepCapacity,Is.EqualTo(4));
            var restored=w.CaptureSave().Restore();Assert.That(restored.Foundations.City(Side.West).PhysicalLoad,Is.EqualTo(2));
        }
        [Test] public void DisconnectedMinorPaysOnlyItsOwnStock()
        {
            var w=new CrossroadsScenario(foundations:true);var f=w.Foundations;f.Capture(15,Side.West);
            var t=f.West.Research.Single(p=>p.Tech==CityTech.Extraction);t.Paid=true;t.Work=6;t.Provenance[1]=6;
            var site=f.Location(15);var cost=CityFoundations.Cost(CityProjectKind.ExtractionDepot);
            var gold=w.West.Gold;var wood=f.West.Wood;
            Assert.That(f.QueueProject(w,Side.West,15,CityProjectKind.ExtractionDepot),Is.True);Assert.That(site.Active,Is.Null);
            site.Stock[0]=cost.gold;site.Stock[2]=cost.wood;site.Stock[3]=cost.iron;
            f.RefreshEconomy(w);Assert.That(site.Active,Is.Not.Null);Assert.That(site.Stock[0],Is.Zero);
            Assert.That(w.West.Gold,Is.EqualTo(gold+30));Assert.That(f.West.Wood,Is.EqualTo(wood+6));
        }
        [Test] public void NewlyEligibleCityContributesOnlyRemainingResearchWork()
        {
            // Explicit multi-City eligibility fixture, not a new City-capture system.
            var w=new CrossroadsScenario(foundations:true);var f=w.Foundations;var second=f.Location(13);
            second.Controller=Side.West;second.Functioning=false;second.Institute=true;
            Assert.That(f.QueueResearch(w,Side.West,CityTech.Extraction),Is.True);
            CityFoundationsTests.Cycle(w);CityFoundationsTests.Cycle(w);CityFoundationsTests.Cycle(w);
            var p=f.West.Research.Single(t=>t.Tech==CityTech.Extraction);Assert.That(p.Work,Is.EqualTo(4));Assert.That(p.Provenance.ContainsKey(13),Is.False);
            second.Functioning=true;CityFoundationsTests.Cycle(w);
            Assert.That(p.Work,Is.EqualTo(6));Assert.That(p.Provenance[1],Is.EqualTo(4.5m));Assert.That(p.Provenance[13],Is.EqualTo(1.5m));
            Assert.That(f.West.ActiveResearch,Is.Null);CityFoundationsTests.Cycle(w);Assert.That(p.Provenance.Values.Sum(),Is.EqualTo(6));
            Assert.That(CityFoundations.Universal(CityTech.Extraction),Is.True);
            Assert.That(ResearchAccounting.Eligible(true,1,1,false,false,false),Is.True);
            Assert.That(ResearchAccounting.Eligible(true,1,1,true,false,false),Is.False);
        }
    }
}
