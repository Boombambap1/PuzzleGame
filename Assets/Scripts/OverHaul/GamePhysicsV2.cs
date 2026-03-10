using System.Collections.Generic;
using UnityEngine;
using NewArch;

namespace NewArch
{

/// <summary>
/// Resolves one full physics step synchronously on player input.
/// Produces a StepData record that AnimationControlV2 replays visually.
///
/// Step order per Tick:
///   1. Box sliding
///   2. Gravity / falling + death
///
/// Part of the Model layer (MVC).
/// </summary>
public class GamePhysicsV2 : MonoBehaviour
{
    // ── Configuration ─────────────────────────────────────────────────────────

    [Header("Physics Settings")]
    [SerializeField] private int   deathBoundY          = -10;
    [SerializeField] private int   respawnHeightOffset  = 10;
    [SerializeField] private int   maxTicksPerStep      = 100;
    [SerializeField] private int   stableTickThreshold  = 2;

    // ── Dependencies ──────────────────────────────────────────────────────────

    private GeoStateV2  geoState;
    private GameStateV2 gameState;

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Fired when a full step has resolved, carrying all tick data.</summary>
    public static event System.Action<StepData> OnStepResolved;

    // ── Runtime State ─────────────────────────────────────────────────────────

    private int      tickCount;
    private StepData currentStep;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        geoState  = GetComponent<GeoStateV2>();
        gameState = GetComponent<GameStateV2>();
    }

