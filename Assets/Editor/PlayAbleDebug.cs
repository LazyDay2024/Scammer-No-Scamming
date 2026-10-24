using System.Reflection;
using UnityEditor;
using UnityEngine;
using ScammerNoScamming;

// Play-mode steps for driving the PlayAble case flow through EditorBridge ("exec PlayAbleDebug.Start" etc.),
// since the IMGUI screens cannot be clicked from outside.
public static class PlayAbleDebug
{
    static T Field<T>(object o, string name) => (T)o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public).GetValue(o);
    static void Call(object o, string name, params object[] args) =>
        o.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public).Invoke(o, args);

    public static void CloseScreens()
    {
        foreach (var screen in Object.FindObjectsByType<GameScreen>(FindObjectsSortMode.None)) screen.Close();
    }

    public static void Start()
    {
        var flow = Object.FindFirstObjectByType<CaseFlowDemo>();
        CloseScreens();
        Call(flow, "BeginRun");
        Debug.Log("PlayAbleDebug: run started, phase " + Object.FindFirstObjectByType<CaseBriefing>().Current);
    }

    static void Interact(string name)
    {
        var target = GameObject.Find(name).GetComponent<WorldInteractable>();
        Debug.Log("PlayAbleDebug: interact " + name + " available=" + target.Available);
        target.Interact();
    }

    public static void OpenFile() => Interact("CaseFile");
    public static void OpenMap() => Call(Object.FindFirstObjectByType<CaseBriefing>(), "OpenMap");
    public static void CloseDoc() => Call(Object.FindFirstObjectByType<CaseDocumentScreen>(), "Finish");
    public static void TakeMap() => Interact("FoldedMap");
    public static void PickWrong() => Pick("north_tower");
    public static void Genes() => Call(Object.FindFirstObjectByType<CaseFlowDemo>(), "OnGenes", new GeneLoadout(GeneLoadout.DefaultFreeSlots, GeneLoadout.DefaultMaxSlots, 20f));
    public static void KillGuards() { foreach (var e in Object.FindFirstObjectByType<CaseBriefing>().DoorGuards.Enemies) if (e != null) e.GetComponent<Health>().TakeDamage(new DamageInfo { Amount = 999f }); }
    public static void PickRight() => Pick("east_shophouse");

    static void Pick(string id)
    {
        CloseScreens();
        foreach (var site in MapSite.All)
            if (site.siteId == id) { Call(Object.FindFirstObjectByType<CaseBriefing>(), "Picked", site); return; }
        Debug.LogError("PlayAbleDebug: no site " + id);
    }

    public static void FaceDesk()
    {
        var player = PlayerLocator.Player;
        var target = GameObject.Find("CaseFile").transform.position;
        var look = target - player.GetComponentInChildren<Camera>().transform.position;
        player.transform.rotation = Quaternion.Euler(0f, Quaternion.LookRotation(look).eulerAngles.y, 0f);
        player.ResetLook();
        Debug.Log("PlayAbleDebug: desk " + target + " player " + player.transform.position);
    }

    public static void Tick() => Call(Object.FindFirstObjectByType<CaseBriefing>(), "Update");

    public static void TickPortals() { foreach (var p in Object.FindObjectsByType<DoorPortal>(FindObjectsSortMode.None)) Call(p, "Update"); }

    public static void GoToExit()
    {
        var player = PlayerLocator.Player;
        var cc = player.GetComponent<CharacterController>();
        var exit = GameObject.Find("ExitPortal").transform;
        cc.enabled = false;
        player.transform.position = exit.position;
        cc.enabled = true;
    }

    public static void DieAndRevive()
    {
        var player = PlayerLocator.Player;
        var vitals = player.GetComponent<PlayerVitals>();
        player.GetComponent<Health>().TakeDamage(new DamageInfo { Amount = 999f });
        Debug.Log("PlayAbleDebug: dead controller " + player.enabled + " camRot " + player.GetComponentInChildren<Camera>().transform.localEulerAngles);
        vitals.ResetForAction(RunModifiers.None);
        Debug.Log("PlayAbleDebug: revived controller " + player.enabled + " mount " + player.GetComponent<PlayerMount>().enabled + " gun " + player.GetComponent<PlayerWeaponController>().enabled + " camRot " + player.GetComponentInChildren<Camera>().transform.localEulerAngles + " hp " + vitals.Health.Current);
    }

    static void Visit(string id, float x, float z)
    {
        var player = PlayerLocator.Player;
        var cc = player.GetComponent<CharacterController>();
        var root = HideoutInterior.Find(id).transform;
        cc.enabled = false;
        player.transform.SetPositionAndRotation(root.TransformPoint(new Vector3(x, .3f, z)), Quaternion.identity);
        cc.enabled = true;
        player.ResetLook();
    }

    public static void VisitEast() => Visit("east_shophouse", 0f, 2.6f);
    public static void VisitEastMid() => Visit("east_shophouse", 0f, 9f);
    public static void VisitNorth() => Visit("north_tower", -8f, 2.6f);
    public static void VisitNorthMid() => Visit("north_tower", 0f, 9f);
    public static void VisitSouth() => Visit("south_house", 0f, 2.6f);
    public static void VisitSouthMid() => Visit("south_house", 0f, 8f);

    public static void BeginRaids()
    {
        foreach (var h in Object.FindObjectsByType<Hideout>(FindObjectsSortMode.None)) h.GetComponent<RaidEncounter>().BeginRaid();
        Debug.Log("PlayAbleDebug: raids begun");
    }

    public static void HideCeilings()
    {
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            if (r.name == "Ceiling") r.enabled = false;
        foreach (var h in Object.FindObjectsByType<Hideout>(FindObjectsSortMode.None))
            Debug.Log("PlayAbleDebug: hideout " + h.hideoutId + " at " + h.transform.position);
    }

    public static void Snap() => ScreenCapture.CaptureScreenshot("D:/AI/runtime/snap.png");

    public static void State()
    {
        var b = Object.FindFirstObjectByType<CaseBriefing>();
        Debug.Log($"PlayAbleDebug: phase {b.Current} wrong {b.WrongPicks} map {b.HasMap} inside {b.Inside} stationRaid left {b.stationRaid.RemainingEnemies} guards {(b.DoorGuards != null ? b.DoorGuards.RemainingEnemies : -1)} "
            + $"frame {Time.frameCount} enabled {b.enabled} active {b.stationRaid.Active} count {b.stationRaid.Enemies.Count} cleared {b.stationRaid.Cleared} screen {GameScreen.AnyOpen} hp {PlayerLocator.Player.GetComponent<PlayerVitals>().Health.Current} player {PlayerLocator.Player.transform.position} trouble {Object.FindFirstObjectByType<CaseFlowDemo>().Trouble}");
    }

    public static void KillStationRaid()
    {
        foreach (var e in Object.FindFirstObjectByType<CaseBriefing>().stationRaid.Enemies) if (e != null) e.GetComponent<Health>().TakeDamage(new DamageInfo { Amount = 999f });
    }

    public static void GoToDoor()
    {
        var player = PlayerLocator.Player;
        var cc = player.GetComponent<CharacterController>();
        var door = GameObject.Find("DoorPortal_east_shophouse").transform;
        cc.enabled = false;
        player.transform.position = door.position;
        cc.enabled = true;
    }
}

