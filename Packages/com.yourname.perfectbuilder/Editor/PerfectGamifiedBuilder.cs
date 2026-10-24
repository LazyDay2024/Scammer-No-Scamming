#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PerfectGamifiedBuilder.Editor
{
    /// <summary>
    /// Gamified scene-building toolbox: surface-flush drag snapping, clean 90-degree
    /// local rotation (Q/E), and scale-safe grouping. Open via Tools/Perfect Gamified Builder.
    /// </summary>
    public class PerfectGamifiedBuilder : EditorWindow
    {
        private bool toolActive = false;
        private float rotationStep = 90f;
        private string groupName = "Perfect_Group";

        private bool gridSnapEnabled;
        private Vector3 gridSize = Vector3.one;
        private bool gridSnapY;
        private float snapOffset;

        // --- drag state (shared by handle-drag AND grab-mode) ---
        private int dragControlId = -1;
        private Transform[] dragSelection;
        private Vector3[] dragOffsets;
        private Bounds dragPrimaryBounds;
        private Vector3 dragPivotOffset;
        private float dragFallbackDistance;

        // --- grab mode (G key: pick up without clicking the handle) ---
        private bool grabModeActive;
        private Vector3[] grabOriginalPositions;

        [System.Serializable]
        private class HistoryEntry
        {
            public string label;
            public int undoGroup;
            public long timestampTicks;
        }

        [SerializeField] private List<HistoryEntry> historyEntries = new List<HistoryEntry>();
        [SerializeField] private List<GameObject> trackedObjects = new List<GameObject>();
        [SerializeField] private List<GameObject> favoriteObjects = new List<GameObject>();
        private bool showHistoryPanel = true;
        private bool showObjectManagerPanel = true;
        private Vector2 mainScroll;

        [MenuItem("Tools/Perfect Gamified Builder")]
        public static void Open()
        {
            PerfectGamifiedBuilder window = GetWindow<PerfectGamifiedBuilder>("Perfect Builder");
            window.minSize = new Vector2(360, 480);
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            mainScroll = EditorGUILayout.BeginScrollView(mainScroll);

            DrawHeader();

            EditorGUILayout.Space(8);
            toolActive = EditorGUILayout.ToggleLeft(
                "⚡ เปิดใช้งานเครื่องมือ (Tool Active) — คีย์ลัด Shift+G",
                toolActive, EditorStyles.boldLabel);

            EditorGUILayout.Space(4);
            gridSnapEnabled = EditorGUILayout.ToggleLeft(
                "🧲 ล็อกกริด (Grid Snap) — คีย์ลัด Alt+G", gridSnapEnabled, EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(!gridSnapEnabled))
            {
                EditorGUI.indentLevel++;
                gridSize = EditorGUILayout.Vector3Field("ขนาดช่องกริด (Grid Cell Size)", gridSize);
                gridSize.x = Mathf.Max(0.01f, gridSize.x);
                gridSize.y = Mathf.Max(0.01f, gridSize.y);
                gridSize.z = Mathf.Max(0.01f, gridSize.z);
                gridSnapY = EditorGUILayout.Toggle("ล็อกกริดแกน Y ด้วย", gridSnapY);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4);
            snapOffset = EditorGUILayout.FloatField("📏 ระยะห่างจากผิว (Snap Offset)", snapOffset);
            EditorGUILayout.HelpBox(
                "0 = แนบสนิทกับผิวพอดี (ค่าเริ่มต้น) | มากกว่า 0 = เว้นระยะห่างจากผิว | น้อยกว่า 0 = ฝังจมเข้าไปในผิวเล็กน้อย",
                MessageType.None);

            EditorGUILayout.Space(4);
            rotationStep = EditorGUILayout.FloatField("🔄 มุมหมุนต่อครั้ง (Rotation Step)", rotationStep);
            rotationStep = Mathf.Max(1f, rotationStep);

            groupName = EditorGUILayout.TextField("📦 ชื่อกลุ่ม/วัตถุ (Name)", groupName);

            EditorGUILayout.Space(8);
            EditorGUI.BeginDisabledGroup(Selection.gameObjects.Length == 0);
            if (GUILayout.Button("📦 จัดกลุ่มแบบปลอดภัย (Perfect Grouping)", GUILayout.Height(36)))
            {
                GroupSelectedSafely();
            }

            EditorGUILayout.Space(4);
            Color previousColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.35f, 0.3f);
            if (GUILayout.Button("🔥 หลอมรวมถาวร (Merge To Single Mesh)", GUILayout.Height(36)))
            {
                MergeSelectedToSingleMesh();
            }
            GUI.backgroundColor = previousColor;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "คีย์ลัด (ต้องเมาส์อยู่ใน Scene View):\n" +
                "• Shift+G  = เปิด/ปิด Tool Active ทั้งระบบ (ใช้ได้แม้ตอนนี้ปิดอยู่)\n" +
                "• Alt+G    = เปิด/ปิด Grid Snap\n" +
                "• G        = 🖐 หยิบวัตถุที่เลือกไว้ขึ้นมา (Grab) แล้วตามเมาส์ทันที ไม่ต้องจิ้มแฮนเดิลให้แม่น\n" +
                "             กด G ซ้ำ หรือคลิกซ้าย = วางยืนยัน, คลิกขวา/Esc = ยกเลิกกลับตำแหน่งเดิม\n" +
                "• Q / E    = หมุนรอบแกน Y (Local) ทีละ Rotation Step องศา\n\n" +
                "วิธีใช้:\n" +
                "1) เลือกวัตถุใดๆ ในฉาก (เลือกหลายชิ้นพร้อมกันได้)\n" +
                "2) กด G เพื่อหยิบขึ้นมาทันที (แนะนำ ง่ายกว่าจิ้มทรงกลมสีเหลือง) หรือจะลากที่ทรงกลมสีเหลืองกลางวัตถุก็ได้\n" +
                "3) ถ้า Grid Snap ปิดอยู่ = แปะติดผิวของวัตถุอื่นแบบสนิท (พื้น/ผนัง/ของประกอบฉาก)\n" +
                "4) ถ้า Grid Snap เปิดอยู่ = ตำแหน่งสุดท้ายจะถูกปัดเข้าช่องกริดตาม Grid Cell Size ทับอีกชั้น\n" +
                "5) 'Perfect Grouping' = จัดกลุ่มแบบยังแก้ไขทีละชิ้นได้ (ใช้ตอน prototype ยังไม่นิ่ง)\n" +
                "6) 'Merge To Single Mesh' = หลอมรวมถาวรเป็น GameObject เดียวจริงๆ (Script/Collider เฉพาะของแต่ละชิ้นเดิมจะหายไป)",
                MessageType.Info);

            EditorGUILayout.Space(10);
            DrawHistoryPanel();

            EditorGUILayout.Space(10);
            DrawObjectManagerPanel();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            Rect rect = GUILayoutUtility.GetRect(position.width, 44);
            EditorGUI.DrawRect(rect, new Color(0.10f, 0.10f, 0.16f));
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            style.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUI.Label(rect, "🎮 PERFECT GAMIFIED BUILDER 🎮", style);
        }

        // ------------------------------------------------------------------
        // Scene View handling
        // ------------------------------------------------------------------

        private void OnSceneGUI(SceneView sceneView)
        {
            // ตรวจคีย์ลัดเปิด/ปิดเครื่องมือก่อนเสมอ แม้ตอนนี้ toolActive จะเป็น false อยู่ก็ตาม
            HandleMasterToggleHotkey();
            DrawStatusOverlay();

            if (!toolActive)
            {
                return;
            }

            HandleGrabModeHotkey();
            HandleGridToggleHotkey();
            HandleRotationHotkeys();

            if (grabModeActive)
            {
                HandleGrabModeUpdate();
            }
            else
            {
                HandleDragSnap();
            }
        }

        private void HandleMasterToggleHotkey()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.G && e.shift)
            {
                toolActive = !toolActive;
                e.Use();
                Repaint();
                SceneView.RepaintAll();
            }
        }

        private void HandleGridToggleHotkey()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.G && e.alt && !e.shift)
            {
                gridSnapEnabled = !gridSnapEnabled;
                e.Use();
                Repaint();
                SceneView.RepaintAll();
            }
        }

        // --- Grab Mode: กด G เพื่อ "หยิบ" วัตถุที่เลือกไว้ทันที ไม่ต้องจิ้มแฮนเดิลให้แม่น ---

        private void HandleGrabModeHotkey()
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown || e.keyCode != KeyCode.G || e.shift || e.alt)
            {
                return;
            }

            if (grabModeActive)
            {
                ConfirmGrabMode();
            }
            else
            {
                StartGrabMode();
            }

            e.Use();
            SceneView.RepaintAll();
        }

        private void StartGrabMode()
        {
            Transform primary = Selection.activeTransform;
            if (primary == null)
            {
                return;
            }

            Bounds bounds = GetWorldBounds(primary);
            dragSelection = Selection.transforms;
            dragPrimaryBounds = bounds;
            dragPivotOffset = primary.position - bounds.center;
            dragFallbackDistance = Vector3.Distance(
                SceneView.currentDrawingSceneView.camera.transform.position, primary.position);

            dragOffsets = new Vector3[dragSelection.Length];
            grabOriginalPositions = new Vector3[dragSelection.Length];
            for (int i = 0; i < dragSelection.Length; i++)
            {
                dragOffsets[i] = dragSelection[i].position - primary.position;
                grabOriginalPositions[i] = dragSelection[i].position;
            }

            Undo.RecordObjects(dragSelection, "Perfect Grab Move");
            grabModeActive = true;
        }

        private void ConfirmGrabMode()
        {
            RecordHistory($"🖐 หยิบย้าย (Grab) {dragSelection.Length} ชิ้น");
            TrackObjectsFromTransforms(dragSelection);
            grabModeActive = false;
            dragSelection = null;
            grabOriginalPositions = null;
        }

        private void CancelGrabMode()
        {
            if (dragSelection != null && grabOriginalPositions != null)
            {
                for (int i = 0; i < dragSelection.Length; i++)
                {
                    if (dragSelection[i] != null)
                    {
                        dragSelection[i].position = grabOriginalPositions[i];
                    }
                }
            }

            grabModeActive = false;
            dragSelection = null;
            grabOriginalPositions = null;
        }

        private void HandleGrabModeUpdate()
        {
            Event e = Event.current;

            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            {
                Vector3 snapped = ComputeSnappedPosition(e.mousePosition);
                for (int i = 0; i < dragSelection.Length; i++)
                {
                    if (dragSelection[i] != null)
                    {
                        dragSelection[i].position = snapped + dragOffsets[i];
                    }
                }
                e.Use();
                SceneView.RepaintAll();
            }
            else if (e.type == EventType.MouseDown && e.button == 0)
            {
                ConfirmGrabMode();
                e.Use();
                SceneView.RepaintAll();
            }
            else if (e.type == EventType.MouseDown && e.button == 1)
            {
                CancelGrabMode();
                e.Use();
                SceneView.RepaintAll();
            }
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                CancelGrabMode();
                e.Use();
                SceneView.RepaintAll();
            }
        }

        private void DrawStatusOverlay()
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            Handles.BeginGUI();
            string grabPart = grabModeActive ? "   🖐 GRABBING (คลิกซ้าย=วาง, ขวา/Esc=ยกเลิก)" : "";
            string status = $"🎮 Tool: {(toolActive ? "ON" : "OFF")}   🧲 Grid: {(gridSnapEnabled ? $"ON {gridSize:F1}" : "OFF")}   🔄 Step: {rotationStep}°{grabPart}";
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
            style.normal.textColor = toolActive ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.5f, 0.4f);
            Rect rect = new Rect(8, 8, 420, 20);
            GUI.Label(rect, status, style);
            Handles.EndGUI();
        }

        private void HandleRotationHotkeys()
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown || (e.keyCode != KeyCode.Q && e.keyCode != KeyCode.E))
            {
                return;
            }

            Transform[] selected = Selection.transforms;
            if (selected.Length == 0)
            {
                return;
            }

            float direction = e.keyCode == KeyCode.E ? 1f : -1f;
            float step = Mathf.Max(0.01f, rotationStep);

            foreach (Transform t in selected)
            {
                Undo.RecordObject(t, "Perfect Axis Rotation");
                t.Rotate(Vector3.up * step * direction, Space.Self);

                Vector3 euler = t.localEulerAngles;
                euler.y = SnapAngle(euler.y, step);
                t.localEulerAngles = euler;
            }

            RecordHistory($"🔄 หมุน {(direction > 0 ? "E" : "Q")} {selected.Length} ชิ้น");
            TrackObjectsFromTransforms(selected);

            e.Use();
            SceneView.RepaintAll();
        }

        private static float SnapAngle(float angle, float step)
        {
            float snapped = Mathf.Round(angle / step) * step;
            snapped = Mathf.Repeat(snapped, 360f);
            return (float)System.Math.Round(snapped, 3);
        }

        private void HandleDragSnap()
        {
            Transform primary = Selection.activeTransform;
            if (primary == null)
            {
                return;
            }

            Bounds bounds = GetWorldBounds(primary);
            Vector3 handlePos = bounds.center;
            // ขนาดใหญ่ขึ้นกว่าเดิมมาก (0.15 -> 0.35) เพื่อให้จิ้มโดนง่ายขึ้น — แนะนำให้ใช้คีย์ลัด G (Grab) แทนถ้ายังจิ้มยาก
            float handleSize = HandleUtility.GetHandleSize(handlePos) * 0.35f;

            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            Event e = Event.current;

            switch (e.GetTypeForControl(controlId))
            {
                case EventType.Layout:
                    HandleUtility.AddControl(controlId, HandleUtility.DistanceToCircle(handlePos, handleSize));
                    break;

                case EventType.Repaint:
                    Handles.color = GUIUtility.hotControl == controlId
                        ? new Color(1f, 0.6f, 0f)
                        : new Color(1f, 0.85f, 0.2f);
                    Handles.SphereHandleCap(controlId, handlePos, Quaternion.identity, handleSize, EventType.Repaint);
                    if (GUIUtility.hotControl == controlId)
                    {
                        Handles.Label(handlePos + Vector3.up * handleSize * 2f,
                            $"📍 {primary.position:F2}");
                    }
                    break;

                case EventType.MouseDown:
                    if (e.button == 0 && !e.alt && HandleUtility.nearestControl == controlId)
                    {
                        GUIUtility.hotControl = controlId;
                        dragControlId = controlId;
                        dragSelection = Selection.transforms;
                        dragPrimaryBounds = bounds;
                        // จับค่าคงที่ตอนเริ่มลากเท่านั้น ห้ามคำนวณใหม่จาก primary.position ที่เปลี่ยนไปทุกเฟรม
                        // (ไม่งั้นค่านี้จะสะสมผิดเพี้ยนทบไปเรื่อยๆ จนวัตถุพุ่งหลุดออกจากฉาก)
                        dragPivotOffset = primary.position - bounds.center;
                        dragFallbackDistance = Vector3.Distance(
                            SceneView.currentDrawingSceneView.camera.transform.position, primary.position);

                        dragOffsets = new Vector3[dragSelection.Length];
                        for (int i = 0; i < dragSelection.Length; i++)
                        {
                            dragOffsets[i] = dragSelection[i].position - primary.position;
                        }

                        Undo.RecordObjects(dragSelection, "Perfect Surface Snap Drag");
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId && dragSelection != null)
                    {
                        Vector3 snappedPrimaryPos = ComputeSnappedPosition(e.mousePosition);
                        for (int i = 0; i < dragSelection.Length; i++)
                        {
                            if (dragSelection[i] == null)
                            {
                                continue;
                            }
                            dragSelection[i].position = snappedPrimaryPos + dragOffsets[i];
                        }
                        e.Use();
                        SceneView.RepaintAll();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId)
                    {
                        GUIUtility.hotControl = 0;
                        RecordHistory($"🧲 ลาก/แปะผิว {dragSelection.Length} ชิ้น");
                        TrackObjectsFromTransforms(dragSelection);
                        dragControlId = -1;
                        dragSelection = null;
                        e.Use();
                    }
                    break;
            }
        }

        private Vector3 ComputeSnappedPosition(Vector2 mousePosition)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 2000f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            HashSet<Transform> ignore = BuildIgnoreSet();
            Vector3 halfExtents = dragPrimaryBounds.extents;

            foreach (RaycastHit hit in hits)
            {
                if (ignore.Contains(hit.collider.transform))
                {
                    continue;
                }

                // ระยะที่ต้องดันออกจากผิวชน = การฉาย half-extents ของกล่อง AABB ลงบนแนว normal (สูตร AABB-plane projection)
                // บวก snapOffset เพิ่ม เพื่อให้ผู้ใช้ปรับเองได้ว่าจะแนบสนิท (0) เว้นระยะห่าง (>0) หรือฝังจม (<0)
                float pushOut = Mathf.Abs(halfExtents.x * hit.normal.x) +
                                Mathf.Abs(halfExtents.y * hit.normal.y) +
                                Mathf.Abs(halfExtents.z * hit.normal.z) +
                                snapOffset;

                Vector3 targetBoundsCenter = hit.point + hit.normal * pushOut;
                return ApplyGridSnap(targetBoundsCenter + dragPivotOffset);
            }

            // ไม่เจอผิวใดๆ (เช่น วัตถุปลายทางไม่มี Collider) ให้เกาะแนวเมาส์ที่ระยะคงที่จากกล้อง
            // ใช้ dragFallbackDistance ที่จับค่าไว้ตอนเริ่มลากเท่านั้น ห้ามคำนวณจาก transform.position ปัจจุบัน
            Vector3 fallbackBoundsCenter = ray.GetPoint(dragFallbackDistance);
            return ApplyGridSnap(fallbackBoundsCenter + dragPivotOffset);
        }

        private Vector3 ApplyGridSnap(Vector3 position)
        {
            if (!gridSnapEnabled)
            {
                return position;
            }

            float x = Mathf.Round(position.x / gridSize.x) * gridSize.x;
            float y = gridSnapY ? Mathf.Round(position.y / gridSize.y) * gridSize.y : position.y;
            float z = Mathf.Round(position.z / gridSize.z) * gridSize.z;
            return new Vector3(x, y, z);
        }

        private static HashSet<Transform> BuildIgnoreSet()
        {
            HashSet<Transform> set = new HashSet<Transform>();
            foreach (GameObject go in Selection.gameObjects)
            {
                foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                {
                    set.Add(t);
                }
            }
            return set;
        }

        private static Bounds GetWorldBounds(Transform t)
        {
            Renderer[] renderers = t.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    b.Encapsulate(renderers[i].bounds);
                }
                return b;
            }

            Collider[] colliders = t.GetComponentsInChildren<Collider>();
            if (colliders.Length > 0)
            {
                Bounds b = colliders[0].bounds;
                for (int i = 1; i < colliders.Length; i++)
                {
                    b.Encapsulate(colliders[i].bounds);
                }
                return b;
            }

            return new Bounds(t.position, Vector3.one * 0.5f);
        }

        // ------------------------------------------------------------------
        // History Panel (backed by Unity's own Undo group system)
        // ------------------------------------------------------------------

        private void RecordHistory(string label)
        {
            int group = Undo.GetCurrentGroup();
            historyEntries.Insert(0, new HistoryEntry
            {
                label = label,
                undoGroup = group,
                timestampTicks = System.DateTime.Now.Ticks
            });

            if (historyEntries.Count > 50)
            {
                historyEntries.RemoveAt(historyEntries.Count - 1);
            }

            Undo.IncrementCurrentGroup();
            Repaint();
        }

        private void JumpToHistory(HistoryEntry entry)
        {
            // ย้อนกลับไปยัง "จุดหลังจบการกระทำนี้" (undoGroup + 1 = ทุกอย่างที่เกิดขึ้นหลังจากนั้น)
            Undo.RevertAllDownToGroup(entry.undoGroup + 1);
            SceneView.RepaintAll();
            Repaint();
        }

        private void DrawHistoryPanel()
        {
            showHistoryPanel = EditorGUILayout.Foldout(
                showHistoryPanel, $"📜 ประวัติ (History) ({historyEntries.Count})", true, EditorStyles.foldoutHeader);
            if (!showHistoryPanel)
            {
                return;
            }

            if (historyEntries.Count == 0)
            {
                EditorGUILayout.HelpBox("ยังไม่มีการกระทำใดๆ ในเซสชันนี้ (วาง/หมุน/กรุ๊ป/หลอม จะถูกบันทึกที่นี่)", MessageType.None);
                return;
            }

            EditorGUILayout.HelpBox(
                "คลิกรายการเพื่อย้อนโปรเจกต์กลับไปยังจุดนั้น (เหมือนกด Ctrl+Z รวดเดียวถึงจุดนั้น) " +
                "หมายเหตุ: ใช้กระโดดถอยหลังเท่านั้น ถ้าจะไปข้างหน้าอีกให้ใช้ Ctrl+Y ตามปกติของ Unity",
                MessageType.None);

            foreach (HistoryEntry entry in historyEntries)
            {
                string time = new System.DateTime(entry.timestampTicks).ToString("HH:mm:ss");
                if (GUILayout.Button($"⏪ [{time}] {entry.label}", EditorStyles.miniButton))
                {
                    JumpToHistory(entry);
                }
            }

            EditorGUILayout.Space(4);
            if (GUILayout.Button("🧹 ล้างรายการประวัติ (แค่ list ไม่กระทบ Undo จริง)"))
            {
                historyEntries.Clear();
            }
        }

        // ------------------------------------------------------------------
        // Object Manager Panel
        // ------------------------------------------------------------------

        private void TrackObject(GameObject go)
        {
            if (go != null && !trackedObjects.Contains(go))
            {
                trackedObjects.Add(go);
            }
        }

        private void TrackObjectsFromTransforms(Transform[] transforms)
        {
            if (transforms == null)
            {
                return;
            }

            foreach (Transform t in transforms)
            {
                if (t != null)
                {
                    TrackObject(t.gameObject);
                }
            }
        }

        private void PruneTrackedLists()
        {
            trackedObjects.RemoveAll(go => go == null);
            favoriteObjects.RemoveAll(go => go == null);
        }

        private void DrawObjectManagerPanel()
        {
            PruneTrackedLists();

            showObjectManagerPanel = EditorGUILayout.Foldout(
                showObjectManagerPanel, $"🗂 จัดการวัตถุ ({trackedObjects.Count})", true, EditorStyles.foldoutHeader);
            if (!showObjectManagerPanel)
            {
                return;
            }

            if (favoriteObjects.Count > 0)
            {
                EditorGUILayout.LabelField("⭐ Favorite", EditorStyles.miniBoldLabel);
                foreach (GameObject go in favoriteObjects)
                {
                    DrawObjectRow(go);
                }
                EditorGUILayout.Space(4);
            }

            EditorGUILayout.LabelField("วัตถุที่จัดการด้วยเครื่องมือนี้", EditorStyles.miniBoldLabel);
            if (trackedObjects.Count == 0)
            {
                EditorGUILayout.HelpBox("ยังไม่มีวัตถุที่ถูกลาก/หมุน/กรุ๊ป/หลอม ด้วยเครื่องมือนี้", MessageType.None);
                return;
            }

            foreach (GameObject go in trackedObjects)
            {
                DrawObjectRow(go);
            }
        }

        private void DrawObjectRow(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            if (GUILayout.Button(go.name, EditorStyles.label, GUILayout.ExpandWidth(true)))
            {
                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);
            }

            bool isFavorite = favoriteObjects.Contains(go);
            if (GUILayout.Toggle(isFavorite, "⭐", "Button", GUILayout.Width(28)) != isFavorite)
            {
                if (isFavorite)
                {
                    favoriteObjects.Remove(go);
                }
                else
                {
                    favoriteObjects.Add(go);
                }
            }

            bool isHidden = SceneVisibilityManager.instance.IsHidden(go);
            if (GUILayout.Toggle(isHidden, "👁", "Button", GUILayout.Width(28)) != isHidden)
            {
                if (isHidden)
                {
                    SceneVisibilityManager.instance.Show(go, false);
                }
                else
                {
                    SceneVisibilityManager.instance.Hide(go, false);
                }
            }

            bool isLocked = SceneVisibilityManager.instance.IsPickingDisabled(go);
            if (GUILayout.Toggle(isLocked, "🔒", "Button", GUILayout.Width(28)) != isLocked)
            {
                if (isLocked)
                {
                    SceneVisibilityManager.instance.EnablePicking(go, false);
                }
                else
                {
                    SceneVisibilityManager.instance.DisablePicking(go, false);
                }
            }

            Color previousColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.4f, 0.35f);
            if (GUILayout.Button("🗑", GUILayout.Width(28)))
            {
                trackedObjects.Remove(go);
                favoriteObjects.Remove(go);
                Undo.DestroyObjectImmediate(go);
                GUI.backgroundColor = previousColor;
                EditorGUILayout.EndHorizontal();
                GUIUtility.ExitGUI();
            }
            GUI.backgroundColor = previousColor;

            EditorGUILayout.EndHorizontal();
        }

        // ------------------------------------------------------------------
        // Safe Grouping
        // ------------------------------------------------------------------

        private static Vector3 ComputeAverageCenter(GameObject[] objects)
        {
            Vector3 center = Vector3.zero;
            foreach (GameObject go in objects)
            {
                center += go.transform.position;
            }
            return center / objects.Length;
        }

        private void GroupSelectedSafely()
        {
            GameObject[] selected = Selection.gameObjects;
            if (selected.Length == 0)
            {
                return;
            }

            Vector3 center = ComputeAverageCenter(selected);

            GameObject group = new GameObject(string.IsNullOrEmpty(groupName) ? "Perfect_Group" : groupName);
            Undo.RegisterCreatedObjectUndo(group, "Perfect Grouping");
            group.transform.position = center;
            group.transform.rotation = Quaternion.identity;
            group.transform.localScale = Vector3.one;

            foreach (GameObject go in selected)
            {
                Undo.SetTransformParent(go.transform, group.transform, "Perfect Grouping");
            }

            RecordHistory($"📦 จัดกลุ่ม: {group.name}");
            TrackObject(group);

            Selection.activeGameObject = group;
        }

        // ------------------------------------------------------------------
        // Permanent Mesh Combine (single physical GameObject, destructive)
        // ------------------------------------------------------------------

        private void MergeSelectedToSingleMesh()
        {
            GameObject[] selected = Selection.gameObjects;
            if (selected.Length == 0)
            {
                return;
            }

            List<MeshFilter> sourceFilters = new List<MeshFilter>();
            foreach (GameObject go in selected)
            {
                sourceFilters.AddRange(go.GetComponentsInChildren<MeshFilter>(true));
            }

            if (sourceFilters.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Perfect Gamified Builder",
                    "ไม่พบ MeshFilter ในวัตถุที่เลือก ไม่สามารถหลอมรวมเป็น Mesh เดียวได้",
                    "ตกลง");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "🔥 หลอมรวมถาวร (Merge To Single Mesh)",
                $"จะรวม {sourceFilters.Count} mesh จาก {selected.Length} วัตถุ ให้เป็น GameObject เดียว\n\n" +
                "การกระทำนี้จะ:\n" +
                "• ทำลาย (Destroy) วัตถุต้นฉบับทั้งหมดที่เลือกไว้\n" +
                "• Component อื่นที่ไม่ใช่ MeshFilter/MeshRenderer (เช่น Script, Collider เฉพาะของแต่ละชิ้น) จะหายไปจากวัตถุต้นฉบับ\n" +
                "• สร้าง MeshCollider ใหม่ให้วัตถุที่รวมแล้วแทน 1 ตัว\n\n" +
                "กด Ctrl+Z ย้อนกลับได้ทันทีหลังทำ แต่ระบบอื่นที่อ้างอิงถึงวัตถุเดิม (เช่น field ที่ลากอ้างอิงไว้) จะไม่ถูกกู้คืนอัตโนมัติ",
                "หลอมรวมเลย", "ยกเลิก");

            if (!confirmed)
            {
                return;
            }

            Dictionary<int, List<CombineInstance>> groups = new Dictionary<int, List<CombineInstance>>();
            Dictionary<int, Material> materialLookup = new Dictionary<int, Material>();

            foreach (MeshFilter mf in sourceFilters)
            {
                Mesh mesh = mf.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                Renderer renderer = mf.GetComponent<Renderer>();
                Material[] materials = renderer != null ? renderer.sharedMaterials : new Material[] { null };

                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    Material mat = materials.Length > 0
                        ? materials[Mathf.Min(sub, materials.Length - 1)]
                        : null;
                    int key = mat != null ? mat.GetInstanceID() : 0;

                    if (!groups.TryGetValue(key, out List<CombineInstance> list))
                    {
                        list = new List<CombineInstance>();
                        groups[key] = list;
                        materialLookup[key] = mat;
                    }

                    list.Add(new CombineInstance
                    {
                        mesh = mesh,
                        subMeshIndex = sub,
                        transform = mf.transform.localToWorldMatrix
                    });
                }
            }

            Vector3 center = ComputeAverageCenter(selected);
            List<CombineInstance> finalInstances = new List<CombineInstance>();
            List<Material> finalMaterials = new List<Material>();

            foreach (KeyValuePair<int, List<CombineInstance>> group in groups)
            {
                CombineInstance[] instances = group.Value.ToArray();
                for (int i = 0; i < instances.Length; i++)
                {
                    // ดึงจากพิกัดโลกมาไว้ที่จุดกึ่งกลางเฉลี่ย เพื่อให้วัตถุที่รวมแล้วมี pivot ที่สมเหตุสมผล
                    instances[i].transform = Matrix4x4.Translate(-center) * instances[i].transform;
                }

                Mesh groupMesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                groupMesh.CombineMeshes(instances, true, true);

                finalInstances.Add(new CombineInstance
                {
                    mesh = groupMesh,
                    subMeshIndex = 0,
                    transform = Matrix4x4.identity
                });
                finalMaterials.Add(materialLookup[group.Key]);
            }

            Mesh combinedMesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            combinedMesh.CombineMeshes(finalInstances.ToArray(), false, true);
            combinedMesh.name = string.IsNullOrEmpty(groupName) ? "Merged_Mesh" : groupName;

            // mesh ต่อ material ที่สร้างไว้ข้างบนเป็นของชั่วคราวระหว่างทาง พอรวมเข้า combinedMesh
            // แล้วก็ไม่มีใครอ้างถึงอีก แต่ Mesh เป็น UnityEngine.Object ซึ่ง GC ของ C# เก็บกวาดให้ไม่ได้
            // ต้องสั่งทำลายเอง ไม่งั้นค้างในหน่วยความจำสะสมทุกครั้งที่กดหลอม จนกว่าจะปิด Unity
            foreach (CombineInstance instance in finalInstances)
            {
                if (instance.mesh != null)
                {
                    DestroyImmediate(instance.mesh);
                }
            }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Perfect Mesh Combine");

            GameObject merged = new GameObject(string.IsNullOrEmpty(groupName) ? "Merged_Object" : groupName);
            Undo.RegisterCreatedObjectUndo(merged, "Perfect Mesh Combine");
            merged.transform.position = center;
            merged.transform.rotation = Quaternion.identity;
            merged.transform.localScale = Vector3.one;

            MeshFilter meshFilter = merged.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = combinedMesh;

            MeshRenderer meshRenderer = merged.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterials = finalMaterials.ToArray();

            MeshCollider meshCollider = merged.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = combinedMesh;

            foreach (GameObject go in selected)
            {
                Undo.DestroyObjectImmediate(go);
            }

            Undo.CollapseUndoOperations(undoGroup);

            RecordHistory($"🔥 หลอมรวม: {merged.name}");
            TrackObject(merged);

            Selection.activeGameObject = merged;
        }
    }
}
#endif
