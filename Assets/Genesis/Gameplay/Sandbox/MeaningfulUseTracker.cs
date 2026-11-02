using System.Collections.Generic;

namespace Genesis.Sandbox
{
    /// <summary>Novel target/context combinations qualify once per actor in this test session.</summary>
    public sealed class MeaningfulUseTracker
    {
        private readonly HashSet<(string skill, string target, int context)> _uses = new();
        public (string skill, string target, int context)[] Capture() => new List<(string skill,string target,int context)>(_uses).ToArray();
        public void Restore(IEnumerable<(string skill,string target,int context)> values)
        { _uses.Clear(); foreach (var value in values) _uses.Add(value); }
        public bool TryRecord(string skill, string target, int context)
        {
            if (string.IsNullOrWhiteSpace(skill) || string.IsNullOrWhiteSpace(target)) return false;
            return _uses.Add((skill, target, context));
        }
    }
}
