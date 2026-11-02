using System;
using System.Collections.Generic;
using UnityEngine;

namespace Genesis.Character
{
    /// <summary>Composes identity and equipment visibility without modifying shared meshes or materials.</summary>
    public sealed class ModularAppearance : MonoBehaviour
    {
        [Serializable] public sealed class IdentityGroup
        {
            public string id;
            public bool required = true;
            public string[] partIds = Array.Empty<string>();
        }
        [SerializeField] private GameObject[] parts = Array.Empty<GameObject>();
        [SerializeField] private string[] baseParts = Array.Empty<string>();
        [SerializeField] private IdentityGroup[] identityGroups = Array.Empty<IdentityGroup>();
        private readonly Dictionary<string, GameObject> _parts = new();
        private readonly Dictionary<string, Layer> _layers = new();
        private readonly HashSet<string> _visible = new();
        private string[] _identity;
        public string[] CaptureIdentity() { Cache(); return (string[])_identity.Clone(); }
        private sealed class Layer
        {
            public IReadOnlyList<string> Show, Hide;
        }
        private void Awake() { Cache(); Rebuild(); }
        private void Cache()
        {
            if (_parts.Count > 0) return;
            foreach (var part in parts) if (part != null) _parts.Add(part.name, part);
            _identity = (string[])baseParts.Clone();
        }
        public bool HasPart(string id) { Cache(); return id != null && _parts.ContainsKey(id); }
        public bool IsPartVisible(string id) => HasPart(id) && _parts[id].activeInHierarchy;
        public bool TrySetIdentity(IReadOnlyList<string> ids)
        {
            Cache();
            if (!SupportsIdentity(ids)) return false;
            foreach (var id in ids) if (!HasPart(id)) return false;
            _identity = new string[ids.Count];
            for (int i = 0; i < ids.Count; i++) _identity[i] = ids[i];
            Rebuild(); return true;
        }
        public bool SupportsIdentity(IReadOnlyList<string> ids)
        {
            if (ids == null || ids.Count == 0) return false;
            var selected = new HashSet<string>(ids);
            if (selected.Count != ids.Count) return false;
            if (identityGroups.Length == 0) return selected.SetEquals(baseParts);
            var allowed = new HashSet<string>();
            foreach (var group in identityGroups)
            {
                int count = 0;
                foreach (var id in group.partIds) { allowed.Add(id); if (selected.Contains(id)) count++; }
                if (count > 1 || group.required && count != 1) return false;
            }
            return selected.IsSubsetOf(allowed);
        }
        public void SetEquipmentLayer(string key, IReadOnlyList<string> show, IReadOnlyList<string> hide)
        {
            Cache();
            if (show == null || show.Count == 0) _layers.Remove(key);
            else _layers[key] = new Layer { Show = show, Hide = hide };
            Rebuild();
        }
        private void Rebuild()
        {
            _visible.Clear();
            foreach (var id in _identity) _visible.Add(id);
            foreach (var layer in _layers.Values)
                if (layer.Hide != null) foreach (var id in layer.Hide) _visible.Remove(id);
            foreach (var layer in _layers.Values) foreach (var id in layer.Show) _visible.Add(id);
            foreach (var pair in _parts) pair.Value.SetActive(_visible.Contains(pair.Key));
        }
    }
}
