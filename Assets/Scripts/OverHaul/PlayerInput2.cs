using UnityEngine;
using NewArch;

namespace NewArch
{

/// <summary>
/// Reads player keyboard input and drives GamePhysicsV2.
/// Handles auto-respawn and win-condition checks, both gated on
/// AnimationControlV2.OnStepAnimationComplete.
///
/// This script only asks "is it my turn?" and sends directions.
/// It knows nothing about how physics or animations work internally.
///
/// Part of the Controller layer (MVC).
/// </summary>
public class InputProcessingV2 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CameraController cameraController;

    [Header("Input Settings")]
    [SerializeField] private float inputCooldown = 0.1f;

    // ── Dependencies ──────────────────────────────────────────────────────────

    private GamePhysicsV2    gamePhysics;
    private GameStateV2      gameState;
    private AnimationControlV2 animationControl;

    // ── Runtime State ─────────────────────────────────────────────────────────

    private bool  inputAllowed      = true;
    private float lastInputTime     = float.NegativeInfinity;
    private int   respawnAttempts   = 0;
    private const int MaxRespawnAttempts = 5;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        gamePhysics      = FindFirstObjectByType<GamePhysicsV2>();
        gameState        = FindFirstObjectByType<GameStateV2>();
        animationControl = FindFirstObjectByType<AnimationControlV2>();

        if (gamePhysics      == null) Debug.LogError("[InputProcessingV2] GamePhysicsV2 not found in scene.");
        if (gameState        == null) Debug.LogError("[InputProcessingV2] GameStateV2 not found in scene.");
        if (animationControl == null) Debug.LogError("[InputProcessingV2] AnimationControlV2 not found in scene.");
    }

    void OnEnable()
    {
        AnimationControlV2.OnStepAnimationComplete += HandleAnimationsComplete;
    }

    void OnDisable()
    {
        AnimationControlV2.OnStepAnimationComplete -= HandleAnimationsComplete;
    }

    // ── Update ────────────────────────────────────────────────────────────────

    void Update()
    {
        if (Time.time - lastInputTime < inputCooldown) return;

        int dirIndex = ReadDirectionIndex();
        if (dirIndex < 0) return;

        // If an animation is currently playing, snap it and proceed with the new move.
        // If input is locked for any other reason (respawn in progress, win screen), ignore.
        if (!inputAllowed)
        {
            if (animationControl.IsAnimating())
                animationControl.CancelAnimations();
            else
                return;
        }

        lastInputTime = Time.time;
        inputAllowed  = false;  // Tentatively lock — re-enable below if step was rejected.

        Vector3Int direction = GetCameraRelativeDirection(dirIndex);
        StepData   result    = gamePhysics.StartStep(direction);

        // StartStep returns null when the move is invalid (into a wall, player dead, etc).
        // In that case OnStepResolved never fires, so OnStepAnimationComplete never fires,
        // so we must re-enable input here or the game freezes permanently.
        if (result == null)
            inputAllowed = true;
    }

    // ── Event Handlers ────────────────────────────────────────────────────────

    /// <summary>
    /// Called after every step's animations finish.
    /// Decides whether to trigger respawn, check win, or re-enable input.
    /// </summary>
    private void HandleAnimationsComplete()
    {
        // Priority 1: pending respawn.
        if (gamePhysics.NeedsRespawn())
        {
            respawnAttempts++;

            if (respawnAttempts > MaxRespawnAttempts)
            {
                // Guard against infinite respawn loop — NeedsRespawn() keeps
                // returning true but ProcessRespawning isn't resolving it.
                // This means a spawn point is missing or the object has no prefab.
                Debug.LogError("[InputProcessingV2] Respawn loop detected — giving up after " +
                               MaxRespawnAttempts + " attempts. Check spawn points are registered.");
                respawnAttempts = 0;
                inputAllowed    = true;  // Unlock so the game isn't permanently frozen.
                return;
            }

            gamePhysics.StartStep(Vector3Int.zero);
            return;
        }

        // Respawn resolved cleanly — reset counter.
        respawnAttempts = 0;

        // Priority 2: win condition.
        if (gameState.IsWinningState())
            return;  // Win menu shown inside IsWinningState; keep input locked.

        // Otherwise unlock input.
        inputAllowed = true;
    }

    // ── Input Reading ─────────────────────────────────────────────────────────

    private int ReadDirectionIndex()
    {
        if (Input.GetKeyDown(KeyCode.W)) return 0;
        if (Input.GetKeyDown(KeyCode.D)) return 1;
        if (Input.GetKeyDown(KeyCode.S)) return 2;
        if (Input.GetKeyDown(KeyCode.A)) return 3;
        return -1;
    }

    private Vector3Int GetCameraRelativeDirection(int dirIndex)
    {
        Vector3Int[] dirs = { Vector3Int.back, Vector3Int.left, Vector3Int.forward, Vector3Int.right };
        if (cameraController == null) return dirs[dirIndex];

        int yawSteps = Mathf.RoundToInt(cameraController.GetYawRotation() / 90f);
        return dirs[(dirIndex + yawSteps + 4) % 4];
    }
}

} // namespace NewArch
