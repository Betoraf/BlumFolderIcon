using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Blumgames.FolderIcon
{
    [FilePath("ProjectSettings/BlumFolderIcon.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class FolderIconSettings : ScriptableSingleton<FolderIconSettings>
    {
        [SerializeField] bool initialized;
        [SerializeField] bool enabled = true;
        [SerializeField] bool automatic = true;
        [SerializeField] List<FolderRule> rules = new List<FolderRule>();
        [SerializeField] List<FolderOverride> overrides = new List<FolderOverride>();
        [NonSerialized] Dictionary<string, FolderStyle> byGuid;
        [NonSerialized] Dictionary<string, FolderStyle> byName;
        [NonSerialized] string lastSavedState;

        public static event Action Changed;
        public bool Enabled => enabled;
        public bool Automatic => automatic;
        public IReadOnlyList<FolderRule> Rules { get { EnsureDefaults(); return rules; } }
        public int OverrideCount => overrides.Count;

        void EnsureDefaults()
        {
            if (initialized) return;
            rules = FolderPresets.CreateRules();
            initialized = true;
        }

        void BuildLookup()
        {
            EnsureDefaults();
            byGuid = new Dictionary<string, FolderStyle>(StringComparer.Ordinal);
            byName = new Dictionary<string, FolderStyle>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in overrides)
                if (item != null && !string.IsNullOrEmpty(item.guid) && item.style != null)
                    byGuid[item.guid] = item.style;
            foreach (var rule in rules)
            {
                if (rule == null || !rule.enabled || rule.style == null) continue;
                foreach (var alias in rule.names.Split(','))
                {
                    var name = alias.Trim();
                    // The first rule wins; duplicate aliases never throw.
                    if (name.Length > 0 && !byName.ContainsKey(name)) byName.Add(name, rule.style);
                }
            }
        }

        public FolderStyle Resolve(string guid, string folderName)
        {
            if (!enabled) return null;
            if (byGuid == null) BuildLookup();
            if (byGuid.TryGetValue(guid ?? "", out var manual)) return manual;
            return automatic && byName.TryGetValue(folderName ?? "", out var rule) ? rule : null;
        }

        public bool HasOverride(string guid)
        {
            if (byGuid == null) BuildLookup();
            return byGuid.ContainsKey(guid);
        }

        public void SetOptions(bool enable, bool useAutomatic) => Edit("Change folder icon settings", () =>
        { enabled = enable; automatic = useAutomatic; });

        public void SetOverrides(IEnumerable<string> guids, FolderStyle style) => Edit("Customize folder icons", () =>
        {
            foreach (var guid in guids)
            {
                if (string.IsNullOrEmpty(guid)) continue;
                overrides.RemoveAll(item => item.guid == guid);
                overrides.Add(new FolderOverride { guid = guid, style = style.Copy() });
            }
            overrides.Sort((a, b) => string.CompareOrdinal(a.guid, b.guid));
        });

        public void ResetOverrides(IEnumerable<string> guids) => Edit("Reset folder icons", () =>
        {
            var selected = new HashSet<string>(guids);
            overrides.RemoveAll(item => selected.Contains(item.guid));
        });

        public void SetRule(int index, FolderRule value) => Edit("Edit folder rule", () => rules[index] = value);
        public void AddRule() => Edit("Add folder rule", () => rules.Add(new FolderRule { names = "MyFolder" }));
        public void RemoveRule(int index) => Edit("Remove folder rule", () => rules.RemoveAt(index));
        public void RestoreRules() => Edit("Restore automatic folder rules", () => rules = FolderPresets.CreateRules());

        void Edit(string label, Action change)
        {
            EnsureDefaults();
            Undo.RegisterCompleteObjectUndo(this, label);
            change();
            PersistAndNotify();
        }

        internal void InitializeUndoState()
        {
            EnsureDefaults();
            lastSavedState = EditorJsonUtility.ToJson(this);
        }

        internal void AfterUndoRedo()
        {
            if (lastSavedState == EditorJsonUtility.ToJson(this)) return;
            PersistAndNotify();
        }

        void PersistAndNotify()
        {
            byGuid = null;
            byName = null;
            Save(true);
            lastSavedState = EditorJsonUtility.ToJson(this);
            Changed?.Invoke();
            EditorApplication.RepaintProjectWindow();
        }

        internal void InvalidateLookup() { byGuid = null; byName = null; }
    }
}
