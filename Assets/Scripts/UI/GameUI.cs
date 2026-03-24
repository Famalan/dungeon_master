using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject hudPanel;
    public GameObject gameOverPanel;
    public GameObject optionsPanel;
    public GameObject perkPanel;

    [Header("HUD Elements")]
    public Slider healthBar;
    public Text levelText;
    public Text healthText;
    public Text coinsText;
    public Text dashCooldownText;
    public Text weaponModeText;
    public Text sealNotificationText;
    public Image lowHPVignette;

    [Header("Game Over Elements")]
    public Text gameOverLevelText;

    [Header("Options Elements")]
    public Slider sensitivitySlider;
    public Slider volumeSlider;
    public Text sensitivityValueText;
    public Text volumeValueText;

    [Header("Perk Elements")]
    public Text perkTitleText;
    public Text perkShopCoinsText;
    public Button[] perkButtons;
    public Text[] perkButtonTexts;

    [Header("References")]
    public PerkSystem perkSystem;
    public PlayerController playerRef;
    public HealthSystem playerHealthRef;
    public PlayerStats playerStatsRef;

    PerkData[] currentPerks;
    float sealNotificationTimer;
    int lastKnownLevel;

    void OnEnable()
    {
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
        GameEvents.OnLevelStarted += HandleLevelStarted;
        GameEvents.OnCoinsChanged += UpdateCoins;
        GameEvents.OnSealNotification += ShowSealNotification;
        GameEvents.OnWeaponModeChanged += HandleWeaponModeChanged;
    }

    void OnDisable()
    {
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        GameEvents.OnLevelStarted -= HandleLevelStarted;
        GameEvents.OnCoinsChanged -= UpdateCoins;
        GameEvents.OnSealNotification -= ShowSealNotification;
        GameEvents.OnWeaponModeChanged -= HandleWeaponModeChanged;
    }

    void Start()
    {
        WireButtons();
        SetupSliders();
        ApplyVisualTheme();

        if (playerHealthRef != null)
        {
            playerHealthRef.onHealthChanged.AddListener(UpdateHealth);
        }
    }

    void OnDestroy()
    {
        if (playerHealthRef != null)
        {
            playerHealthRef.onHealthChanged.RemoveListener(UpdateHealth);
        }
    }

    void Update()
    {
        UpdateDashCooldownUI();
        UpdateWeaponModeHud();
        UpdateSealNotification();
        UpdateLowHPVignette();
    }

    void HandleWeaponModeChanged()
    {
        UpdateWeaponModeHud();
    }

    void UpdateWeaponModeHud()
    {
        if (weaponModeText == null) return;
        if (playerRef == null || playerRef.shooter == null) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
        {
            return;
        }

        weaponModeText.text = playerRef.shooter.GetWeaponHudLine();
    }

    void HandleGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.MainMenu:
                ShowMainMenu();
                break;
            case GameState.Playing:
                ShowHUD(lastKnownLevel);
                break;
            case GameState.PerkSelection:
                ShowPerkSelection(lastKnownLevel);
                break;
            case GameState.GameOver:
                ShowGameOver(lastKnownLevel);
                break;
        }
    }

    void HandleLevelStarted(int level)
    {
        lastKnownLevel = level;
        ShowHUD(level);

        if (playerStatsRef != null)
        {
            UpdateCoins(playerStatsRef.coins);
        }

        UpdateWeaponModeHud();
    }

    void UpdateDashCooldownUI()
    {
        if (dashCooldownText == null) return;
        if (playerRef == null) return;

        if (playerRef.dashCooldownTimer > 0f)
        {
            dashCooldownText.gameObject.SetActive(true);
            dashCooldownText.text = "DASH: " + playerRef.dashCooldownTimer.ToString("F1") + "s";
            dashCooldownText.color = Color.gray;
        }
        else
        {
            dashCooldownText.gameObject.SetActive(true);
            dashCooldownText.text = "DASH: READY [Q]";
            dashCooldownText.color = Color.cyan;
        }
    }

    void UpdateSealNotification()
    {
        if (sealNotificationText == null) return;

        if (sealNotificationTimer > 0f)
        {
            sealNotificationTimer -= Time.deltaTime;
            float alpha = Mathf.Clamp01(sealNotificationTimer);
            Color c = sealNotificationText.color;
            c.a = alpha;
            sealNotificationText.color = c;
        }
        else
        {
            sealNotificationText.gameObject.SetActive(false);
        }
    }

    void UpdateLowHPVignette()
    {
        if (lowHPVignette == null) return;
        if (playerHealthRef == null) return;

        float healthPercent = (float)playerHealthRef.currentHealth / playerHealthRef.maxHealth;
        if (healthPercent < 0.3f)
        {
            float intensity = Mathf.Lerp(0.6f, 0f, healthPercent / 0.3f);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3f);
            lowHPVignette.gameObject.SetActive(true);
            Color c = new Color(0.8f, 0f, 0f, intensity * pulse);
            lowHPVignette.color = c;
        }
        else
        {
            lowHPVignette.gameObject.SetActive(false);
        }
    }

    public void ShowSealNotification(string message)
    {
        if (sealNotificationText == null) return;
        sealNotificationText.gameObject.SetActive(true);
        sealNotificationText.text = message;
        sealNotificationText.color = (message.Contains("SEALED")) ?
            new Color(1f, 0.3f, 0.3f, 1f) : new Color(0.3f, 1f, 0.3f, 1f);
        sealNotificationTimer = 2f;
    }

    void WireButtons()
    {
        if (mainMenuPanel != null)
        {
            WireButton(mainMenuPanel, "StartButton", OnStartButtonClicked);
            WireButton(mainMenuPanel, "OptionsButton", OnOptionsButtonClicked);
            WireButton(mainMenuPanel, "QuitButton", OnQuitButtonClicked);
        }
        if (gameOverPanel != null)
        {
            WireButton(gameOverPanel, "RestartButton", OnRestartButtonClicked);
            WireButton(gameOverPanel, "QuitButton2", OnQuitButtonClicked);
        }
        if (optionsPanel != null)
        {
            WireButton(optionsPanel, "BackButton", OnOptionsBackClicked);
        }

        if (perkPanel != null)
        {
            WireButton(perkPanel, "RerollPerksButton", OnRerollPerksClicked);
            WireButton(perkPanel, "BuyHealButton", OnBuyHealClicked);
        }
    }

    void SetupSliders()
    {
        // Тексты значений создаются с широким Rect (CreateUIText 400×60) и по умолчанию
        // перехватывают raycast поверх слайдеров — ползунок не получает клики.
        if (sensitivityValueText != null)
        {
            sensitivityValueText.raycastTarget = false;
            RectTransform srt = sensitivityValueText.GetComponent<RectTransform>();
            if (srt != null)
                srt.sizeDelta = new Vector2(72f, 28f);
        }

        if (volumeValueText != null)
        {
            volumeValueText.raycastTarget = false;
            RectTransform vrt = volumeValueText.GetComponent<RectTransform>();
            if (vrt != null)
                vrt.sizeDelta = new Vector2(72f, 28f);
        }

        if (sensitivitySlider != null)
        {
            float savedSens = PlayerPrefs.GetFloat("MouseSensitivity", 2f);
            sensitivitySlider.minValue = 0.5f;
            sensitivitySlider.maxValue = 10f;
            sensitivitySlider.value = savedSens;
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            UpdateSensitivityText(savedSens);
        }

        if (volumeSlider != null)
        {
            float savedVol = PlayerPrefs.GetFloat("Volume", 1f);
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = savedVol;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            AudioListener.volume = savedVol;
            UpdateVolumeText(savedVol);
        }
    }

    void OnSensitivityChanged(float value)
    {
        PlayerPrefs.SetFloat("MouseSensitivity", value);
        UpdateSensitivityText(value);

        if (playerRef != null)
        {
            playerRef.mouseSensitivity = value;
        }
    }

    void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("Volume", value);
        UpdateVolumeText(value);
    }

    void UpdateSensitivityText(float value)
    {
        if (sensitivityValueText != null)
        {
            sensitivityValueText.text = value.ToString("F1");
        }
    }

    void UpdateVolumeText(float value)
    {
        if (volumeValueText != null)
        {
            volumeValueText.text = Mathf.RoundToInt(value * 100) + "%";
        }
    }

    void WireButton(GameObject panel, string buttonName, UnityEngine.Events.UnityAction action)
    {
        Transform btnTransform = panel.transform.Find(buttonName);
        if (btnTransform == null) return;

        Button btn = btnTransform.GetComponent<Button>();
        if (btn == null) return;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }

    public void ShowMainMenu()
    {
        HideAll();
        SetPanelActive(mainMenuPanel, true);
    }

    public void ShowHUD(int level)
    {
        HideAll();
        SetPanelActive(hudPanel, true);

        if (levelText != null)
        {
            levelText.text = "Level: " + level;
        }

        if (playerStatsRef != null)
        {
            UpdateCoins(playerStatsRef.coins);
        }

        UpdateWeaponModeHud();
    }

    public void ShowGameOver(int reachedLevel)
    {
        HideAll();
        SetPanelActive(gameOverPanel, true);

        if (gameOverLevelText != null)
        {
            gameOverLevelText.text = "You reached level " + reachedLevel;
        }
    }

    public void ShowPerkSelection(int level)
    {
        HideAll();
        SetPanelActive(perkPanel, true);

        if (perkTitleText != null)
        {
            perkTitleText.text = "LEVEL " + level + " - CHOOSE A PERK";
        }

        if (perkSystem != null)
        {
            currentPerks = perkSystem.GetRandomPerks(3);
            SetupPerkButtons();
        }

        RefreshPerkShopUI();
    }

    void SetupPerkButtons()
    {
        if (currentPerks == null) return;

        for (int i = 0; i < 3; i++)
        {
            if (perkButtons != null && i < perkButtons.Length && perkButtons[i] != null)
            {
                perkButtons[i].onClick.RemoveAllListeners();
                int perkIndex = i;
                perkButtons[i].onClick.AddListener(() => OnPerkClicked(perkIndex));
            }

            if (perkButtonTexts != null && i < perkButtonTexts.Length && perkButtonTexts[i] != null)
            {
                if (i < currentPerks.Length && currentPerks[i] != null)
                {
                    perkButtonTexts[i].text = currentPerks[i].Name + "\n" + currentPerks[i].Description;
                }
            }
        }
    }

    void OnPerkClicked(int index)
    {
        if (currentPerks == null || index >= currentPerks.Length) return;

        PerkData chosen = currentPerks[index];
        if (perkSystem != null && playerStatsRef != null)
        {
            perkSystem.ApplyPerk(chosen, playerStatsRef);
        }

        GameEvents.FireRequestPerkChosen();
    }

    public void UpdateHealth(int current, int max)
    {
        if (healthBar != null)
        {
            healthBar.maxValue = max;
            healthBar.value = current;
        }

        if (healthText != null)
        {
            healthText.text = current + " / " + max;
        }
    }

    public void UpdateCoins(int amount)
    {
        if (coinsText != null)
        {
            coinsText.text = "◆ " + amount.ToString();
        }

        RefreshPerkShopUI();
    }

    void RefreshPerkShopUI()
    {
        if (perkPanel == null || !perkPanel.activeInHierarchy) return;

        if (perkShopCoinsText != null && playerStatsRef != null)
        {
            perkShopCoinsText.text = "Монеты: ◆ " + playerStatsRef.coins.ToString();
        }

        bool canReroll = playerStatsRef != null && playerStatsRef.coins >= 8;
        bool canHeal = playerStatsRef != null && playerStatsRef.coins >= 5 &&
            playerHealthRef != null && playerHealthRef.currentHealth < playerHealthRef.maxHealth;

        SetPerkShopButtonInteractable("RerollPerksButton", canReroll);
        SetPerkShopButtonInteractable("BuyHealButton", canHeal);
    }

    void SetPerkShopButtonInteractable(string childName, bool interactable)
    {
        if (perkPanel == null) return;
        Transform t = perkPanel.transform.Find(childName);
        if (t == null) return;
        Button btn = t.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = interactable;
        }
    }

    void OnRerollPerksClicked()
    {
        if (playerStatsRef == null || perkSystem == null) return;
        if (!playerStatsRef.TrySpendCoins(8)) return;

        currentPerks = perkSystem.GetRandomPerks(3);
        SetupPerkButtons();
        RefreshPerkShopUI();
    }

    void OnBuyHealClicked()
    {
        if (playerStatsRef == null || playerHealthRef == null) return;
        if (!playerStatsRef.TrySpendCoins(5)) return;

        int need = playerHealthRef.maxHealth - playerHealthRef.currentHealth;
        if (need > 0)
        {
            playerHealthRef.Heal(need);
        }

        RefreshPerkShopUI();
    }

    void OnStartButtonClicked()
    {
        GameEvents.FireRequestStartGame();
    }

    void OnOptionsButtonClicked()
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(optionsPanel, true);
    }

    void OnOptionsBackClicked()
    {
        PlayerPrefs.Save();
        SetPanelActive(optionsPanel, false);
        SetPanelActive(mainMenuPanel, true);
    }

    void OnRestartButtonClicked()
    {
        GameEvents.FireRequestRestartGame();
    }

    void OnQuitButtonClicked()
    {
        GameEvents.FireRequestQuitGame();
    }

    /// <summary>
    /// Единая палитра из docs/superpowers/specs/2026-03-23-visual-polish-design.md (меню + HUD).
    /// </summary>
    void ApplyVisualTheme()
    {
        Color menuBackdrop = new Color(0.07f, 0.048f, 0.11f, 0.93f);
        Color titleCream = new Color(1f, 0.93f, 0.78f, 1f);
        Color bodyMuted = new Color(0.72f, 0.68f, 0.78f, 1f);

        ApplyMenuPanelBackdrop(mainMenuPanel, menuBackdrop);
        ApplyMenuPanelBackdrop(optionsPanel, menuBackdrop);
        ApplyMenuPanelBackdrop(gameOverPanel, menuBackdrop);
        ApplyMenuPanelBackdrop(perkPanel, new Color(0.06f, 0.04f, 0.1f, 0.94f));

        StyleButtonsUnder(mainMenuPanel);
        StyleButtonsUnder(optionsPanel);
        StyleButtonsUnder(gameOverPanel);
        StyleButtonsUnder(perkPanel);

        StyleTextByName(mainMenuPanel, "Title", titleCream, 44, FontStyle.Bold);
        StyleTextByName(gameOverPanel, "GameOverTitle", titleCream, 52, FontStyle.Bold);
        StyleTextByName(optionsPanel, "OptionsTitle", titleCream, 44, FontStyle.Bold);
        if (perkTitleText != null)
        {
            perkTitleText.color = titleCream;
            perkTitleText.fontSize = 40;
            perkTitleText.fontStyle = FontStyle.Bold;
        }

        StyleTextByName(optionsPanel, "SensLabel", bodyMuted, 22, FontStyle.Normal);
        StyleTextByName(optionsPanel, "VolLabel", bodyMuted, 22, FontStyle.Normal);
        StyleTextByName(gameOverPanel, "LevelReached", bodyMuted, 26, FontStyle.Normal);

        if (gameOverLevelText != null)
        {
            gameOverLevelText.color = bodyMuted;
            gameOverLevelText.fontSize = 26;
        }

        if (healthBar != null)
        {
            RectTransform hbRt = healthBar.GetComponent<RectTransform>();
            if (hbRt != null)
            {
                hbRt.sizeDelta = new Vector2(240f, 24f);
            }

            Transform bgTr = healthBar.transform.Find("Background");
            if (bgTr != null && bgTr.TryGetComponent<Image>(out var bgImg))
            {
                bgImg.color = new Color(0.1f, 0.08f, 0.12f, 0.92f);
            }

            if (healthBar.fillRect != null && healthBar.fillRect.TryGetComponent<Image>(out var fillImg))
            {
                fillImg.color = new Color(0.95f, 0.2f, 0.32f, 1f);
            }
        }

        if (healthText != null)
        {
            healthText.color = new Color(0.98f, 0.9f, 0.9f, 1f);
            healthText.fontSize = 20;
            healthText.fontStyle = FontStyle.Bold;
        }

        if (levelText != null)
        {
            levelText.color = new Color(0.52f, 0.48f, 0.58f, 1f);
            levelText.fontSize = 22;
        }

        if (coinsText != null)
        {
            coinsText.color = new Color(1f, 0.84f, 0.22f, 1f);
            coinsText.fontSize = 30;
            coinsText.fontStyle = FontStyle.Bold;
        }

        if (dashCooldownText != null)
        {
            dashCooldownText.color = new Color(0.42f, 0.68f, 0.76f, 1f);
        }

        if (weaponModeText != null)
        {
            weaponModeText.color = new Color(0.5f, 0.54f, 0.64f, 1f);
        }

        if (sealNotificationText != null)
        {
            sealNotificationText.fontSize = 30;
            sealNotificationText.fontStyle = FontStyle.Bold;
        }
    }

    static void ApplyMenuPanelBackdrop(GameObject panel, Color backdrop)
    {
        if (panel == null) return;
        if (!panel.TryGetComponent<Image>(out var img)) return;
        img.color = backdrop;
    }

    static void StyleButtonsUnder(GameObject root)
    {
        if (root == null) return;
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        ColorBlock cb = new ColorBlock
        {
            normalColor = new Color(0.19f, 0.14f, 0.27f, 1f),
            highlightedColor = new Color(0.34f, 0.24f, 0.46f, 1f),
            pressedColor = new Color(0.12f, 0.09f, 0.17f, 1f),
            selectedColor = new Color(0.34f, 0.24f, 0.46f, 1f),
            disabledColor = new Color(0.14f, 0.14f, 0.14f, 0.45f),
            colorMultiplier = 1f,
            fadeDuration = 0.12f
        };

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].colors = cb;
            Graphic g = buttons[i].targetGraphic;
            if (g != null)
            {
                g.color = Color.white;
            }

            Transform textTr = buttons[i].transform.Find("Text");
            if (textTr != null && textTr.TryGetComponent<Text>(out var txt))
            {
                txt.color = new Color(0.96f, 0.92f, 0.98f, 1f);
                txt.fontStyle = FontStyle.Bold;
            }
        }
    }

    static void StyleTextByName(GameObject root, string childName, Color color, int fontSize, FontStyle style)
    {
        if (root == null) return;
        Transform t = root.transform.Find(childName);
        if (t == null) return;
        if (!t.TryGetComponent<Text>(out var txt)) return;
        txt.color = color;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
    }

    void HideAll()
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(hudPanel, false);
        SetPanelActive(gameOverPanel, false);
        SetPanelActive(optionsPanel, false);
        SetPanelActive(perkPanel, false);
    }

    void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }
}
