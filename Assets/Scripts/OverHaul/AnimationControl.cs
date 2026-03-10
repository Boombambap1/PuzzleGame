using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NewArch;

namespace NewArch
{

/// <summary>
/// Receives StepData from GamePhysicsV2 and replays it visually, one tick at a time.
/// All visual transforms are driven from here — individual objects do not animate themselves.
///
/// Setup:
///   1. Call RegisterObject() for every game object on scene load.
///   2. This class auto-subscribes to GamePhysicsV2.OnStepResolved.
///   3. Listen to OnStepAnimationComplete to know when all visuals have finished.
///
/// Part of the View layer (MVC).
/// </summary>
public class AnimationControlV2 : MonoBehaviour
{
    // ── Configuration ─────────────────────────────────────────────────────────

    [Header("Timing")]
    [SerializeField] private float animationSpeed          = 10f;
    [SerializeField] private float fallSpeedMultiplier     = 1.5f;
    [SerializeField] private float destroyAnimationDuration = 0.25f;

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Fired when the last tick of a step finishes animating.</summary>
    public static event System.Action OnStepAnimationComplete;

    // ── Private State ─────────────────────────────────────────────────────────

    private Dictionary<GameObj, Transform> objectTransforms = new();
    private bool      isPaused      = false;
    private bool      isAnimating   = false;
    private Coroutine activeRoutine = null;

    // ── Dependencies ──────────────────────────────────────────────────────────

    private AudioControlV2 audioControl;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        audioControl = GetComponent<AudioControlV2>();
    }

    void OnEnable()
    {
        GamePhysicsV2.OnStepResolved += ProcessStepAnimation;
    }

    void OnDisable()
    {
        GamePhysicsV2.OnStepResolved -= ProcessStepAnimation;
    }

    // ── Registration ──────────────────────────────────────────────────────────

    /// <summary>
    /// Register a logical GameObj with its scene Transform.
    /// Must be called before any animation is played for this object.
    /// </summary>
    public void RegisterObject(GameObj obj, Transform visual)
    {
        if (obj == null || visual == null)
        {
            Debug.LogWarning("[AnimationControlV2] RegisterObject called with null argument.");
            return;
        }
        objectTransforms[obj] = visual;
    }

    public void UnregisterObject(GameObj obj) => objectTransforms.Remove(obj);

    /// <summary>
    /// Remove all registered objects at once.
    /// Called by LevelJsonLoaderV2 before destroying visuals on level load/reload
    /// so no stale Transform references remain in the dictionary.
    /// </summary>
    public void ClearAllRegistrations() => objectTransforms.Clear();

    // ── Public Playback API ───────────────────────────────────────────────────

    /// <summary>
    /// Begin animating a full step. Ticks play sequentially;
    /// all movements within one tick animate simultaneously.
    /// </summary>
    public void ProcessStepAnimation(StepData stepData)
    {
        if (stepData == null || stepData.ticks.Count == 0)
        {
            OnStepAnimationComplete?.Invoke();
            return;
        }

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(PlayStepRoutine(stepData));
    }

    /// <summary>
    /// Stop all animations and snap every registered object to its current
    /// logical position (as stored in the GameObj). Called when the player
    /// inputs a new move before the current animation finishes.
    /// </summary>
    public void CancelAnimations()
    {
        if (activeRoutine != null) { StopCoroutine(activeRoutine); activeRoutine = null; }

        // Snap all registered transforms to their logical positions so the
        // next move starts from the correct grid cell, not mid-lerp.
        foreach (KeyValuePair<GameObj, Transform> entry in objectTransforms)
        {
            if (entry.Value == null) continue;
            entry.Value.localScale = Vector3.one;
            entry.Value.position   = (Vector3)entry.Key.position;

            // Ensure visibility matches alive state.
            entry.Value.gameObject.SetActive(entry.Key.IsAlive());
        }

        isAnimating   = false;
        activeRoutine = null;
        // Do NOT fire OnStepAnimationComplete here — the new input will
        // trigger a fresh step which goes through the normal flow.
    }

    public void PauseStepAnimation()  => isPaused = true;
    public void ResumeStepAnimation() => isPaused = false;

