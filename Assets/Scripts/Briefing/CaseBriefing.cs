using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ScammerNoScamming
{
    // The start of a case, inside the police station: read the case file, take the map from the desk, pick
    // which hideout to raid. A wrong pick brings scammers into the station (stationRaid); a right one hands an
    // AnalysisResult back to CaseFlowDemo, which goes on to the gene screen and the raid itself.
    public class CaseBriefing : MonoBehaviour
    {
        public CaseFlowDemo flow;
        [Tooltip("Station root; the desk is built at deskLocalPosition in its frame.")]
        public Transform stationAnchor;
        public Vector3 deskLocalPosition = new Vector3(0f, 0f, 1.5f);
        [Tooltip("Scammers who storm the station after a wrong pick. Children of this object's templates are cloned.")]
        public RaidEncounter stationRaid;
        public float wrongPickTrouble = 20f;
        [Tooltip("Looks of the case folder and the folded map on the desk; the interaction boxes stay invisible cubes.")]
        public GameObject caseFileModel, foldedMapModel;

        public enum Phase { Idle, ReadFile, TakeMap, Choose, StationRaid, Done }
        public Phase Current { get; private set; }
        public bool HasMap { get; private set; }
        public int WrongPicks => wrong.Count;

        CaseDefinition definition;
        Action<AnalysisResult> done;
        CaseDocumentScreen document;
        CityMapScreen mapScreen;
        WorldInteractable folder, mapOnDesk;
        GameObject mapVisual;
        readonly HashSet<string> wrong = new HashSet<string>();
        string pickedSiteId;
        PlayerVitals vitals;

        // The current case's door guards, spawned when the raid starts.
        public RaidEncounter DoorGuards
        {
            get
            {
                var entrance = definition != null ? HideoutEntrance.Find(definition.HideoutId) : null;
                return entrance != null ? entrance.guards : null;
            }
        }

        public bool Inside
        {
            get
            {
                var player = PlayerLocator.Player;
                if (player == null) return false;
                foreach (var entrance in HideoutEntrance.All)
                    if (entrance.interiorAnchor != null
                        && (player.transform.position - entrance.interiorAnchor.position).sqrMagnitude < entrance.interiorRadius * entrance.interiorRadius)
                        return true;
                return false;
            }
        }

        void Awake()
        {
            document = gameObject.AddComponent<CaseDocumentScreen>();
            mapScreen = gameObject.AddComponent<CityMapScreen>();
            if (flow == null) flow = GetComponent<CaseFlowDemo>();
        }

        void Start()
        {
            BuildDesk();
            Refresh();
        }

        public void Begin(CaseDefinition caseDefinition, Action<AnalysisResult> onPicked)
        {
            definition = caseDefinition;
            done = onPicked;
            wrong.Clear();
            pickedSiteId = null;
            HasMap = false;
            if (stationRaid != null) stationRaid.ResetRaid();
            foreach (var entrance in HideoutEntrance.All)
                if (entrance.guards != null) entrance.guards.ResetRaid();
            CityMapView.Instance?.Prepare();
            StartCoroutine(SwitchInterior(HideoutEntrance.Find(caseDefinition.HideoutId)));
            Current = Phase.ReadFile;
            Refresh();
        }

        // The hideout interior is its own scene, loaded on top of the city while its case runs and dropped after.
        string loadedScene;
        bool loading;

        public bool InteriorLoading => loading;

        IEnumerator SwitchInterior(HideoutEntrance wanted)
        {
            while (loading) yield return null;
            loading = true;
            string name = wanted != null ? wanted.sceneName : null;
            if (loadedScene != null && loadedScene != name)
            {
                yield return SceneManager.UnloadSceneAsync(loadedScene);
                loadedScene = null;
            }
            if (name != null && loadedScene == null)
            {
                yield return SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
                loadedScene = name;
            }
            if (wanted != null)
            {
                var interior = HideoutInterior.Find(wanted.hideoutId);
                if (interior != null) wanted.Bind(interior);
                else Debug.LogError("Hideout interior scene " + name + " has no HideoutInterior for " + wanted.hideoutId, this);
            }
            loading = false;
        }

        // Back to the menu or a new run: the desk goes quiet.
        public void Stop()
        {
            Current = Phase.Idle;
            HasMap = false;
            if (stationRaid != null) stationRaid.ResetRaid();
            if (loadedScene != null && isActiveAndEnabled) StartCoroutine(SwitchInterior(null));
            Refresh();
        }

        public void OnActionBegan()
        {
            var guards = DoorGuards;
            if (guards != null) guards.BeginRaid();
        }

        // Shown instead of the default raid objective until the player is inside the hideout.
        public string ActionObjective(bool hasEvidence)
        {
            if (hasEvidence || Inside) return null;
            var door = DoorGuards;
            int guards = door != null ? door.RemainingEnemies : 0;
            return guards > 0
                ? "กำจัดยามหน้าประตูรัง · เหลือ " + guards + " คน (กด M ดูแผนที่)"
                : "เดินเข้าประตูรัง (ลงจากม้าก่อน กด M ดูแผนที่)";
        }

        // Called by CaseFlowDemo when the player dies; true if the briefing handled it.
        public bool HandleDeath()
        {
            if (Current != Phase.StationRaid) return false;
            if (stationRaid != null) stationRaid.ResetRaid();
            flow.ShowStory("สถานีถูกบุก", "คุณล้มลงกลางสถานี",
                "สแกมเมอร์ที่บุกเข้ามาถูกพาตัวไปแล้ว กลับมาตั้งหลักที่โต๊ะ แล้วเลือกรังใหม่ให้ถูกกว่าเดิม",
                "ลุกขึ้นสู้ต่อ", () =>
                {
                    flow.RespawnPlayerAtStart();
                    Current = Phase.Choose;
                    Refresh();
                });
            return true;
        }

        void Update()
        {
            if (Current == Phase.StationRaid && stationRaid != null && stationRaid.Cleared)
            {
                Current = Phase.Choose;
                Refresh();
            }
            var keyboard = Keyboard.current;
            if (HasMap && keyboard != null && keyboard[Key.M].wasPressedThisFrame && !GameScreen.AnyOpen && Current != Phase.Idle
                && Current != Phase.StationRaid)
                OpenMap();
        }

        void OpenMap()
        {
            mapScreen.Show(Current == Phase.Choose, wrong, pickedSiteId, Picked);
        }

        IEnumerator PickWhenReady(MapSite site)
        {
            while (loading) yield return null;
            Picked(site);
        }

        void Picked(MapSite site)
        {
            if (Current != Phase.Choose || definition == null) return;
            if (loading) { StartCoroutine(PickWhenReady(site)); return; } // the interior must exist before its beacon is raised
            if (site.siteId == definition.HideoutId)
            {
                pickedSiteId = site.siteId;
                var result = new AnalysisResult { CorrectCount = 1, WrongCount = wrong.Count, TroublePenalty = wrong.Count * wrongPickTrouble };
                result.Unlock(IntelKind.HideoutLocation);
                Current = Phase.Done;
                Refresh();
                done?.Invoke(result);
                return;
            }
            wrong.Add(site.siteId);
            Current = Phase.StationRaid;
            Refresh();
            if (stationRaid == null) { Current = Phase.Choose; return; }
            stationRaid.BeginRaid();
            var player = PlayerLocator.Player;
            foreach (var enemy in stationRaid.Enemies)
                if (enemy != null && player != null) enemy.Alert(player.transform.position);
        }

        string Objective()
        {
            switch (Current)
            {
                case Phase.ReadFile: return "เปิดแฟ้มคดีบนโต๊ะ (กด E) อ่านคำให้การและหลักฐาน";
                case Phase.TakeMap: return "หยิบแผนที่บนโต๊ะ (กด E)";
                case Phase.Choose: return wrong.Count == 0
                    ? "กด M เปิดแผนที่ เลือกรังของแก๊งที่ตรงกับหลักฐาน"
                    : "ปลอดภัยแล้ว กด M เลือกรังใหม่ (ผิดไปแล้ว " + wrong.Count + " ที่)";
                case Phase.StationRaid: return "สแกมเมอร์บุกสถานี! กำจัดให้หมด · เหลือ " + (stationRaid != null ? stationRaid.RemainingEnemies : 0) + " คน";
                default: return null;
            }
        }

        void Refresh()
        {
            if (folder != null) folder.Available = Current != Phase.Idle && Current != Phase.StationRaid;
            if (mapOnDesk != null)
            {
                mapOnDesk.gameObject.SetActive(!HasMap);
                if (mapVisual != null) mapVisual.SetActive(!HasMap);
                mapOnDesk.Available = Current == Phase.TakeMap || Current == Phase.Choose;
            }
        }

        void OnGUI()
        {
            if (Current == Phase.Idle || Current == Phase.Done || GameScreen.AnyOpen || definition == null) return;
            if (vitals == null && PlayerLocator.Player != null) vitals = PlayerLocator.Player.GetComponent<PlayerVitals>();
            GameHud.Draw(definition.Title, Objective(), vitals != null ? vitals.Health.Current : 100f,
                vitals != null ? vitals.Health.max : 100f, flow != null ? flow.Trouble : 0f,
                flow != null ? flow.policeThreshold : 100f, false);
        }

        // Placeholder furniture; the art pass replaces these primitives by moving real models onto the same objects.
        void BuildDesk()
        {
            var parent = stationAnchor != null ? stationAnchor : transform;
            var root = new GameObject("BriefingDesk").transform;
            root.SetParent(parent, false);
            root.localPosition = deskLocalPosition;
            var wood = Mat(new Color(.4f, .25f, .14f));
            Piece(root, "DeskTop", new Vector3(0f, 1f, 0f), new Vector3(2.6f, .12f, 1.2f), wood);
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                    Piece(root, "DeskLeg", new Vector3(sx * 1.15f, .5f, sz * .5f), new Vector3(.12f, 1f, .12f), wood);

            var folderPiece = Piece(root, "CaseFile", new Vector3(-.6f, 1.1f, 0f), new Vector3(.7f, .08f, .5f), Mat(new Color(.55f, .4f, .22f)));
            Dress(root, folderPiece, caseFileModel, -12f);
            folder = folderPiece.gameObject.AddComponent<WorldInteractable>();
            folder.Prompt = "เปิดแฟ้มคดี";
            folder.Range = 4f;
            folder.OnInteract = () =>
            {
                if (definition == null) return;
                document.Show(definition, () =>
                {
                    if (Current == Phase.ReadFile) { Current = Phase.TakeMap; Refresh(); }
                });
            };

            var mapPiece = Piece(root, "FoldedMap", new Vector3(.6f, 1.08f, 0f), new Vector3(.8f, .04f, .6f), Mat(new Color(.78f, .7f, .51f)));
            mapVisual = Dress(root, mapPiece, foldedMapModel, 8f);
            mapOnDesk = mapPiece.gameObject.AddComponent<WorldInteractable>();
            mapOnDesk.Prompt = "หยิบแผนที่";
            mapOnDesk.Range = 4f;
            mapOnDesk.OnInteract = () =>
            {
                HasMap = true;
                if (Current == Phase.TakeMap) Current = Phase.Choose;
                Refresh();
                if (Current == Phase.Choose) OpenMap();
            };
        }

        // Puts a model on the desk top (y 1.06) where an interaction cube stands, and hides the cube's renderer.
        static GameObject Dress(Transform root, Transform piece, GameObject model, float yaw)
        {
            if (model == null) return null;
            piece.GetComponent<Renderer>().enabled = false;
            var visual = Instantiate(model, root);
            foreach (var collider in visual.GetComponentsInChildren<Collider>()) Destroy(collider);
            visual.transform.localPosition = new Vector3(piece.localPosition.x, 1.06f, piece.localPosition.z);
            visual.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return visual;
        }

        static Transform Piece(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go.transform;
        }

        static Material Mat(Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", .2f);
            return material;
        }
    }
}
