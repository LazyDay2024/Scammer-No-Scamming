using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    // Physical presentation of the existing AnalysisSession, GeneLoadout and LessonPage rules.
    // Reading never pauses movement: E advances testimony/files or selects the object under the crosshair.
    public class StationInvestigation : MonoBehaviour
    {
        public Transform stationAnchor;
        public GameObject[] victimPrefabs;
        public GameObject[] interiorPrefabs;
        public float TroublePerWrongAnswer = 10f;
        public AnalysisSession Session => session;
        public bool IsActive => phase != Phase.Closed;
        public string StageName => phase.ToString();
        public WorldInteractable Victim => victim;
        public WorldInteractable AdvanceTarget => advance;
        public WorldInteractable DispatchTarget => dispatch;
        public WorldInteractable CaseFileTarget => caseFile;
        public IReadOnlyList<WorldInteractable> AnswerTargets => answers;
        public IReadOnlyList<WorldInteractable> MapPins => pins;
        public IReadOnlyList<WorldInteractable> GeneBottles => bottles;
        enum Phase { Closed, Testimony, Questions, Feedback, Genes, Lesson }
        Phase phase;
        AnalysisSession session;
        GeneLoadout loadout;
        GeneCatalog catalog;
        Action<AnalysisResult> analysed;
        Action<GeneLoadout> dispatched;
        Action lessonDone;
        readonly List<string> pages = new List<string>();
        int pageIndex;
        Transform root, victimVisual;
        WorldInteractable victim, advance, dispatch, caseFile;
        WorldLabel heading, advanceLabel, quota, fileLabel;
        readonly List<WorldInteractable> clues = new List<WorldInteractable>();
        readonly List<WorldInteractable> answers = new List<WorldInteractable>();
        readonly List<WorldInteractable> pins = new List<WorldInteractable>();
        readonly List<WorldInteractable> bottles = new List<WorldInteractable>();
        readonly List<Renderer> bottleGlass = new List<Renderer>();
        readonly List<Material> materials = new List<Material>();
        Light aura;
        string subtitleTitle, subtitle;
        Transform readingSource;

        void Start() => EnsureBuilt();

        public void BeginCase(CaseDefinition definition, GeneLoadout chosen, Action<AnalysisResult> onAnalysed, Action<GeneLoadout> onDispatched)
        {
            EnsureBuilt();
            EnsureInteractor();
            session = new AnalysisSession(definition, TroublePerWrongAnswer);
            loadout = chosen ?? new GeneLoadout();
            analysed = onAnalysed;
            dispatched = onDispatched;
            phase = Phase.Testimony;
            heading.Text = definition.Title + "\nคุยกับผู้เสียหาย · อ่านเบาะแส · เลือกคำตอบ";
            SetVictim(definition);
            SetPages(definition.VictimStory.Split('\n'));
            victim.Prompt = "คุยกับ" + definition.VictimName;
            victim.ReadingTitle = definition.VictimName + " · " + definition.VictimRole;
            for (int i = 0; i < clues.Count; i++)
            {
                bool exists = i < definition.Clues.Count;
                clues[i].gameObject.SetActive(exists);
                if (!exists) continue;
                clues[i].ReadingTitle = "เบาะแส " + (i + 1);
                clues[i].ReadingBody = definition.Clues[i];
                clues[i].GetComponentInChildren<WorldLabel>().Text = "เบาะแส " + (i + 1) + "\n" + definition.Clues[i];
            }
            subtitle = "เดินเข้าไปคุยกับ" + definition.VictimName + " แล้วตรวจเบาะแสบนบอร์ด";
            subtitleTitle = "รับคดี";
            readingSource = victim.transform;
            Refresh();
        }

        public void ShowLesson(LessonPage page, Action done)
        {
            EnsureBuilt(); EnsureInteractor();
            phase = Phase.Lesson;
            lessonDone = done;
            var content = new List<string> { page.Title };
            content.AddRange(page.TrapNotes);
            if (page.Spotted.Count > 0) content.Add("คุณจับสัญญาณได้ทัน: " + string.Join(", ", page.Spotted));
            foreach (var item in page.RedFlags) content.Add("สัญญาณเตือน\n" + item);
            foreach (var item in page.HowToRespond) content.Add("ถ้าเจอแบบนี้\n" + item);
            if (!string.IsNullOrEmpty(page.Footnote)) content.Add(page.Footnote);
            content.Add((page.Footer ?? "") + "\n" + string.Join(" · ", page.Hotlines));
            SetPages(content);
            fileLabel.Text = "แฟ้มคดีปิดแล้ว\n" + page.Title;
            caseFile.ReadingTitle = "บทเรียนจากคดีนี้";
            caseFile.ReadingBody = "เดินมาที่โต๊ะ แล้วกด E เพื่อเปิดแฟ้ม";
            heading.Text = "ปิดคดีแล้ว\nอ่านแฟ้มบนโต๊ะก่อนรับคดีต่อไป";
            subtitle = null;
            Refresh();
        }

        public void Close()
        {
            phase = Phase.Closed; subtitle = null;
            if (root != null) Refresh();
        }

        public void Reset() { Close(); session = null; analysed = null; dispatched = null; lessonDone = null; }

        static void EnsureInteractor()
        {
            var player = PlayerLocator.Player;
            if (player != null && player.GetComponent<WorldInteractor>() == null) player.gameObject.AddComponent<WorldInteractor>();
        }

        void Testimony()
        {
            if (phase != Phase.Testimony) return;
            if (pageIndex < pages.Count)
            {
                Say(victim.transform, victim.ReadingTitle, pages[pageIndex++]);
                victim.Prompt = pageIndex < pages.Count ? "ฟังต่อ" : "รับคำให้การ แล้วเริ่มวิเคราะห์ที่บอร์ด";
                return;
            }
            phase = Phase.Questions;
            Say(advance.transform, "วิเคราะห์คดี", "อ่านเบาะแส แล้วเล็งป้ายคำตอบที่คุณเลือกและกด E");
            Refresh();
        }

        void Choose(int index)
        {
            if (phase != Phase.Questions || session == null || session.IsComplete) return;
            var question = session.CurrentQuestion;
            bool correct = session.Answer(index);
            string answer = correct ? "ถูกต้อง" : "คำตอบที่ถูก: " + question.Options[question.CorrectIndex].Text;
            Say(advance.transform, correct ? "ต่อเบาะแสได้แล้ว" : "ทบทวนเบาะแส", answer + "\n" + question.Explanation + (correct ? "" : "\nTrouble +" + TroublePerWrongAnswer.ToString("0")));
            phase = Phase.Feedback;
            Refresh();
        }

        void Advance()
        {
            if (phase != Phase.Feedback) return;
            if (!session.IsComplete) { phase = Phase.Questions; subtitle = null; Refresh(); return; }
            phase = Phase.Genes;
            var result = session.Result;
            heading.Text = "วิเคราะห์เสร็จ · ถูก " + result.CorrectCount + "/" + session.Case.Questions.Count + "\nเลือกยีนที่ชั้นขวดหน้าสถานี แล้วรับวิทยุออกปฏิบัติการ";
            Say(dispatch.transform, "เตรียมม้าออกปฏิบัติการ", result.IsUnlocked(IntelKind.HideoutLocation)
                ? "พบตำแหน่งรังแล้ว เลือกยีนที่ชั้นขวด ก่อนออกตามลำแสงสีทอง"
                : "ยังไม่รู้ตำแหน่งรัง รู้เพียงว่าอยู่แถว " + session.Case.ZoneHint + " เลือกยีนก่อนออกตามหา");
            Refresh();
            analysed?.Invoke(result);
        }

        void ToggleGene(int index)
        {
            if (phase != Phase.Genes) return;
            var gene = catalog.Genes[index];
            bool selected = loadout.Toggle(gene);
            Say(bottles[index].transform, (selected ? "ฉีดยีน: " : "ถอดยีน: ") + gene.DisplayName, gene.Description);
            UpdateGenes();
        }

        void Dispatch()
        {
            if (phase != Phase.Genes) return;
            var chosen = loadout;
            Close();
            dispatched?.Invoke(chosen);
        }

        void ReadFile()
        {
            if (phase != Phase.Lesson) return;
            if (pageIndex < pages.Count)
            {
                Say(caseFile.transform, "แฟ้มคดี · " + (pageIndex + 1) + "/" + pages.Count, pages[pageIndex++]);
                caseFile.Prompt = pageIndex < pages.Count ? "อ่านหน้าต่อไป" : "เก็บแฟ้ม แล้วรับคดีต่อไป";
                caseFile.ReadingBody = subtitle;
                return;
            }
            var done = lessonDone;
            Close(); done?.Invoke();
        }

        void Refresh()
        {
            victim.Available = phase == Phase.Testimony;
            advance.Available = phase == Phase.Feedback;
            advance.gameObject.SetActive(phase == Phase.Feedback);
            advanceLabel.Text = session != null && session.IsComplete ? "สรุปคดี\nE · เตรียมม้า" : "รับทราบ\nE · คำถามต่อไป";
            for (int i = 0; i < clues.Count; i++) clues[i].Available = phase == Phase.Testimony || phase == Phase.Questions || phase == Phase.Feedback;
            var q = phase == Phase.Questions ? session.CurrentQuestion : null;
            bool map = q != null && q.Reward == IntelKind.HideoutLocation;
            if (q != null) heading.Text = "คำถาม " + (session.Current + 1) + "/" + session.Case.Questions.Count + "\n" + q.Prompt + (map ? "\nเลือกหมุดบนโต๊ะแผนที่" : "");
            for (int i = 0; i < answers.Count; i++)
            {
                bool active = q != null && i < q.Options.Count;
                answers[i].gameObject.SetActive(active && !map);
                pins[i].gameObject.SetActive(active && map);
                if (!active) continue;
                foreach (var target in new[] { answers[i], pins[i] })
                {
                    target.ReadingTitle = q.Prompt;
                    target.ReadingBody = q.Options[i].Text;
                    target.GetComponentInChildren<WorldLabel>().Text = q.Options[i].Text;
                }
                if (map) pins[i].transform.localPosition = MapPosition(q.Options[i].Text, i);
            }
            foreach (var bottle in bottles) bottle.Available = phase == Phase.Genes;
            dispatch.Available = phase == Phase.Genes;
            caseFile.Available = phase == Phase.Lesson;
            caseFile.gameObject.SetActive(phase == Phase.Lesson);
            if (phase == Phase.Genes) UpdateGenes();
        }

        // A pin's placement is presentation only. AnalysisSession remains the only answer authority.
        public static Vector3 MapPosition(string option, int fallback)
        {
            float radius = option.Contains("ตึกสูง") ? 1.35f : option.Contains("ตึกแถว") ? .95f : .55f;
            Vector3 direction = option.Contains("เหนือ") ? Vector3.forward : option.Contains("ใต้") ? Vector3.back
                : option.Contains("ตะวันตก") ? Vector3.left : option.Contains("ตะวันออก") ? Vector3.right : new Vector3(fallback - 1, 0f, 0f);
            return new Vector3(5f, 1.48f, 0f) + direction * radius;
        }

        void UpdateGenes()
        {
            quota.Text = "ยีนม้า " + loadout.Count + "/" + loadout.MaxSlots + "\nฟรี " + loadout.FreeSlots + " ขวด · Trouble +" + loadout.OverBudgetTrouble.ToString("0");
            Color mix = Color.black;
            for (int i = 0; i < bottles.Count; i++)
            {
                bool selected = loadout.Contains(catalog.Genes[i]);
                bottles[i].Prompt = (selected ? "ถอดยีน " : "ฉีดยีน ") + catalog.Genes[i].DisplayName;
                bottleGlass[i].sharedMaterial.SetColor("_BaseColor", GeneColor(i) * (selected ? 1.6f : .7f));
                if (selected) mix += GeneColor(i);
            }
            if (aura == null && HorseController.All.Count > 0)
            {
                var go = new GameObject("GeneAura"); go.transform.SetParent(HorseController.All[0].transform, false);
                go.transform.localPosition = Vector3.up * 1.7f; aura = go.AddComponent<Light>(); aura.type = LightType.Point; aura.range = 5f;
            }
            if (aura != null) { aura.color = loadout.Count > 0 ? mix / loadout.Count : Color.white; aura.intensity = loadout.Count > 0 ? 4f : 0f; }
        }

        void SetPages(IEnumerable<string> source)
        {
            pages.Clear(); pageIndex = 0;
            foreach (string paragraph in source)
            {
                if (string.IsNullOrWhiteSpace(paragraph)) continue;
                // Keep testimony and case-file pages within the reading panel, without changing content.
                // Cut at a space (or at least never between a Thai consonant and its marks).
                int start = 0;
                while (start < paragraph.Length)
                {
                    int end = Math.Min(paragraph.Length, start + 190);
                    if (end < paragraph.Length)
                    {
                        int cut = paragraph.LastIndexOf(' ', end - 1, end - start - 1);
                        if (cut <= start + 60) { cut = end; while (cut > start + 1 && !WorldText.CanBreakBefore(paragraph, cut)) cut--; }
                        end = cut;
                    }
                    pages.Add(paragraph.Substring(start, end - start).Trim());
                    start = end;
                }
            }
        }

        void Say(Transform source, string title, string body) { readingSource = source; subtitleTitle = title; subtitle = body; }

        void OnGUI()
        {
            if (phase == Phase.Closed || GameScreen.AnyOpen || string.IsNullOrEmpty(subtitle) || readingSource == null) return;
            var player = PlayerLocator.Player;
            if (player == null || Vector3.Distance(player.transform.position, readingSource.position) > 8f) return;
            ThaiGui.BeginCanvas();
            var panel = new Rect(280f, 735f, 1360f, 185f);
            ThaiGui.Panel(panel, .94f);
            GUI.Label(new Rect(panel.x + 28f, panel.y + 14f, panel.width - 56f, 48f), subtitleTitle, ThaiGui.Text(27, ThaiGui.Gold, FontStyle.Bold));
            GUI.Label(new Rect(panel.x + 28f, panel.y + 62f, panel.width - 56f, 115f), subtitle, ThaiGui.Text(28, ThaiGui.Bone));
            ThaiGui.EndCanvas();
        }

        void EnsureBuilt()
        {
            if (root != null) return;
            catalog = GeneCatalog.CreateDefault();
            root = new GameObject("StationInvestigationWorld").transform;
            if (stationAnchor == null)
            {
                var station = GameObject.Find("PoliceStation");
                if (station != null) stationAnchor = station.transform;
            }
            root.SetParent(stationAnchor != null ? stationAnchor : transform, false);
            var wood = Mat(new Color(.2f, .12f, .065f));
            var paper = Mat(new Color(.78f, .7f, .51f));
            var metal = Mat(new Color(.07f, .13f, .14f));
            Box("ClueBoard", new Vector3(-5f, 2.1f, 5.85f), new Vector3(6.4f, 3.3f, .18f), wood);
            Box("AnswerBoard", new Vector3(3f, 2.1f, 5.85f), new Vector3(8.5f, 3.3f, .18f), wood);
            heading = Label(root, "สถานีตำรวจ · รับคดี", new Vector3(0f, 4.3f, 5.6f), new Vector2(11f, 1.2f), .1f, ThaiGui.Gold);
            for (int i = 0; i < 4; i++)
            {
                var card = Interactive("Clue_" + i, new Vector3(-6.6f + i % 2 * 3.2f, 2.9f - i / 2 * 1.4f, 5.67f), new Vector3(2.95f, 1.2f, .12f), paper, "อ่านเบาะแส", () => { });
                Label(card.transform, "เบาะแส", new Vector3(0f, 0f, -.09f), new Vector2(2.7f, 1.05f), .06f, ThaiGui.Night);
                clues.Add(card);
            }
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var answer = Interactive("Answer_" + i, new Vector3(.1f + i * 2.8f, 2.1f, 5.65f), new Vector3(2.6f, 2.5f, .18f), paper, "เลือกคำตอบนี้", () => Choose(index));
                Label(answer.transform, "คำตอบ", new Vector3(0f, 0f, -.13f), new Vector2(2.3f, 2.2f), .075f, ThaiGui.Night);
                answers.Add(answer);
                var pin = Interactive("MapPin_" + i, MapPosition("", i), new Vector3(.22f, .55f, .22f), Mat(GeneColor(i)), "ปักหมุดที่นี่", () => Choose(index), PrimitiveType.Cylinder);
                Label(pin.transform, "ตำแหน่ง", new Vector3(0f, .55f, 0f), new Vector2(1.05f, .42f), .035f, ThaiGui.Gold);
                pins.Add(pin);
            }
            advance = Interactive("AnalysisAcknowledgement", new Vector3(3f, 1.35f, 3.8f), new Vector3(2.6f, .85f, .3f), metal, "อ่านสรุป แล้วไปต่อ", Advance);
            advanceLabel = Label(advance.transform, "รับทราบ", new Vector3(0f, 0f, -.2f), new Vector2(2.4f, .75f), .09f, ThaiGui.Gold);
            victim = Interactive("Victim", new Vector3(-5f, 1f, -1.5f), new Vector3(.8f, 1.8f, .8f), metal, "คุยกับผู้เสียหาย", Testimony);
            Table("MapTable", new Vector3(5f, 0f, 0f), new Vector3(3.6f, .18f, 3.6f), wood);
            Box("MapSurface", new Vector3(5f, 1.28f, 0f), new Vector3(3.45f, .04f, 3.45f), Mat(new Color(.3f, .36f, .27f)));
            for (int i = 0; i < 24; i++)
            {
                float angle = i * Mathf.PI / 12f;
                Box("MapBuilding", new Vector3(5f + Mathf.Sin(angle) * 1.2f, 1.37f, Mathf.Cos(angle) * 1.2f), new Vector3(.14f, .15f + i % 3 * .06f, .14f), paper, false);
            }
            Label(root, "แผนที่เมือง · เหนือ ↑", new Vector3(5f, 1.75f, 1.95f), new Vector2(3.2f, .35f), .06f, ThaiGui.Gold);
            Table("CaseFileTable", new Vector3(-4f, 0f, -4f), new Vector3(3f, .18f, 1.5f), wood);
            caseFile = Interactive("SealedCaseFile", new Vector3(-4f, 1.38f, -4f), new Vector3(1.4f, .14f, .8f), paper, "เปิดแฟ้มคดี", ReadFile);
            fileLabel = Label(caseFile.transform, "แฟ้มคดี", new Vector3(0f, .45f, -.1f), new Vector2(2.6f, .7f), .07f, ThaiGui.Gold);
            Box("GeneShelf", new Vector3(7f, 1.05f, -12f), new Vector3(4.7f, .15f, 1f), wood);
            foreach (float x in new[] { 4.8f, 9.2f }) Box("ShelfLeg", new Vector3(x, .5f, -12f), new Vector3(.15f, 1f, .6f), wood);
            quota = Label(root, "ยีนม้า · ฟรี 2 ขวด", new Vector3(7f, 2.6f, -11.7f), new Vector2(4.4f, .9f), .09f, ThaiGui.Gold);
            for (int i = 0; i < catalog.Genes.Count; i++)
            {
                int index = i;
                var bottle = Interactive("GeneBottle_" + catalog.Genes[i].GeneId, new Vector3(5.4f + i * 1.05f, 1.45f, -12f), new Vector3(.36f, .65f, .36f), Mat(GeneColor(i)), "ฉีดยีน " + catalog.Genes[i].DisplayName, () => ToggleGene(index), PrimitiveType.Cylinder);
                Box("BottleStopper", bottle.transform.localPosition + Vector3.up * .4f, new Vector3(.16f, .16f, .16f), wood, false);
                Label(bottle.transform, catalog.Genes[i].DisplayName, new Vector3(0f, -.55f, -.32f), new Vector2(.95f, .28f), .05f, ThaiGui.Bone);
                bottle.ReadingTitle = catalog.Genes[i].DisplayName; bottle.ReadingBody = catalog.Genes[i].Description;
                bottles.Add(bottle); bottleGlass.Add(bottle.transform.Find("Geometry").GetComponent<Renderer>());
            }
            Table("DispatchTable", new Vector3(7f, 0f, -15f), new Vector3(2f, .18f, 1f), wood);
            dispatch = Interactive("DispatchRadio", new Vector3(7f, 1.55f, -15f), new Vector3(.65f, .7f, .35f), metal, "รับวิทยุ แล้วออกปฏิบัติการ", Dispatch);
            Label(dispatch.transform, "ออกปฏิบัติการ\nE · รับวิทยุ", new Vector3(0f, .75f, -.3f), new Vector2(2.4f, .8f), .09f, ThaiGui.Gold);
            Box("RadioAntenna", new Vector3(7.23f, 2.25f, -15f), new Vector3(.035f, .7f, .035f), metal, false);
            // The station roof blocks the low sun; warm lamps keep the boards readable.
            foreach (var lamp in new[] { new Vector3(-4f, 3.9f, 2.5f), new Vector3(3.5f, 3.9f, 2.5f), new Vector3(-1f, 3.9f, -3f) })
            {
                var light = new GameObject("StationLamp").AddComponent<Light>();
                light.transform.SetParent(root, false); light.transform.localPosition = lamp;
                light.type = LightType.Point; light.color = new Color(1f, .8f, .55f); light.intensity = 4f; light.range = 11f;
            }
            if (interiorPrefabs != null)
                for (int i = 0; i < interiorPrefabs.Length; i++)
                    if (interiorPrefabs[i] != null) FitModel(interiorPrefabs[i], new Vector3(-10f + i * 2.8f, 0f, 5.5f), 1.8f);
            Refresh();
        }

        void SetVictim(CaseDefinition definition)
        {
            if (victimVisual != null) Destroy(victimVisual.gameObject);
            victim.transform.Find("Geometry").GetComponent<Renderer>().enabled = true;
            if (victimPrefabs == null || victimPrefabs.Length == 0) return;
            int index = definition.IsBoss ? 3 : definition.CaseId.Contains("02") ? 1 : definition.CaseId.Contains("03") ? 2 : 0;
            var prefab = victimPrefabs[Mathf.Clamp(index, 0, victimPrefabs.Length - 1)];
            if (prefab == null) return;
            victimVisual = FitModel(prefab, new Vector3(-5f, 0f, -1.5f), 1.8f);
            victimVisual.localRotation = Quaternion.Euler(0f, 180f, 0f);
            victim.transform.Find("Geometry").GetComponent<Renderer>().enabled = false;
        }

        Transform FitModel(GameObject prefab, Vector3 position, float height)
        {
            var clone = Instantiate(prefab, root);
            clone.transform.localPosition = Vector3.zero;
            var renderers = clone.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                float scale = height / Mathf.Max(.01f, bounds.size.y);
                clone.transform.localScale *= scale;
                clone.transform.localPosition = position - Vector3.up * (bounds.min.y - root.position.y) * scale;
            }
            else clone.transform.localPosition = position;
            foreach (var collider in clone.GetComponentsInChildren<Collider>()) collider.enabled = false;
            return clone.transform;
        }

        void Table(string name, Vector3 p, Vector3 size, Material material)
        {
            Box(name, p + Vector3.up * 1.15f, size, material);
            for (int i = 0; i < 4; i++) Box(name + "Leg", p + new Vector3((i % 2 == 0 ? -.4f : .4f) * size.x, .55f, (i / 2 == 0 ? -.4f : .4f) * size.z), new Vector3(.15f, 1.1f, .15f), material);
        }

        GameObject Box(string name, Vector3 p, Vector3 size, Material material, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(root, false);
            go.transform.localPosition = p; go.transform.localScale = size; go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) go.GetComponent<Collider>().enabled = false;
            return go;
        }

        WorldInteractable Interactive(string name, Vector3 p, Vector3 size, Material material, string prompt, Action action, PrimitiveType shape = PrimitiveType.Cube)
        {
            var go = Box(name, p, size, material); var target = go.AddComponent<WorldInteractable>();
            // Child text must use world units; geometry dimensions live in the collider/mesh child.
            var geometry = new GameObject("Geometry"); geometry.transform.SetParent(go.transform, false);
            geometry.transform.localScale = shape == PrimitiveType.Cylinder ? new Vector3(size.x, size.y * .5f, size.z) : size;
            geometry.AddComponent<MeshFilter>().sharedMesh = shape == PrimitiveType.Cube ? go.GetComponent<MeshFilter>().sharedMesh : PrimitiveMesh(shape);
            geometry.AddComponent<MeshRenderer>().sharedMaterial = material;
            go.GetComponent<Renderer>().enabled = false;
            go.transform.localScale = Vector3.one;
            ((BoxCollider)go.GetComponent<Collider>()).size = size;
            target.Prompt = prompt; target.OnInteract = action;
            return target;
        }

        WorldLabel Label(Transform parent, string text, Vector3 p, Vector2 area, float maxSize, Color color)
        {
            var go = new GameObject("Label"); go.transform.SetParent(parent, false); go.transform.localPosition = p;
            var mesh = go.AddComponent<TextMesh>(); mesh.font = ThaiGui.Font; mesh.fontSize = 48;
            mesh.anchor = TextAnchor.MiddleCenter; mesh.alignment = TextAlignment.Center; mesh.color = color;
            var label = go.AddComponent<WorldLabel>();
            label.width = area.x; label.height = area.y; label.maxSize = maxSize;
            label.Text = text;
            return label;
        }

        static readonly Dictionary<PrimitiveType, Mesh> primitiveMeshes = new Dictionary<PrimitiveType, Mesh>();
        static Mesh PrimitiveMesh(PrimitiveType shape)
        {
            if (primitiveMeshes.TryGetValue(shape, out var mesh) && mesh != null) return mesh;
            var temp = GameObject.CreatePrimitive(shape);
            mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(temp);
            return primitiveMeshes[shape] = mesh;
        }

        Material Mat(Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")); material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", .2f); materials.Add(material); return material;
        }

        static Color GeneColor(int i) => i == 0 ? ThaiGui.Orange : i == 1 ? new Color(.3f, .55f, 1f) : i == 2 ? ThaiGui.Gold : ThaiGui.Teal;
        void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
            if (aura != null) Destroy(aura.gameObject);
            foreach (var material in materials) if (material != null) Destroy(material);
            if (catalog != null) { foreach (var gene in catalog.Genes) if (gene != null) Destroy(gene); Destroy(catalog); }
        }
    }
}
