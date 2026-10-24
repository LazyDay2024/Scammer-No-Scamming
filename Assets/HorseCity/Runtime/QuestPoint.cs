using UnityEngine;

namespace HorseCity
{
    public enum QuestPointKind
    {
        PlayerSpawn,
        PoliceStation,
        PoliceStable,
        Vet,
        HorseSeller,
        WeaponShop,
        CentralTower,
        SuspectHideout,
        HitchingPost,
    }

    /// <summary>
    /// Named anchor for quests and objectives. Look them up with QuestPoint.Find(kind).
    /// </summary>
    public class QuestPoint : MonoBehaviour
    {
        public QuestPointKind kind;
        [TextArea] public string note;

        public static QuestPoint Find(QuestPointKind kind)
        {
            foreach (QuestPoint p in Resources.FindObjectsOfTypeAll<QuestPoint>())
                if (p.kind == kind && p.gameObject.scene.IsValid()) return p;
            return null;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = kind == QuestPointKind.WeaponShop ? Color.red : new Color(0.2f, 0.8f, 1f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up, 1.2f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 6f);
        }
    }
}
