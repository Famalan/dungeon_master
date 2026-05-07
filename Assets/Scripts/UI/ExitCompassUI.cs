using UnityEngine;
using UnityEngine.UI;

public class ExitCompassUI : MonoBehaviour
{
    public GameObject compassPanel;
    public RectTransform arrowRoot;
    public float arrowSpriteAngleOffsetDegrees = 180f;

    Transform playerTransform;
    DungeonBuilder dungeonBuilder;

    void OnEnable()
    {
        GameEvents.OnLevelGenerated += RefreshReferences;
        GameEvents.OnGameStateChanged += HandleState;
    }

    void OnDisable()
    {
        GameEvents.OnLevelGenerated -= RefreshReferences;
        GameEvents.OnGameStateChanged -= HandleState;
    }

    void Start()
    {
        RefreshReferences();
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            HandleState(gm.CurrentState);
        }
    }

    void HandleState(GameState state)
    {
        bool show = state == GameState.Playing;
        if (compassPanel != null)
        {
            compassPanel.SetActive(show);
        }
    }

    void RefreshReferences()
    {
        dungeonBuilder = Object.FindAnyObjectByType<DungeonBuilder>();
        PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            playerTransform = pc.transform;
        }
    }

    void LateUpdate()
    {
        if (arrowRoot == null || !arrowRoot.gameObject.activeInHierarchy)
        {
            return;
        }
        if (playerTransform == null || dungeonBuilder == null)
        {
            return;
        }

        Vector3 toExit = dungeonBuilder.ExitPosition - playerTransform.position;
        toExit.y = 0f;
        if (toExit.sqrMagnitude < 0.01f)
        {
            return;
        }

        float angleToExit = Mathf.Atan2(toExit.x, toExit.z) * Mathf.Rad2Deg;
        float playerYaw = playerTransform.eulerAngles.y;
        float relative = Mathf.DeltaAngle(playerYaw, angleToExit);
        arrowRoot.localEulerAngles = new Vector3(0f, 0f, -relative + arrowSpriteAngleOffsetDegrees);
    }
}