public static class PlayAbleDump
{
    public static void Police()
    {
        foreach (var go in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (go.name != "PoliceStation" || go.parent == null) continue;
            var sb = new System.Text.StringBuilder("PlayAbleDump: " + go.name + " parent " + go.parent.name + " pos " + go.position + " yaw " + go.eulerAngles.y + "\n");
            foreach (Transform c in go) sb.AppendLine("  " + c.name + " " + c.localPosition + " children " + c.childCount);
            Debug.Log(sb.ToString());
        }
        foreach (var q in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (q.name.StartsWith("QP_")) Debug.Log("PlayAbleDump: " + q.name + " " + q.position + " yaw " + q.eulerAngles.y);
    }
}

// Drives one whole case through the real systems, step by step, for EditorBridge ("exec PlayAbleRun.X").
public static class PlayAbleRun
{
    static CaseFlowDemo Flow => Object.FindFirstObjectByType<CaseFlowDemo>();
    static CaseBriefing Briefing => Object.FindFirstObjectByType<CaseBriefing>();
    static string Id => Flow.Current.HideoutId;
    static void Call(object o, string name, params object[] args) =>
        o.GetType().GetMethod(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).Invoke(o, args);

    static void Teleport(Vector3 position)
    {
        var player = PlayerLocator.Player;
        var cc = player.GetComponent<CharacterController>();
        cc.enabled = false;
        player.transform.position = position;
        cc.enabled = true;
    }