    /// <summary>Returns true while any step animation is running.</summary>
    public bool IsAnimating() => isAnimating;

    /// <summary>Returns true when no tick animations are in progress.</summary>
    public bool AreAllTickAnimationsComplete() => !isAnimating;

    // ── Coroutine Sequencer ───────────────────────────────────────────────────

    private IEnumerator PlayStepRoutine(StepData stepData)
    {
        isAnimating = true;

        foreach (TickData tick in stepData.ticks)
        {
            while (isPaused) yield return null;
            yield return StartCoroutine(PlayTickRoutine(tick));
        }

        isAnimating   = false;
        activeRoutine = null;
        OnStepAnimationComplete?.Invoke();
    }

    private IEnumerator PlayTickRoutine(TickData tick)
    {
        var running = new List<Coroutine>();

        foreach (ObjectMovement movement in tick.movements)
        {
            Coroutine c = movement.moveType switch
            {
                MoveType.Walk    => StartCoroutine(AnimateObjectMovement(movement.obj, movement.fromPos, movement.toPos)),
                MoveType.Slide   => StartCoroutine(AnimateObjectMovement(movement.obj, movement.fromPos, movement.toPos)),
                MoveType.Fall    => StartCoroutine(AnimateObjectFall(movement.obj, movement.fromPos, movement.toPos)),
                MoveType.Die     => StartCoroutine(AnimateObjectDestroy(movement.obj)),
                MoveType.Respawn => StartCoroutine(AnimateObjectRespawn(movement.obj, movement.toPos)),
                _                => null
            };
            if (c != null) running.Add(c);
        }

        foreach (Coroutine c in running) yield return c;
    }

    // ── Per-Object Animations ─────────────────────────────────────────────────

    /// <summary>Smoothly move an object's visual from fromPos to toPos.</summary>
    public IEnumerator AnimateObjectMovement(GameObj obj, Vector3Int fromPos, Vector3Int toPos)
    {
        if (!objectTransforms.TryGetValue(obj, out Transform t)) yield break;
        float duration = Vector3Int.Distance(fromPos, toPos) / animationSpeed;
        yield return LerpTransform(t, fromPos, toPos, duration);
    }

    /// <summary>Smoothly drop an object's visual from fromPos to toPos.</summary>
    public IEnumerator AnimateObjectFall(GameObj obj, Vector3Int fromPos, Vector3Int toPos)
    {
        if (!objectTransforms.TryGetValue(obj, out Transform t)) yield break;
        float duration = Vector3Int.Distance(fromPos, toPos) / (animationSpeed * fallSpeedMultiplier);
        yield return LerpTransform(t, fromPos, toPos, duration);
    }

    /// <summary>Shrink and hide an object's visual on death.</summary>
    public IEnumerator AnimateObjectDestroy(GameObj obj)
    {
        if (!objectTransforms.TryGetValue(obj, out Transform t)) yield break;

        audioControl?.PlayDeathSound();

        float   elapsed    = 0f;
        Vector3 startScale = t.localScale;

        while (elapsed < destroyAnimationDuration)
        {
            elapsed      += Time.deltaTime;
            t.localScale  = Vector3.Lerp(startScale, Vector3.zero, Mathf.Clamp01(elapsed / destroyAnimationDuration));
            yield return null;
        }

        t.localScale = Vector3.zero;
        t.gameObject.SetActive(false);
    }

    /// <summary>Snap an object's visual to its respawn position and show it.</summary>
    private IEnumerator AnimateObjectRespawn(GameObj obj, Vector3Int spawnPos)
    {
        if (!objectTransforms.TryGetValue(obj, out Transform t)) yield break;

        // Always force scale back to one — AnimateObjectDestroy may have shrunk it
        // to zero, or CancelAnimations may have interrupted mid-shrink.
        t.localScale          = Vector3.one;
        t.position            = (Vector3)spawnPos;
        t.gameObject.SetActive(true);

        audioControl?.PlayRespawnSound();

        yield return null;
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private IEnumerator LerpTransform(Transform t, Vector3 from, Vector3 to, float duration)
    {
        if (duration <= 0f) { t.position = to; yield break; }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed    += Time.deltaTime;
            t.position  = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        t.position = to;
    }
}

} // namespace NewArch
