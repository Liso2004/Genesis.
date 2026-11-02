#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Genesis.Character;
using Genesis.Core;
using Genesis.Creation;
using Genesis.Items;
using Genesis.Skill;
using Genesis.Skill.Definitions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Genesis.Tests
{
    public sealed class PlayerCreationTests
    {
        private static CreationDefinition Definition => AssetDatabase.LoadAssetAtPath<CreationDefinition>("Assets/Content/Creation/GenesisCreation.asset");
        private static PlayerCreationPreset Preset => AssetDatabase.LoadAssetAtPath<PlayerCreationPreset>("Assets/Content/Creation/SandboxStartingProfile.asset");
        private static PlayerCreationSession Session(int seed,bool fate=false,IReadOnlyDictionary<string,float> resonance=null)
        {
            var session=new PlayerCreationSession(Definition,seed,resonance);session.Begin();session.CompleteLoreIntro();session.SelectRace(Preset.race);
            session.SetAppearance("Test Wanderer",Preset.appearanceParts);session.AnswerQuestions(Preset.answers,fate);return session;
        }
        private static PlayerCreationProfile Profile(int seed,bool fate=false,StartingSkillMode mode=StartingSkillMode.Guided)
        {var session=Session(seed,fate);session.DetermineStartingSkills(mode);session.AwakenUnique();return session.ConfirmSummary();}

        [Test] public void CreationOrderCannotSkipIntroOrChooseUniqueDirectly()
        {
            var session=new PlayerCreationSession(Definition,42);
            Assert.Throws<InvalidOperationException>(()=>session.SelectRace(Preset.race));
            session.Begin();Assert.That(session.Stage,Is.EqualTo(CreationStage.LoreIntro));session.CompleteLoreIntro();
            session.SelectRace(Preset.race);session.SetAppearance("Wanderer",Preset.appearanceParts);session.AnswerQuestions(Preset.answers);
            Assert.Throws<InvalidOperationException>(()=>session.AwakenUnique());
            session.DetermineStartingSkills(StartingSkillMode.Guided);session.AwakenUnique();
            Assert.Throws<InvalidOperationException>(()=>session.AwakenUnique());
            var profile=session.ConfirmSummary();Assert.That(session.Stage,Is.EqualTo(CreationStage.EnterAethel));
            Assert.That(profile.InitialAllegiance,Is.EqualTo("None"));Assert.That(profile.InitialReputation,Is.EqualTo("Unknown"));
        }
        [Test] public void SameSeedIsReproducibleButAwakeningsVaryAcrossSeeds()
        {
            var first=Profile(17);var second=Profile(17);
            Assert.That(first.UniqueSkill,Is.SameAs(second.UniqueSkill));CollectionAssert.AreEqual(first.StartingBasicSkills,second.StartingBasicSkills);
            var skills=new HashSet<string>();for(int seed=0;seed<128;seed++)skills.Add(Profile(seed).UniqueSkill.SkillId);
            Assert.That(skills.Count,Is.GreaterThanOrEqualTo(3));
        }
        [Test] public void FateAndSurpriseKeepBasicsSmallAndUniqueSeparate()
        {
            foreach(bool fate in new[]{false,true})
            {
                var profile=Profile(21,fate,StartingSkillMode.Random);
                Assert.That(profile.StartingBasicSkills.Count,Is.InRange(2,4));
                Assert.That(profile.StartingBasicSkills.All(s=>s.Tier==SkillTier.Basic),Is.True);
                Assert.That(profile.UniqueSkill.Tier,Is.EqualTo(SkillTier.Unique));
                Assert.That(profile.Answers.Count,Is.EqualTo(fate?0:6));
                Assert.That(profile.Personality.Values.Distinct().Count(),Is.GreaterThan(1));
            }
        }
        [Test] public void ChoicesMustComeFromTheOfferedDistinctBasicPool()
        {
            var session=Session(9);var offered=session.GetBasicCandidates();
            Assert.Throws<ArgumentException>(()=>session.DetermineStartingSkills(StartingSkillMode.Choice,new[]{offered[0],offered[0],offered[1]}));
            Assert.Throws<ArgumentException>(()=>session.DetermineStartingSkills(StartingSkillMode.Choice,new[]{Definition.uniqueSkills[0].skill,offered[0],offered[1]}));
            session.DetermineStartingSkills(StartingSkillMode.Choice,offered.Take(3).ToArray());
            session.AwakenUnique();Assert.That(session.ConfirmSummary().StartingBasicSkills.Count,Is.EqualTo(3));
        }
        [Test] public void AllBibleRacesRemainInDataWithExplicitPendingVisuals()
        {
            CollectionAssert.AreEquivalent(new[]{"human","beastfolk","ash-kin","root-gnawers","weavers"},Definition.races.Select(r=>r.raceId));
            foreach(var race in Definition.races)
            {
                var session=new PlayerCreationSession(Definition,3);session.Begin();session.CompleteLoreIntro();
                session.SelectRace(race,race.raceId=="beastfolk"?"feline":"");
                session.SetAppearance("Test",race.VisualPending?Array.Empty<string>():Preset.appearanceParts);
                session.AnswerQuestions(null,true);session.DetermineStartingSkills(StartingSkillMode.Random);session.AwakenUnique();
                Assert.That(session.ConfirmSummary().Race,Is.SameAs(race));
                Assert.That(race.VisualPending,Is.EqualTo(race.raceId!="human"));
            }
            var beast=Definition.races.Single(r=>r.raceId=="beastfolk");Assert.That(beast.subRaces.Length,Is.EqualTo(5));Assert.That(beast.AcceptsSubRace("invented"),Is.False);
        }
        [Test] public void ProfileCopiesAnswersAndRejectsEquipmentAsIdentity()
        {
            var session=new PlayerCreationSession(Definition,2);session.Begin();session.CompleteLoreIntro();session.SelectRace(Preset.race);
            var invalid=Preset.appearanceParts.Concat(new[]{"Chr_BackAttachment_03"}).ToArray();
            Assert.Throws<ArgumentException>(()=>session.SetAppearance("Test",invalid));
            session.SetAppearance("Test",Preset.appearanceParts);var answers=(int[])Preset.answers.Clone();session.AnswerQuestions(answers);answers[0]=2;
            session.DetermineStartingSkills(StartingSkillMode.Guided);session.AwakenUnique();var profile=session.ConfirmSummary();
            Assert.That(profile.Answers[0],Is.EqualTo(Preset.answers[0]));
            Assert.Throws<NotSupportedException>(()=>((IList<int>)profile.Answers)[0]=1);
        }
        [Test] public void ResonanceWeightsInfluenceAwakeningWithoutGrantingFavor()
        {
            int sage=0;for(int seed=0;seed<128;seed++)
            {
                var session=Session(seed,false,new Dictionary<string,float>{{"knowledge",10000}});session.DetermineStartingSkills(StartingSkillMode.Random);
                if(session.AwakenUnique().SkillId=="great-sage")sage++;
            }
            Assert.That(sage,Is.GreaterThan(120));
        }
        [UnityTest] public IEnumerator ProfileAppliesOnceAndEquipmentRestoresAlternativeIdentity()
        {
            var runtime=GenesisRuntime.Instance==null?new GameObject("Creation test runtime"):null;if(runtime!=null)runtime.AddComponent<GenesisRuntime>();GameObject player=null;
            try
            {
                player=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Genesis/Character/Prefabs/GenesisPlayer.prefab"));player.GetComponent<PlayerController>().enabled=false;
                var creation=player.GetComponent<PlayerCreationBootstrap>();var skills=player.GetComponent<SkillSystem>();var inventory=player.GetComponent<ActorInventory>();
                Assert.That(creation.Profile,Is.Not.Null);Assert.That(skills.LearnedSkillIds.Count(),Is.EqualTo(4));Assert.That(inventory.Inventory.Entries.Count,Is.Zero);
                Assert.That(creation.TryApply(Preset.Generate(),Definition.progression,out _),Is.False);
                var appearance=player.GetComponentInChildren<ModularAppearance>();var ids=creation.Profile.AppearanceParts.Select(id=>id=="Chr_Hair_01"?"Chr_Hair_02":id=="Chr_Head_Male_00"?"Chr_Head_Male_01":id).Concat(new[]{"Chr_FacialHair_Male_01"}).ToArray();
                Assert.That(appearance.TrySetIdentity(ids),Is.True);
                var helmet=AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Content/Sandbox/Systems/Item-training-head.asset");inventory.Inventory.TryAdd(helmet,1);inventory.Equipment.TryEquip(helmet.ItemId);
                Assert.That(appearance.IsPartVisible("Chr_Hair_02"),Is.False);inventory.Equipment.TryUnequip(EquipmentSlot.Head);
                Assert.That(appearance.IsPartVisible("Chr_Hair_02"),Is.True);Assert.That(appearance.IsPartVisible("Chr_Head_Male_01"),Is.True);
                Assert.That(appearance.IsPartVisible("Chr_FacialHair_Male_01"),Is.True);
                skills.RegisterMeaningfulUse(creation.Profile.StartingBasicSkills[0].SkillId,1);
                Assert.That(creation.TryApply(Preset.Generate(),Definition.progression,out _),Is.False);
                Assert.That(inventory.Inventory.Get(helmet.ItemId).Count,Is.EqualTo(1));yield return null;
            }
            finally{if(player!=null)Object.DestroyImmediate(player);if(runtime!=null)Object.DestroyImmediate(runtime);}
        }
    }
}
#endif
