using System;
using System.Collections.Generic;
using System.Linq;
using Genesis.Skill.Definitions;

namespace Genesis.Creation
{
    /// <summary>Ordered creation flow, independent of any UI or scene. Seeded draws are reproducible and testable.</summary>
    public sealed class PlayerCreationSession
    {
        private readonly CreationDefinition _definition;
        private readonly Random _random;
        private readonly int _seed;
        private readonly Dictionary<PersonalityTrait,float> _personality = new();
        private readonly Dictionary<string,float> _resonance;
        private RaceDefinition _race;
        private string _subRace, _name;
        private string[] _appearance;
        private int[] _answers;
        private SkillDefinition[] _basics, _candidates;
        private SkillDefinition _unique;
        private bool _fate;
        private StartingSkillMode _mode;
        public CreationStage Stage { get; private set; } = CreationStage.NewGame;

        public PlayerCreationSession(CreationDefinition definition, int seed, IReadOnlyDictionary<string,float> initialResonance = null)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (definition.questions.Length < 5 || definition.questions.Length > 8 || definition.startingSkillCount < 2 || definition.startingSkillCount > 4)
                throw new ArgumentException("Creation requires 5–8 questions and 2–4 Basic skills.");
            ValidatePool(definition.basicSkills, SkillTier.Basic, definition.startingSkillCount);
            ValidatePool(definition.uniqueSkills, SkillTier.Unique, 1);
            _seed=seed; _random=new Random(seed); _resonance=new Dictionary<string,float>();
            if(initialResonance!=null) foreach(var pair in initialResonance)
            { if(!Finite(pair.Value)||pair.Value<0)throw new ArgumentException("Invalid resonance hint."); _resonance.Add(pair.Key,pair.Value); }
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static void ValidatePool(StartingSkillCandidate[] pool, SkillTier tier, int minimum)
        {
            if(pool==null||pool.Length<minimum)throw new ArgumentException("Insufficient "+tier+" candidates.");
            var ids=new HashSet<string>();
            foreach(var entry in pool)
            {
                if(entry?.skill==null||!entry.skill.IsValid||entry.skill.Tier!=tier||!ids.Add(entry.skill.SkillId)||!Finite(entry.baseWeight)||entry.baseWeight<=0)
                    throw new ArgumentException("Invalid or duplicate "+tier+" candidate.");
                if(entry.personality.Any(w=>!Finite(w.weight))||entry.races.Any(r=>r.race==null||!Finite(r.multiplier)||r.multiplier<=0)||entry.resonance.Any(r=>!Finite(r.weight)))
                    throw new ArgumentException("Invalid candidate weighting.");
            }
        }
        private void Require(CreationStage expected)
        { if(Stage!=expected)throw new InvalidOperationException("Expected "+expected+", current stage is "+Stage+"."); }
        public void Begin() { Require(CreationStage.NewGame); Stage=CreationStage.LoreIntro; }
        public void CompleteLoreIntro() { Require(CreationStage.LoreIntro); Stage=CreationStage.Race; }
        public void SelectRace(RaceDefinition race, string subRace = "")
        {
            Require(CreationStage.Race);
            if(race==null||!_definition.races.Contains(race)||!race.AcceptsSubRace(subRace))throw new ArgumentException("Select an authored race and valid sub-race.");
            _race=race; _subRace=subRace??""; Stage=CreationStage.Appearance;
        }
        public void SetAppearance(string name, IReadOnlyList<string> parts)
        {
            Require(CreationStage.Appearance);
            if(string.IsNullOrWhiteSpace(name)||name.Trim().Length>40||parts==null||parts.Any(string.IsNullOrWhiteSpace)||parts.Distinct().Count()!=parts.Count)
                throw new ArgumentException("A name and valid appearance part IDs are required.");
            if(!_race.VisualPending && parts.Count==0)throw new ArgumentException("Choose the supported modular appearance.");
            if(!_race.VisualPending)
            {
                var appearance=_race.compatibleVisualPrefab.GetComponent<Genesis.Character.ModularAppearance>();
                if(appearance==null||!appearance.SupportsIdentity(parts))throw new ArgumentException("Appearance must contain supported body parts, without equipment.");
            }
            _name=name.Trim(); _appearance=parts.ToArray(); Stage=CreationStage.PersonalityQuestions;
        }
        public void AnswerQuestions(IReadOnlyList<int> answers, bool letFateDecide = false)
        {
            Require(CreationStage.PersonalityQuestions);
            var weights=new Dictionary<PersonalityTrait,float>();
            foreach(PersonalityTrait trait in Enum.GetValues(typeof(PersonalityTrait)))weights[trait]=letFateDecide?(float)_random.NextDouble()*2:0;
            if(!letFateDecide)
            {
                if(answers==null||answers.Count!=_definition.questions.Length)throw new ArgumentException("Answer each authored question.");
                for(int i=0;i<answers.Count;i++)
                {
                    var question=_definition.questions[i];
                    if(answers[i]<0||answers[i]>=question.answers.Length)throw new ArgumentException("Answer index is outside the authored options.");
                    foreach(var weight in question.answers[answers[i]].weights)
                    {if(!Finite(weight.weight))throw new ArgumentException("Non-finite personality weight.");weights[weight.trait]+=weight.weight/answers.Count;}
                }
            }
            _personality.Clear(); foreach(var pair in weights)_personality.Add(pair.Key,pair.Value);
            _answers=letFateDecide?Array.Empty<int>():answers.ToArray(); _fate=letFateDecide;
            Stage=CreationStage.StartingSkills;
        }
        public IReadOnlyList<SkillDefinition> GetBasicCandidates()
        {
            Require(CreationStage.StartingSkills);
            _candidates ??= Draw(_definition.basicSkills,Math.Min(_definition.basicSkills.Length,Math.Max(_definition.startingSkillCount,_definition.choiceCandidateCount)),false);
            return Array.AsReadOnly(_candidates);
        }
        public void DetermineStartingSkills(StartingSkillMode mode, IReadOnlyList<SkillDefinition> choices = null)
        {
            Require(CreationStage.StartingSkills);
            if(mode==StartingSkillMode.Choice)
            {
                var candidates=GetBasicCandidates();
                if(choices==null||choices.Count!=_definition.startingSkillCount||choices.Distinct().Count()!=choices.Count||choices.Any(s=>!candidates.Contains(s)))
                    throw new ArgumentException("Choose the requested number of distinct offered Basic skills.");
                _basics=choices.ToArray();
            }
            else if(mode==StartingSkillMode.Guided||mode==StartingSkillMode.Random)
                _basics=Draw(_definition.basicSkills,_definition.startingSkillCount,mode==StartingSkillMode.Random);
            else throw new ArgumentOutOfRangeException(nameof(mode));
            _mode=mode; Stage=CreationStage.UniqueAwakening;
        }
        public SkillDefinition AwakenUnique()
        {
            Require(CreationStage.UniqueAwakening);
            _unique=Draw(_definition.uniqueSkills,1,false)[0]; Stage=CreationStage.CharacterSummary; return _unique;
        }
        public PlayerCreationProfile ConfirmSummary()
        {
            Require(CreationStage.CharacterSummary);
            var profile=new PlayerCreationProfile(_name,_race,_subRace,_appearance,_answers,_personality,_basics,_unique,_resonance,_seed,_fate,_mode);
            Stage=CreationStage.EnterAethel; return profile;
        }
        private SkillDefinition[] Draw(StartingSkillCandidate[] source, int count, bool surprise)
        {
            var remaining=source.ToList(); var selected=new SkillDefinition[count];
            for(int draw=0;draw<count;draw++)
            {
                var weights=remaining.Select(c=>Weight(c,surprise)).ToArray();
                double roll=_random.NextDouble()*weights.Sum(); int index=weights.Length-1;
                for(int i=0;i<weights.Length;i++){roll-=weights[i];if(roll<0){index=i;break;}}
                selected[draw]=remaining[index].skill; remaining.RemoveAt(index);
            }
            return selected;
        }
        private double Weight(StartingSkillCandidate candidate, bool surprise)
        {
            double value=candidate.baseWeight;
            if(!surprise)foreach(var weight in candidate.personality)value+=_personality[weight.trait]*weight.weight;
            foreach(var affinity in candidate.races)
                if(affinity.race==_race&&(string.IsNullOrEmpty(affinity.subRaceId)||affinity.subRaceId==_subRace))value*=affinity.multiplier;
            foreach(var hint in candidate.resonance)if(_resonance.TryGetValue(hint.domainId,out float amount))value+=hint.weight*amount;
            // A preference changes probability; it never closes off an otherwise compatible skill.
            return Math.Max(.01,Math.Min(1000000,value));
        }
    }
}
