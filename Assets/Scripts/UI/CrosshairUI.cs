using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    [Header("Crosshair")]
    public float size = 20f;
    public float thickness = 2f;
    public float gap = 4f;
    public Color crosshairColor = Color.white;

    void OnGUI()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
            return;

        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f;

        GUI.color = crosshairColor;

        // Top line
        GUI.DrawTexture(new Rect(centerX - thickness / 2, centerY - gap - size, thickness, size), Texture2D.whiteTexture);
        // Bottom line
        GUI.DrawTexture(new Rect(centerX - thickness / 2, centerY + gap, thickness, size), Texture2D.whiteTexture);
        // Left line
        GUI.DrawTexture(new Rect(centerX - gap - size, centerY - thickness / 2, size, thickness), Texture2D.whiteTexture);
        // Right line
        GUI.DrawTexture(new Rect(centerX + gap, centerY - thickness / 2, size, thickness), Texture2D.whiteTexture);

        // Center dot
        GUI.DrawTexture(new Rect(centerX - 1, centerY - 1, 2, 2), Texture2D.whiteTexture);
    }
}
