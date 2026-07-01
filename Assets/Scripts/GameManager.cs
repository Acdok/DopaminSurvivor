using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Owns the prototype-level play/game-over state without depending on future UI or controller scripts.
/// </summary>
[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    private enum GameState
    {
        Playing,
        GameOver
    }

    [Header("Required References")]
    [SerializeField]
    private Health playerHealth;

    [Header("Game Over Input")]
    [SerializeField]
    private KeyCode restartKey = KeyCode.R;

    [SerializeField]
    private KeyCode quitKey = KeyCode.Escape;

    private GameState state = GameState.Playing;
    private bool warnedMissingPlayerHealth;

    /// <summary>
    /// Raised once when the manager transitions from playing to game over.
    /// </summary>
    public event Action GameOver;

    /// <summary>
    /// Public state check for controllers, UI, and other future systems.
    /// </summary>
    public bool IsGameOver => state == GameState.GameOver;

    /// <summary>
    /// Public state check for systems that should run only during active play.
    /// </summary>
    public bool IsPlaying => state == GameState.Playing;

    private void Awake()
    {
        state = GameState.Playing;
    }

    private void OnEnable()
    {
        // Pair subscription with OnDisable so disabled managers and scene reloads do not leave stale handlers.
        SubscribeToPlayerHealth();
    }

    private void Start()
    {
        WarnIfPlayerHealthMissing();
    }

    private void OnDisable()
    {
        UnsubscribeFromPlayerHealth();
    }

    private void Update()
    {
        if (!IsGameOver)
        {
            return;
        }

        if (WasKeyPressed(restartKey))
        {
            RestartCurrentScene();
            return;
        }

        if (WasKeyPressed(quitKey))
        {
            QuitGame();
        }
    }

    /// <summary>
    /// Allows other components or debug tools to force the same single game-over transition.
    /// </summary>
    public void TriggerGameOver()
    {
        if (IsGameOver)
        {
            return;
        }

        state = GameState.GameOver;
        GameOver?.Invoke();
    }

    /// <summary>
    /// Reloads the active scene to reset prototype runtime state.
    /// </summary>
    public void RestartCurrentScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(activeScene.buildIndex);
            return;
        }

        SceneManager.LoadScene(activeScene.name);
    }

    /// <summary>
    /// Quits play mode in the editor and the application in builds.
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SubscribeToPlayerHealth()
    {
        if (playerHealth == null)
        {
            return;
        }

        playerHealth.Died += HandlePlayerDied;
    }

    private void UnsubscribeFromPlayerHealth()
    {
        if (playerHealth == null)
        {
            return;
        }

        playerHealth.Died -= HandlePlayerDied;
    }

    private void HandlePlayerDied(Health health)
    {
        TriggerGameOver();
    }

    private void WarnIfPlayerHealthMissing()
    {
        if (playerHealth != null || warnedMissingPlayerHealth)
        {
            return;
        }

        warnedMissingPlayerHealth = true;
        Debug.LogWarning($"{nameof(GameManager)} on {name} has no Player Health assigned.", this);
    }

    private static bool WasKeyPressed(KeyCode keyCode)
    {
        if (keyCode == KeyCode.None)
        {
            return false;
        }

        // 프로젝트 규칙에 따라 게임오버 입력도 Old Input으로만 처리한다.
        return Input.GetKeyDown(keyCode);
    }
}