    void Start()
    {
        Invoke(nameof(RunInitialGravity), 0.1f);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Entry point for all player-initiated moves.
    /// Pass Vector3Int.zero to trigger a pending respawn without moving.
    /// </summary>
    public StepData StartStep(Vector3Int playerInput)
    {
        // Respawn pass takes priority over movement.
        if (NeedsRespawn())
        {
            BeginStep();
            TickData respawnTick = NewTick();
            ProcessRespawning(respawnTick);
            CommitTick(respawnTick);
            Step();
            return FinishStep();
        }

        if (playerInput == Vector3Int.zero) return null;

        GameObj player = gameState.GetPlayer();
        if (player == null || !player.IsAlive()) return null;

        Vector3Int targetPos = player.position + playerInput;
        if (!IsValidMoveTarget(targetPos, playerInput)) return null;

        BeginStep();
        ResetAllMovementFlags();

        TickData firstTick = NewTick();
        ExecutePlayerMove(player, playerInput, targetPos, firstTick);
        CommitTick(firstTick);

        Step();
        return FinishStep();
    }

    /// <summary>Returns true if any dead object has a prefab (pending respawn).</summary>
    public bool NeedsRespawn()
    {
        foreach (GameObj obj in gameState.GetAllObjects())
            if (!obj.IsAlive() && obj.prefab != null) return true;
        return false;
    }

    // ── Step / Tick Loop ──────────────────────────────────────────────────────

    private void Step()
    {
        int emptyTicks = 0;

        while (tickCount < maxTicksPerStep)
        {
            TickData tick  = NewTick();
            bool     moved = Tick(tick);

            if (moved)
            {
                CommitTick(tick);
                emptyTicks = 0;
            }
            else
            {
                emptyTicks++;
                if (emptyTicks >= stableTickThreshold) break;
            }
        }
    }

    private bool Tick(TickData tick)
    {
        bool activity = false;
        activity |= ProcessBoxSliding(tick);
        activity |= ProcessFalling(tick);
        return activity;
    }

    // ── Physics Processors ────────────────────────────────────────────────────

    private bool ProcessFalling(TickData tick)
    {
        bool activity = false;

        foreach (GameObj obj in gameState.GetAllObjects())
        {
            if (!obj.IsAlive()) continue;
            if (!gameState.IsObjectInFreefall(obj)) continue;

            if (obj.position.y <= deathBoundY)
            {
                KillObject(obj, tick);
                activity = true;
            }
            else if (CanFallOneCell(obj))
            {
                MoveObject(obj, obj.position + Vector3Int.down, MoveType.Fall, tick);
                activity = true;
            }
        }

        return activity;
    }

    /// <summary>Placeholder for future box-sliding logic.</summary>
    private bool ProcessBoxSliding(TickData tick) => false;

    private bool ProcessRespawning(TickData tick)
    {
        bool activity = false;

        foreach (GameObj obj in gameState.GetAllObjects())
        {
            if (obj.IsAlive()) continue;
            if (obj.prefab == null) continue;
            if (obj.type != ObjectType.Box && obj.type != ObjectType.Player && obj.type != ObjectType.LargeBox) continue;

            Vector3Int? spawnPos = geoState.FindSpawnPosition(obj.prefab);
            if (!spawnPos.HasValue)
            {
                Debug.LogWarning($"[GamePhysicsV2] No spawn point found for prefab '{obj.prefab.name}'.");
                continue;
            }

            RespawnObject(obj, spawnPos.Value + new Vector3Int(0, respawnHeightOffset, 0), tick);
            activity = true;
        }

        return activity;
    }

    // ── Player Move Execution ─────────────────────────────────────────────────

    private void ExecutePlayerMove(GameObj player, Vector3Int direction, Vector3Int targetPos, TickData tick)
    {
        GameObj carriedObj  = gameState.GetObjectAt(player.position + Vector3Int.up);
        GameObj objAtTarget = gameState.GetObjectAt(targetPos);
        bool    playerMoved = false;

        if (objAtTarget != null)
        {
            Vector3Int pushTarget = targetPos + direction;
            if (IsValidMoveTarget(pushTarget, direction))
            {
                PushObject(objAtTarget, direction, tick);
                MoveObject(player, targetPos, MoveType.Walk, tick);
                playerMoved = true;
            }
        }
        else
        {
            MoveObject(player, targetPos, MoveType.Walk, tick);
            playerMoved = true;
        }

        if (playerMoved && carriedObj != null && carriedObj.IsAlive() && carriedObj.movement == Vector3Int.zero)
            MoveCarriedObject(carriedObj, direction, tick);
    }

    private void MoveCarriedObject(GameObj obj, Vector3Int direction, TickData tick)
    {
        if (obj.movement != Vector3Int.zero) return;
        if (!CanObjectMoveInDirection(obj, direction)) return;

        List<GameObj> riders = GetRidersOf(obj);
        MoveObject(obj, obj.position + direction, MoveType.Walk, tick);

        foreach (GameObj rider in riders)
            if (rider.IsAlive() && rider.movement == Vector3Int.zero)
                MoveCarriedObject(rider, direction, tick);
    }

    private void PushObject(GameObj obj, Vector3Int direction, TickData tick)
    {
        if (obj.movement != Vector3Int.zero) return;

        Vector3Int target  = obj.position + direction;
        GameObj    blocker = gameState.GetObjectAt(target);

        if (blocker != null && blocker.IsAlive() && blocker != obj)
            PushObject(blocker, direction, tick);

        if (obj.type == ObjectType.LargeBox)
        {
            Vector3Int secondaryTarget  = obj.GetSecondaryPosition() + direction;
            GameObj    secondaryBlocker = gameState.GetObjectAt(secondaryTarget);
            if (secondaryBlocker != null && secondaryBlocker.IsAlive() &&
                secondaryBlocker != obj && secondaryBlocker != blocker)
                PushObject(secondaryBlocker, direction, tick);
        }

        List<GameObj> riders = GetRidersOf(obj);
        MoveObject(obj, target, MoveType.Slide, tick);

        foreach (GameObj rider in riders)
            if (rider.IsAlive() && rider.movement == Vector3Int.zero)
                MoveCarriedObject(rider, direction, tick);
    }

    // ── Atomic Object Actions ─────────────────────────────────────────────────

    private void MoveObject(GameObj obj, Vector3Int targetPos, MoveType moveType, TickData tick)
    {
        Vector3Int fromPos = obj.position;
        gameState.MoveObjectTo(obj, targetPos);
        obj.movement += targetPos - fromPos;
        tick.AddMovement(new ObjectMovement(obj, fromPos, targetPos, moveType));
    }

    private void KillObject(GameObj obj, TickData tick)
    {
        Vector3Int deathPos = obj.position;
        obj.alive = false;
        gameState.RemoveObjectAt(deathPos);
        tick.AddMovement(new ObjectMovement(obj, deathPos, deathPos, MoveType.Die));
    }

    private void RespawnObject(GameObj obj, Vector3Int spawnPos, TickData tick)
    {
        Vector3Int oldPos = obj.position;
        obj.alive = true;
        gameState.MoveObjectTo(obj, spawnPos);
        tick.AddMovement(new ObjectMovement(obj, oldPos, spawnPos, MoveType.Respawn));
    }

    // ── Movement Validation ───────────────────────────────────────────────────

    private bool IsValidMoveTarget(Vector3Int targetPos, Vector3Int direction)
    {
        if (direction == Vector3Int.zero) return false;

        GeoType geoType = geoState.GetGeoTypeAt(targetPos);
        if (geoType == GeoType.Block || geoType == GeoType.Exit)                 return false;
        if (geoType == GeoType.Spawn  && direction != Vector3Int.down)           return false;

        GameObj objAtPos = gameState.GetObjectAt(targetPos);
        if (objAtPos == null || !objAtPos.IsAlive()) return true;

        Vector3Int nextPos = targetPos + direction;
        if (objAtPos.type == ObjectType.LargeBox)
        {
            if (targetPos == objAtPos.position           && direction ==  objAtPos.rotation)
                nextPos = objAtPos.GetSecondaryPosition() + direction;
            else if (targetPos == objAtPos.GetSecondaryPosition() && direction == -objAtPos.rotation)
                nextPos = objAtPos.position + direction;
        }

        return IsValidMoveTarget(nextPos, direction);
    }

    private bool CanFallOneCell(GameObj obj)
    {
        foreach (Vector3Int cell in obj.GetOccupiedPositions())
        {
            Vector3Int below = cell + Vector3Int.down;
            if (!IsValidMoveTarget(below, Vector3Int.down)) return false;
            if (gameState.GetObjectAt(below) != null)       return false;
        }
        return true;
    }

    private bool CanObjectMoveInDirection(GameObj obj, Vector3Int direction)
    {
        foreach (Vector3Int cell in obj.GetOccupiedPositions())
        {
            Vector3Int target   = cell + direction;
            if (!IsValidMoveTarget(target, direction)) return false;
            GameObj    occupant = gameState.GetObjectAt(target);
            if (occupant != null && occupant != obj && occupant.IsAlive()) return false;
        }
        return true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private List<GameObj> GetRidersOf(GameObj obj)
    {
        var riders = new List<GameObj>();
        foreach (Vector3Int cell in obj.GetOccupiedPositions())
        {
            GameObj above = gameState.GetObjectAt(cell + Vector3Int.up);
            if (above != null && above != obj && above.IsAlive() && !riders.Contains(above))
                riders.Add(above);
        }
        return riders;
    }

    private void ResetAllMovementFlags()
    {
        foreach (GameObj obj in gameState.GetAllObjects())
            obj.movement = Vector3Int.zero;
    }

    // ── Step Lifecycle ────────────────────────────────────────────────────────

    private void BeginStep()    { tickCount = 0; currentStep = new StepData(); }
    private TickData NewTick()  { tickCount++; return new TickData(tickCount); }
    private void CommitTick(TickData tick) => currentStep.ticks.Add(tick);

    private StepData FinishStep()
    {
        OnStepResolved?.Invoke(currentStep);
        return currentStep;
    }

    private void RunInitialGravity() { BeginStep(); Step(); FinishStep(); }
}

} // namespace NewArch