    public static void Pause() => UnityEditor.EditorApplication.isPaused = true;
    public static void Step() { for (int i = 0; i < 20; i++) UnityEditor.EditorApplication.Step(); }

    public static void Brief()
    {
        PlayAbleDebug.CloseScreens();
        GameObject.Find("CaseFile").GetComponent<WorldInteractable>().Interact();
        PlayAbleDebug.CloseDoc();
        GameObject.Find("FoldedMap").GetComponent<WorldInteractable>().Interact();
        PlayAbleDebug.CloseScreens();
        foreach (var site in MapSite.All)
            if (site.siteId == Id) Call(Briefing, "Picked", site);
        Log("briefed");
    }

    public static void Dispatch()
    {
        PlayAbleDebug.CloseScreens();
        PlayAbleDebug.Genes();
        Log("dispatched");
    }

    public static void KillGuards() { PlayAbleDebug.KillGuards(); Log("guards killed"); }

    public static void EnterDoor()
    {
        Teleport(GameObject.Find("DoorPortal_" + Id).transform.position);
        foreach (var p in Object.FindObjectsByType<DoorPortal>(FindObjectsSortMode.None)) Call(p, "Update");
        Log("entered");
    }

    public static void ClearInside()
    {
        var raid = Hideout.Find(Id).GetComponent<RaidEncounter>();
        foreach (var e in raid.Enemies) if (e != null && !e.IsDead) e.GetComponent<Health>().TakeDamage(new DamageInfo { Amount = 999f });
        Log("inside cleared, enemies left " + raid.RemainingEnemies);
    }

    public static void EnterClearCollect() { EnterDoor(); ClearInside(); Collect(); }

    public static void ToMenu() { PlayAbleDebug.CloseScreens(); Call(Flow, "ReturnToMenu"); Log("menu"); }
    public static void Where() => Log("where");
    public static void Die() { PlayerLocator.Player.GetComponent<Health>().TakeDamage(new DamageInfo { Amount = 999f }); Log("died"); }
    public static void Retry() { PlayAbleDebug.CloseScreens(); Flow.RetryAction(); Log("retried"); }

    public static void Collect()
    {
        var raid = Hideout.Find(Id).GetComponent<RaidEncounter>();
        Debug.Log("PlayAbleRun: cleared " + raid.Cleared + " collected " + (raid.TryCollectEvidence() | raid.TryCollectEvidence() | raid.TryCollectEvidence()));
        Log("evidence");
    }

    public static void Leave()
    {
        Teleport(Hideout.Find(Id).transform.position + new Vector3(HideoutInteriors.DoorX(PlanOf(Id)), .3f, .8f));
        foreach (var p in Object.FindObjectsByType<DoorPortal>(FindObjectsSortMode.None)) Call(p, "Update");
        Log("left");
    }

    static InteriorPlan PlanOf(string id) => id == "north_tower" ? InteriorPlan.CallCenter : id == "south_house" ? InteriorPlan.FakeOfficer : InteriorPlan.FakeShop;

    public static void Seal()
    {
        Debug.Log("PlayAbleRun: seal " + Flow.CompleteSeal());
        Log("sealed");
    }

    public static void NextCase()
    {
        PlayAbleDebug.CloseScreens();
        Call(Flow, "NextCase");
        Log("next");
    }

    public static void Log(string what)
    {
        var f = Flow;
        Debug.Log("PlayAbleRun: " + what + " | case " + (f.Current != null ? f.Current.CaseId : "-") + " evidence " + f.HasEvidence + " action " + f.ActionActive
            + " trouble " + f.Trouble + " player " + PlayerLocator.Player.transform.position + " briefing " + Briefing.Current + " loading " + Briefing.InteriorLoading
            + " screens " + GameScreen.AnyOpen + " frame " + Time.frameCount);
    }
}
// touch
