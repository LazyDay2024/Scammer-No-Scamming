using System;
using UnityEngine;

namespace ScammerNoScamming
{
    // The evidence of one case: several items in the hideout and the Seal table at the station.
    // The player collects each item on foot (hold E beside it), carries them back and Seals them one at a time
    // at the table (hold E). The mission is complete only when every item is Sealed.
    // Lives in the scene, so replaying the action phase after a death puts every item back.
    public class EvidenceCase : MonoBehaviour
    {
        public EvidenceItem[] items = Array.Empty<EvidenceItem>();
        public Transform sealTable;
        public float interactRange = 2.5f;
        public float collectSeconds = 1f;
        public float sealSeconds = 1.2f;

        public int Total => items.Length;
        public int Carried { get; private set; }
        public int Sealed { get; private set; }
        public bool IsComplete => Total > 0 && Sealed == Total;

        PlayerControls controls;
        EvidenceItem target;
        bool atTable;
        float hold;

        void Update()
        {
            var player = PlayerLocator.Player;
            var character = Interaction.Player;
            if (IsComplete || player == null || character == null || character.Health.IsDead || player.IsMounted || GameScreen.AnyOpen)
            {
                target = null;
                atTable = false;
                hold = 0f;
                return;
            }

            Aim(player.transform.position);
            if (controls == null) controls = player.GetComponent<PlayerControls>();
            bool holding = (target != null || atTable) && controls != null && controls.Interact.IsPressed();
            hold = holding ? hold + Time.deltaTime : 0f;
            if (hold < (target != null ? collectSeconds : sealSeconds)) return;

            hold = 0f;
            if (target != null)
            {
                target.Collect();
                Carried++;
            }
            else
            {
                Carried--;
                Sealed++;
            }
            GameEvents.RaiseEvidenceChanged(Carried, Sealed, Total);
            if (IsComplete) GameEvents.RaiseMissionComplete();
        }

        // Picks the nearest item still lying around; with none in reach, the table counts if something is in hand.
        void Aim(Vector3 playerPosition)
        {
            target = null;
            float best = interactRange * interactRange;
            foreach (var item in items)
            {
                if (item == null || item.Collected) continue;
                float distance = FlatSqr(item.transform.position - playerPosition);
                if (distance > best) continue;
                best = distance;
                target = item;
            }
            atTable = target == null && Carried > 0 && sealTable != null
                && FlatSqr(sealTable.position - playerPosition) <= interactRange * interactRange;
        }

        static float FlatSqr(Vector3 offset)
        {
            offset.y = 0f;
            return offset.sqrMagnitude;
        }

        void OnGUI()
        {
            if (Total == 0 || GameScreen.AnyOpen) return;
            var canvas = ThaiGui.BeginCanvas();

            var panel = new Rect(canvas.width - 520f, 130f, 480f, 84f);
            GUI.DrawTexture(panel, ThaiGui.Solid(new Color(0.04f, 0.07f, 0.08f, 0.7f)));
            GUI.Label(new Rect(panel.x + 20f, panel.y + 8f, 440f, 36f), "หลักฐาน " + Sealed + "/" + Total + " Seal แล้ว",
                ThaiGui.Text(28, IsComplete ? ThaiGui.Teal : ThaiGui.Gold, FontStyle.Bold));
            GUI.Label(new Rect(panel.x + 20f, panel.y + 44f, 440f, 32f), "ถืออยู่ " + Carried + " ชิ้น",
                ThaiGui.Text(24, ThaiGui.Bone));

            if (IsComplete)
            {
                var banner = new Rect(canvas.width * 0.5f - 420f, canvas.height * 0.3f, 840f, 150f);
                GUI.DrawTexture(banner, ThaiGui.Solid(new Color(0.04f, 0.07f, 0.08f, 0.8f)));
                GUI.Label(new Rect(banner.x, banner.y + 14f, banner.width, 70f), "ภารกิจสำเร็จ",
                    ThaiGui.Text(56, ThaiGui.Gold, FontStyle.Bold, TextAnchor.MiddleCenter));
                GUI.Label(new Rect(banner.x, banner.y + 86f, banner.width, 50f), "Seal หลักฐานครบทุกชิ้น",
                    ThaiGui.Text(32, ThaiGui.Bone, FontStyle.Normal, TextAnchor.MiddleCenter));
            }
            else if (target != null || atTable)
            {
                var prompt = new Rect(canvas.width * 0.5f - 420f, canvas.height * 0.7f, 840f, 70f);
                GUI.DrawTexture(prompt, ThaiGui.Solid(new Color(0.04f, 0.07f, 0.08f, 0.75f)));
                string text = target != null ? "กด E ค้างเพื่อเก็บของกลาง: " + target.label : "กด E ค้างเพื่อ Seal หลักฐาน (ถือ " + Carried + " ชิ้น)";
                GUI.Label(prompt, text, ThaiGui.Text(32, ThaiGui.Bone, FontStyle.Bold, TextAnchor.MiddleCenter));

                var bar = new Rect(canvas.width * 0.5f - 250f, prompt.yMax + 12f, 500f, 20f);
                GUI.DrawTexture(bar, ThaiGui.Solid(new Color(0f, 0f, 0f, 0.6f)));
                float seconds = target != null ? collectSeconds : sealSeconds;
                GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(hold / seconds), bar.height), ThaiGui.Solid(ThaiGui.Gold));
            }
            ThaiGui.EndCanvas();
        }
    }
}
