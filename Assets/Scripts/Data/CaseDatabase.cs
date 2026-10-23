using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    // All cases plus shared text, loaded from Resources/Content/game_content.json.
    public class CaseDatabase : ScriptableObject
    {
        public const string DefaultResource = "Content/game_content";

        public List<CaseDefinition> Pool = new List<CaseDefinition>();
        public GameText Text = new GameText();

        public IEnumerable<CaseDefinition> Regular => Pool.FindAll(c => !c.IsBoss);
        public CaseDefinition Boss => Pool.Find(c => c.IsBoss);
        public CaseDefinition Find(string caseId) => Pool.Find(c => c.CaseId == caseId);

        // Field names match CaseDefinition, so each entry is copied over with JsonUtility.
        [Serializable]
        class CaseJson
        {
            public string CaseId;
            public string Title;
            public ScamType ScamType;
            public bool IsBoss;
            public string VictimName;
            public string VictimRole;
            public string Portrait;
            public string VictimStory;
            public List<string> Clues;
            public List<AnalysisQuestion> Questions;
            public List<TrapKind> Traps;
            public string HideoutId;
            public string ZoneHint;
            public LessonContent Lesson;
        }

        [Serializable]
        class ContentJson
        {
            public List<CaseJson> Cases = new List<CaseJson>();
            public GameText Text = new GameText();
        }

        public static CaseDatabase FromJson(string json)
        {
            var content = JsonUtility.FromJson<ContentJson>(json);
            var db = CreateInstance<CaseDatabase>();
            db.name = "CaseDatabase";
            db.Text = content.Text ?? new GameText();
            foreach (var entry in content.Cases)
            {
                var def = CreateInstance<CaseDefinition>();
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(entry), def);
                def.name = entry.CaseId;
                db.Pool.Add(def);
            }
            return db;
        }

        public static CaseDatabase LoadDefault()
        {
            var asset = Resources.Load<TextAsset>(DefaultResource);
            if (asset == null) throw new InvalidOperationException($"Missing Resources/{DefaultResource}.json");
            return FromJson(asset.text);
        }

        // Decided: cases are a pool and each run picks some at random. The first pick is the tutorial
        // case when one is marked by id order (CASE_01...), the boss always comes last.
        public List<CaseDefinition> PickRun(int count, int seed)
        {
            var regular = new List<CaseDefinition>(Regular);
            regular.Sort((a, b) => string.CompareOrdinal(a.CaseId, b.CaseId));
            var picked = new List<CaseDefinition>();
            if (regular.Count > 0)
            {
                picked.Add(regular[0]);
                regular.RemoveAt(0);
            }
            var rng = new System.Random(seed);
            while (picked.Count < count && regular.Count > 0)
            {
                int i = rng.Next(regular.Count);
                picked.Add(regular[i]);
                regular.RemoveAt(i);
            }
            if (Boss != null) picked.Add(Boss);
            return picked;
        }
    }
}
