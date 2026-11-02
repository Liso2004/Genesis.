using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Genesis.Character;
using Genesis.Creation;
using Genesis.Items;
using Genesis.NPC;
using Genesis.Skill.Definitions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using A = Genesis.Sandbox.Editor.SandboxSystemsAssets;

namespace Genesis.Sandbox.Editor
{
    public static class CreationFoundationSetup
    {
        private const string Root="Assets/Content/Creation";
        private static readonly Dictionary<string,ScriptableObject> Created=new();
        [MenuItem("Genesis/Prototype/4 Configure Creation Foundation")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying||SceneManager.GetActiveScene().path!="Assets/Scenes/Sandbox/Sandbox.unity")throw new InvalidOperationException("Open Sandbox in Edit mode.");
            if(Object.FindFirstObjectByType<PlayerCreationBootstrap>()!=null)throw new InvalidOperationException("Creation foundation is already configured.");
            Directory.CreateDirectory(Root); AssetDatabase.Refresh();
            ConfigureAppearanceOptions();
            var human=Race("human","Human","Adaptability, broad learning potential and resilience.",new[]{"Adaptive Potential","Enduring Will","Legacy of the First"});
            human.compatibleVisualPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrototypeInventorySetup.ModelPath);
            var beast=Race("beastfolk","Beastfolk","Instinctual peoples with distinct racial capabilities.",Array.Empty<string>());
            string[] subNames={"Canine","Feline","Ursine","Avian","Reptilian"}, traits={"Keen Nose","Silent Step","Iron Hide","Lightweight","Cold Blood"};
            beast.subRaces=subNames.Select((n,i)=>new SubRaceDefinition{id=n.ToLowerInvariant(),displayName=n,traits=new[]{traits[i]}}).ToArray();
            var ash=Race("ash-kin","Ash-Kin","Memory, fire, fading existence and the past.",new[]{"Ash-Skin","Ember Memory","Fading Form","Warmth Sense"});
            var root=Race("root-gnawers","Root-Gnawers","Nature, decay and ecological awareness.",new[]{"Root-Bond","Prune","Green Tongue","Animus Ward"});
            var weaver=Race("weavers","Weavers","Silk, architecture, trade, unusual traversal and group connection.",new[]{"Silk-Weave","Gossamer Step","Web-Sense","Hive-Mind Instinct"});
            var definition=Asset<CreationDefinition>("GenesisCreation");
            definition.races=new[]{human,beast,ash,root,weaver};
            definition.startingSkillCount=3; definition.choiceCandidateCount=5;
            definition.progression=AssetDatabase.LoadAssetAtPath<SkillProgressionConfig>("Assets/Content/Skills/Perception/PerceptionProgression.asset");
            definition.loreIntroduction="The universe is a classroom. The System recognizes potential, and the Gods watch a living world. In Aethel, civilizations continue their own lives while people seek power, answers and purpose in the Dungeon. The Dungeon teaches; the world tests what is learned. You arrive unknown and unequipped. Nothing says who you must become.";
            definition.questions=Questions();
            var perception=AssetDatabase.LoadAssetAtPath<SkillDefinition>("Assets/Content/Skills/Perception/SkillDefinition.asset");
            var sword=AssetDatabase.LoadAssetAtPath<SkillDefinition>("Assets/Content/Sandbox/Systems/Swordsmanship.asset");
            var dodge=Skill("dodge","Dodge","Basic evasive movement.",SkillTier.Basic);
            var appraise=Skill("appraise","Appraise","Basic assessment of an object's nature and value.",SkillTier.Basic);
            var track=Skill("track","Track","Follow signs left by a creature's passage.",SkillTier.Basic);
            var forage=Skill("forage","Forage","Recognize and gather useful natural resources.",SkillTier.Basic);
            var light=Skill("light","Light","A foundational light-producing magical skill.",SkillTier.Basic);
            var sneak=Skill("sneak","Sneak","Move with care to avoid notice.",SkillTier.Basic);
            definition.basicSkills=new[]{
                Candidate(perception,PersonalityTrait.Observation,PersonalityTrait.Curiosity),Candidate(sword,PersonalityTrait.Discipline,PersonalityTrait.Ambition),
                Candidate(dodge,PersonalityTrait.RiskTolerance,PersonalityTrait.Observation),Candidate(appraise,PersonalityTrait.Curiosity,PersonalityTrait.Patience),
                Candidate(track,PersonalityTrait.Observation,PersonalityTrait.Patience),Candidate(forage,PersonalityTrait.Patience,PersonalityTrait.Compassion),
                Candidate(light,PersonalityTrait.Creativity,PersonalityTrait.Curiosity),Candidate(sneak,PersonalityTrait.Observation,PersonalityTrait.Discipline)};
            definition.basicSkills[4].races=new[]{Affinity(beast,1.4f,"canine")};
            definition.basicSkills[7].races=new[]{Affinity(beast,1.4f,"feline")};
            definition.uniqueSkills=new[]{
                Candidate(Skill("great-sage","Great Sage","Analysis, accelerated thought and parallel processing.",SkillTier.Unique),PersonalityTrait.Curiosity,PersonalityTrait.Observation),
                Candidate(Skill("predator","Predator","Absorb and analyze what is consumed.",SkillTier.Unique),PersonalityTrait.Ambition,PersonalityTrait.RiskTolerance),
                Candidate(Skill("defender","Defender","Shield allies and absorb damage.",SkillTier.Unique),PersonalityTrait.Compassion,PersonalityTrait.Discipline),
                Candidate(Skill("cartographer","Cartographer","Remember locations and create maps.",SkillTier.Unique),PersonalityTrait.Observation,PersonalityTrait.Creativity)};
            definition.uniqueSkills[0].races=new[]{Affinity(human,1.1f),Affinity(ash,1.2f)};
            definition.uniqueSkills[1].races=new[]{Affinity(beast,1.2f)};
            definition.uniqueSkills[3].races=new[]{Affinity(root,1.2f),Affinity(weaver,1.2f)};
            definition.uniqueSkills[0].resonance=new[]{new ResonanceAffinity{domainId="knowledge",weight=.5f}};
            var preset=Asset<PlayerCreationPreset>("SandboxStartingProfile");
            preset.definition=definition; preset.race=human; preset.playerName="Sandbox Wanderer";
            preset.appearanceParts=(string[])PrototypeInventorySetup.BaseParts.Clone();
            preset.answers=new[]{0,1,0,1,0,1}; preset.mode=StartingSkillMode.Choice;
            preset.choices=new[]{perception,sword,dodge};
            // A reproducible test profile keeps the existing Perception and training stations available.
            // Runtime creation never searches for a preferred Unique result.
            for(int seed=0;seed<10000;seed++)
            {
                var session=new PlayerCreationSession(definition,seed); session.Begin();session.CompleteLoreIntro();session.SelectRace(human);
                session.SetAppearance(preset.playerName,preset.appearanceParts);session.AnswerQuestions(preset.answers);
                if(preset.choices.All(c=>session.GetBasicCandidates().Contains(c))){preset.seed=seed;break;}
            }
            var profile=preset.Generate();
            foreach(var asset in Created.Values)EditorUtility.SetDirty(asset);
            foreach(var race in definition.races)EditorUtility.SetDirty(race);
            AssetDatabase.SaveAssets();
            var player=PrefabUtility.LoadPrefabContents(PrototypeInventorySetup.PlayerPath);
            try
            {
                A.Set(player.GetComponent<NPC_Setup>(),"registerPerceptionOnAwake",false);
                var creation=player.GetComponent<PlayerCreationBootstrap>()??player.AddComponent<PlayerCreationBootstrap>();
                A.Set(creation,"testPreset",preset);
                PrefabUtility.SaveAsPrefabAsset(player,PrototypeInventorySetup.PlayerPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(player);}
            var live=Object.FindFirstObjectByType<PlayerController>();
            A.Set(live.GetComponent<SandboxActor>(),"registerScenarioSkills",false);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            File.WriteAllText("Validation/GenesisPolish/Creation-test-profile.txt",profile.Summary+"\nSeed: "+preset.seed);
            Debug.Log("[Genesis Creation] Five Bible races, six questions, eight Basic candidates, four Unique candidates and the Sandbox test profile authored. Non-human visuals remain explicitly pending.");
        }
        private static T Asset<T>(string name)where T:ScriptableObject
        {
            string path=Root+"/"+name+".asset";
            if(Created.TryGetValue(path,out var cached))return (T)cached;
            var asset=AssetDatabase.LoadAssetAtPath<T>(path);
            if(asset==null&&File.Exists(path)){AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);asset=AssetDatabase.LoadAssetAtPath<T>(path);}
            if(asset==null){asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);}
            Created.Add(path,asset);return asset;
        }
        private static RaceDefinition Race(string id,string name,string description,string[] traits)
        {var race=Asset<RaceDefinition>("Race-"+id);race.raceId=id;race.displayName=name;race.description=description;race.traits=traits;return race;}
        private static SkillDefinition Skill(string id,string name,string description,SkillTier tier)
        {
            var skill=Asset<SkillDefinition>("Skill-"+id);A.Set(skill,"skillId",id);A.Set(skill,"displayName",name);A.Set(skill,"description",description);A.Set(skill,"tier",(int)tier);
            A.Set(skill,"category",(int)(id=="great-sage"||id=="cartographer"||id=="appraise"?SkillCategory.GiftedKnowledge:SkillCategory.Active));return skill;
        }
        private static TraitWeight W(PersonalityTrait trait,float weight=1)=>new(){trait=trait,weight=weight};
        private static RaceAffinity Affinity(RaceDefinition race,float weight,string sub="")=>new(){race=race,subRaceId=sub,multiplier=weight};
        private static StartingSkillCandidate Candidate(SkillDefinition skill,PersonalityTrait primary,PersonalityTrait secondary)=>new(){skill=skill,personality=new[]{W(primary,2),W(secondary)}};
        private static PersonalityAnswer Answer(string text,PersonalityTrait a,PersonalityTrait b)=>new(){text=text,weights=new[]{W(a,2),W(b)}};
        private static PersonalityQuestion[] Questions()=>new[]{
            new PersonalityQuestion{id="sealed-ruin",text="You discover a sealed ruin that the town avoids. What interests you first?",answers=new[]{Answer("What may be hidden inside.",PersonalityTrait.Curiosity,PersonalityTrait.RiskTolerance),Answer("Why someone sealed it.",PersonalityTrait.Observation,PersonalityTrait.Patience),Answer("What preparation an expedition would need.",PersonalityTrait.Discipline,PersonalityTrait.Leadership)}},
            new PersonalityQuestion{id="threatened-stranger",text="Someone stronger threatens a stranger. What is your first instinct?",answers=new[]{Answer("Step between them.",PersonalityTrait.Compassion,PersonalityTrait.RiskTolerance),Answer("Observe what is happening before acting.",PersonalityTrait.Observation,PersonalityTrait.Patience),Answer("Find someone who can help.",PersonalityTrait.Leadership,PersonalityTrait.Compassion)}},
            new PersonalityQuestion{id="unknown-object",text="You receive an object nobody understands. What do you do?",answers=new[]{Answer("Study its details.",PersonalityTrait.Curiosity,PersonalityTrait.Discipline),Answer("Try a careful experiment.",PersonalityTrait.Creativity,PersonalityTrait.RiskTolerance),Answer("Ask someone with relevant knowledge.",PersonalityTrait.Leadership,PersonalityTrait.Curiosity)}},
            new PersonalityQuestion{id="journey",text="You have a day to reach a distant settlement. Which route appeals to you?",answers=new[]{Answer("An unfamiliar shortcut.",PersonalityTrait.RiskTolerance,PersonalityTrait.Ambition),Answer("A reliable road with time to prepare.",PersonalityTrait.Discipline,PersonalityTrait.Patience),Answer("A route with interesting landmarks.",PersonalityTrait.Observation,PersonalityTrait.Curiosity)}},
            new PersonalityQuestion{id="shared-task",text="A group has more work than daylight. How do you contribute?",answers=new[]{Answer("Help coordinate the work.",PersonalityTrait.Leadership,PersonalityTrait.Discipline),Answer("Find a more efficient method.",PersonalityTrait.Creativity,PersonalityTrait.Patience),Answer("Assist whoever is struggling.",PersonalityTrait.Compassion,PersonalityTrait.Observation)}},
            new PersonalityQuestion{id="setback",text="Your first attempt at a difficult task fails. What draws you onward?",answers=new[]{Answer("Finding a different explanation.",PersonalityTrait.Creativity,PersonalityTrait.Curiosity),Answer("Practicing the difficult part.",PersonalityTrait.Discipline,PersonalityTrait.Patience),Answer("Trying another route to the goal.",PersonalityTrait.RiskTolerance,PersonalityTrait.Ambition)}}};

        private static void ConfigureAppearanceOptions()
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonFantasyHeroCharacters/Prefabs/ModularCharacters.prefab");
            var model=PrefabUtility.LoadPrefabContents(PrototypeInventorySetup.ModelPath);
            try
            {
                foreach(string id in new[]{"Chr_Head_Male_01","Chr_Hair_02","Chr_FacialHair_Male_01"})
                {
                    if(model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(r=>r.name==id))continue;
                    var original=source.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name==id);
                    var parent=model.transform.Find(AnimationUtility.CalculateTransformPath(original.transform.parent,source.transform));
                    var part=Object.Instantiate(original.gameObject,parent);part.name=id;part.SetActive(false);
                    var renderer=part.GetComponent<SkinnedMeshRenderer>();
                    renderer.bones=original.bones.Select(b=>model.transform.Find(AnimationUtility.CalculateTransformPath(b,source.transform))).ToArray();
                    renderer.rootBone=model.transform.Find(AnimationUtility.CalculateTransformPath(original.rootBone,source.transform));
                    if(renderer.bones.Any(b=>b==null))throw new InvalidOperationException("Incomplete appearance bone mapping.");
                }
                var appearance=model.GetComponent<ModularAppearance>();
                A.References(appearance,"parts",model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r=>(Object)r.gameObject).ToArray());
                var data=new SerializedObject(appearance);var groups=data.FindProperty("identityGroups");groups.arraySize=PrototypeInventorySetup.BaseParts.Length+1;
                for(int i=0;i<PrototypeInventorySetup.BaseParts.Length;i++)
                {
                    string id=PrototypeInventorySetup.BaseParts[i];var group=groups.GetArrayElementAtIndex(i);
                    group.FindPropertyRelative("id").stringValue=id;group.FindPropertyRelative("required").boolValue=i!=1&&i!=2;
                    string[] options=i==0?new[]{id,"Chr_Head_Male_01"}:i==2?new[]{id,"Chr_Hair_02"}:new[]{id};
                    var list=group.FindPropertyRelative("partIds");list.arraySize=options.Length;for(int j=0;j<options.Length;j++)list.GetArrayElementAtIndex(j).stringValue=options[j];
                }
                var facial=groups.GetArrayElementAtIndex(groups.arraySize-1);facial.FindPropertyRelative("id").stringValue="Facial hair";facial.FindPropertyRelative("required").boolValue=false;
                var choices=facial.FindPropertyRelative("partIds");choices.arraySize=1;choices.GetArrayElementAtIndex(0).stringValue="Chr_FacialHair_Male_01";
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(model,PrototypeInventorySetup.ModelPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(model);}
            AssetDatabase.ImportAsset(PrototypeInventorySetup.ModelPath,ImportAssetOptions.ForceSynchronousImport);
            var helmet=AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Content/Sandbox/Systems/Item-training-head.asset");
            PrototypeInventorySetup.Strings(helmet,"hiddenParts",new[]{"Chr_Hair_01","Chr_Hair_02"});
        }
    }
}
