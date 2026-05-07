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
    public Image damageFlashImage;
    public Text hintText;

    [Header("UI Asset Theme")]
    public Font uiFont;
    public Sprite panelSprite;
    public Sprite buttonSprite;
    public Sprite compassArrowSprite;

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
    public Text[] perkButtonIconTexts;

    [Header("References")]
    public PerkSystem perkSystem;
    public ArtifactSystem artifactSystem;
    public PlayerController playerRef;
    public HealthSystem playerHealthRef;
    public PlayerStats playerStatsRef;

    PerkData[] currentPerks;
    ArtifactData[] currentArtifacts;
    float sealNotificationTimer;
    float damageFlashTimer;
    float hintTimer;
    int lastKnownLevel;
    int lastHealth = -1;
    Image hintBackplateImage;

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
        ResolveRuntimeReferences();
        WireButtons();
        SetupSliders();
        EnsureRuntimeHudElements();
        ApplyVisualTheme();

        if (playerHealthRef != null)
        {
            playerHealthRef.onHealthChanged.AddListener(UpdateHealth);
            lastHealth = playerHealthRef.currentHealth;
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
        UpdateDamageFlash();
        UpdateHint();
    }

    void ResolveRuntimeReferences()
    {
        if (artifactSystem == null)
        {
            artifactSystem = Object.FindAnyObjectByType<ArtifactSystem>();
        }

        if (artifactSystem == null && perkSystem != null)
        {
            artifactSystem = perkSystem.gameObject.AddComponent<ArtifactSystem>();
        }

        if (playerStatsRef == null)
        {
            playerStatsRef = Object.FindAnyObjectByType<PlayerStats>();
        }

        if (playerHealthRef == null)
        {
            playerHealthRef = Object.FindAnyObjectByType<HealthSystem>();
        }
    }

    void HandleWeaponModeChanged()
    {
        UpdateWeaponModeHud();
    }

    void UpdateWeaponModeHud()
    {
        if (weaponModeText == null) return;
        if (playerRef == null) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
        {
            return;
        }

        if (playerRef.meleeWeapon != null)
        {
            weaponModeText.text = playerRef.meleeWeapon.GetWeaponHudLine();
        }
        else if (playerRef.shooter != null)
        {
            weaponModeText.text = playerRef.shooter.GetWeaponHudLine();
        }
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
            dashCooldownText.text = "DASH  " + playerRef.dashCooldownTimer.ToString("F1") + "s";
            dashCooldownText.color = new Color(0.58f, 0.56f, 0.64f, 1f);
        }
        else
        {
            dashCooldownText.gameObject.SetActive(true);
            dashCooldownText.text = "DASH  READY  [Q]";
            dashCooldownText.color = new Color(0.38f, 0.96f, 0.82f, 1f);
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

    void UpdateDamageFlash()
    {
        if (damageFlashImage == null) return;

        if (damageFlashTimer > 0f)
        {
            damageFlashTimer -= Time.deltaTime;
            float alpha = Mathf.Clamp01(damageFlashTimer / 0.35f) * 0.42f;
            damageFlashImage.gameObject.SetActive(true);
            damageFlashImage.color = new Color(0.95f, 0.05f, 0.04f, alpha);
        }
        else
        {
            damageFlashImage.gameObject.SetActive(false);
        }
    }

    void TriggerDamageFlash()
    {
        damageFlashTimer = 0.35f;
    }

    void ShowHint(string message, float duration)
    {
        if (hintText == null) return;

        hintText.text = message;
        hintText.gameObject.SetActive(true);
        hintText.color = new Color(0.95f, 0.9f, 0.74f, 1f);
        if (hintBackplateImage != null)
        {
            hintBackplateImage.gameObject.SetActive(true);
            hintBackplateImage.color = new Color(0.055f, 0.045f, 0.065f, 0.68f);
        }
        hintTimer = duration;
    }

    void UpdateHint()
    {
        if (hintText == null) return;

        if (hintTimer > 0f)
        {
            hintTimer -= Time.deltaTime;
            Color c = hintText.color;
            c.a = Mathf.Clamp01(hintTimer);
            hintText.color = c;
            if (hintBackplateImage != null)
            {
                Color bg = hintBackplateImage.color;
                bg.a = Mathf.Clamp01(hintTimer) * 0.68f;
                hintBackplateImage.color = bg;
            }
        }
        else
        {
            hintText.gameObject.SetActive(false);
            if (hintBackplateImage != null)
            {
                hintBackplateImage.gameObject.SetActive(false);
            }
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
        btn.onClick.AddListener(() =>
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUIClick();
            }
            action();
        });
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

        if (level == 1)
        {
            ShowHint("WASD - движение  |  ЛКМ - атака  |  Q - рывок  |  стрелка ведет к выходу", 7f);
        }
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
            perkTitleText.text = "LEVEL " + level + " - CHOOSE AN ARTIFACT";
        }

        if (artifactSystem != null)
        {
            currentArtifacts = artifactSystem.GetChoices(3);
            SetupArtifactButtons();
        }
        else if (perkSystem != null)
        {
            currentPerks = perkSystem.GetRandomPerks(3);
            SetupPerkButtons();
        }

        RefreshPerkShopUI();
        ShowHint("Matching artifact tags unlock 2/4 and 4/4 synergies.", 6f);
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

    void SetupArtifactButtons()
    {
        if (currentArtifacts == null) return;

        for (int i = 0; i < 3; i++)
        {
            if (perkButtons != null && i < perkButtons.Length && perkButtons[i] != null)
            {
                perkButtons[i].onClick.RemoveAllListeners();
                int artifactIndex = i;
                perkButtons[i].onClick.AddListener(() => OnArtifactClicked(artifactIndex));

                Image img = perkButtons[i].GetComponent<Image>();
                if (img != null && i < currentArtifacts.Length && currentArtifacts[i] != null)
                {
                    img.color = Color.Lerp(new Color(0.13f, 0.08f, 0.14f, 1f), currentArtifacts[i].color, 0.58f);

                    Transform strip = perkButtons[i].transform.Find("ArtifactAccentStrip");
                    if (strip == null)
                    {
                        GameObject stripObj = new GameObject("ArtifactAccentStrip");
                        stripObj.transform.SetParent(perkButtons[i].transform, false);
                        RectTransform stripRt = stripObj.AddComponent<RectTransform>();
                        stripRt.anchorMin = new Vector2(0f, 0f);
                        stripRt.anchorMax = new Vector2(0f, 1f);
                        stripRt.pivot = new Vector2(0f, 0.5f);
                        stripRt.anchoredPosition = Vector2.zero;
                        stripRt.sizeDelta = new Vector2(8f, 0f);
                        strip = stripObj.transform;
                    }

                    Image stripImage = strip.GetComponent<Image>();
                    if (stripImage == null)
                    {
                        stripImage = strip.gameObject.AddComponent<Image>();
                    }
                    stripImage.color = Color.Lerp(currentArtifacts[i].color, Color.white, 0.18f);
                    stripImage.raycastTarget = false;

                    Text iconText = GetOrCreateArtifactIconText(perkButtons[i].transform);
                    if (perkButtonIconTexts != null && i < perkButtonIconTexts.Length)
                    {
                        perkButtonIconTexts[i] = iconText;
                    }

                    iconText.text = currentArtifacts[i].iconGlyph;
                    iconText.color = Color.Lerp(currentArtifacts[i].color, Color.white, 0.35f);
                }
            }

            if (perkButtonTexts != null && i < perkButtonTexts.Length && perkButtonTexts[i] != null)
            {
                if (i < currentArtifacts.Length && currentArtifacts[i] != null)
                {
                    ArtifactData artifact = currentArtifacts[i];
                    perkButtonTexts[i].text =
                        artifact.displayName + "\n" +
                        artifact.rarity + " | " + artifact.tag + " synergy\n" +
                        artifact.description;
                    perkButtonTexts[i].color = Color.Lerp(Color.white, artifact.color, 0.24f);
                }
            }
        }
    }

    Text GetOrCreateArtifactIconText(Transform buttonRoot)
    {
        Transform existing = buttonRoot.Find("ArtifactIcon");
        if (existing != null)
        {
            Text text = existing.GetComponent<Text>();
            if (text != null) return text;
        }

        GameObject iconObj = new GameObject("ArtifactIcon");
        iconObj.transform.SetParent(buttonRoot, false);
        RectTransform rt = iconObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(30f, -28f);
        rt.sizeDelta = new Vector2(38f, 38f);

        Text icon = iconObj.AddComponent<Text>();
        icon.alignment = TextAnchor.MiddleCenter;
        icon.fontSize = 28;
        icon.fontStyle = FontStyle.Bold;
        icon.raycastTarget = false;
        return icon;
    }

    void OnPerkClicked(int index)
    {
        if (currentPerks == null || index >= currentPerks.Length) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUIClick();
        }

        PerkData chosen = currentPerks[index];
        if (perkSystem != null && playerStatsRef != null)
        {
            perkSystem.ApplyPerk(chosen, playerStatsRef);
        }

        GameEvents.FireRequestPerkChosen();
    }

    void OnArtifactClicked(int index)
    {
        if (currentArtifacts == null || index >= currentArtifacts.Length) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUIClick();
        }

        ArtifactData chosen = currentArtifacts[index];
        if (artifactSystem != null && playerStatsRef != null)
        {
            artifactSystem.ApplyArtifact(chosen, playerStatsRef);
        }

        GameEvents.FireRequestPerkChosen();
    }

    public void UpdateHealth(int current, int max)
    {
        if (lastHealth >= 0 && current < lastHealth)
        {
            TriggerDamageFlash();
        }
        lastHealth = current;

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

        int rerollCost = GetArtifactRerollCost();
        bool canReroll = playerStatsRef != null && playerStatsRef.coins >= rerollCost;
        bool canHeal = playerStatsRef != null && playerStatsRef.coins >= 5 &&
            playerHealthRef != null && playerHealthRef.currentHealth < playerHealthRef.maxHealth;

        UpdateShopButtonLabel("RerollPerksButton", "Reroll artifacts (" + rerollCost + " ◆)");
        SetPerkShopButtonInteractable("RerollPerksButton", canReroll);
        SetPerkShopButtonInteractable("BuyHealButton", canHeal);
    }

    int GetArtifactRerollCost()
    {
        int discount = playerStatsRef != null ? playerStatsRef.artifactRerollDiscount : 0;
        return Mathf.Max(2, 8 - discount);
    }

    void UpdateShopButtonLabel(string childName, string label)
    {
        if (perkPanel == null) return;
        Transform t = perkPanel.transform.Find(childName);
        if (t == null) return;
        Transform text = t.Find("Text");
        if (text == null) return;
        Text uiText = text.GetComponent<Text>();
        if (uiText != null)
        {
            uiText.text = label;
        }
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
        if (playerStatsRef == null) return;
        if (!playerStatsRef.TrySpendCoins(GetArtifactRerollCost())) return;

        if (artifactSystem != null)
        {
            currentArtifacts = artifactSystem.GetChoices(3);
            SetupArtifactButtons();
        }
        else if (perkSystem != null)
        {
            currentPerks = perkSystem.GetRandomPerks(3);
            SetupPerkButtons();
        }

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
    void EnsureRuntimeHudElements()
    {
        if (damageFlashImage == null)
        {
            GameObject obj = new GameObject("DamageFlash");
            obj.transform.SetParent(transform, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            damageFlashImage = obj.AddComponent<Image>();
            damageFlashImage.raycastTarget = false;
            damageFlashImage.color = new Color(0.95f, 0.05f, 0.04f, 0f);
            damageFlashImage.gameObject.SetActive(false);
        }

        if (hintText == null)
        {
            GameObject obj = new GameObject("HintText");
            obj.transform.SetParent(transform, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 86f);
            rt.sizeDelta = new Vector2(900f, 54f);

            hintText = obj.AddComponent<Text>();
            hintText.text = "";
            hintText.fontSize = 20;
            hintText.alignment = TextAnchor.MiddleCenter;
            hintText.raycastTarget = false;
            hintText.font = GetRuntimeFont();
            hintText.gameObject.SetActive(false);
        }

        EnsureHintBackplate();
        EnsureHudChrome();
    }

    Font GetRuntimeFont()
    {
        if (uiFont != null)
        {
            return uiFont;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        return font;
    }

    void ApplyVisualTheme()
    {
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
        }

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
        ApplyFontToAllText();

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
                hbRt.anchorMin = new Vector2(0f, 1f);
                hbRt.anchorMax = new Vector2(0f, 1f);
                hbRt.pivot = new Vector2(0f, 1f);
                hbRt.anchoredPosition = new Vector2(24f, -48f);
                hbRt.sizeDelta = new Vector2(250f, 20f);
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
            healthText.fontSize = 18;
            healthText.fontStyle = FontStyle.Bold;
            healthText.alignment = TextAnchor.UpperLeft;
            SetHudTextRect(healthText, new Vector2(24f, -72f), new Vector2(220f, 24f));
            AddTextShadow(healthText, new Color(0f, 0f, 0f, 0.72f), new Vector2(1.5f, -1.5f));
        }

        if (levelText != null)
        {
            levelText.color = new Color(0.84f, 0.78f, 0.64f, 1f);
            levelText.fontSize = 18;
            levelText.fontStyle = FontStyle.Bold;
            levelText.alignment = TextAnchor.UpperLeft;
            SetHudTextRect(levelText, new Vector2(24f, -18f), new Vector2(150f, 24f));
            AddTextShadow(levelText, new Color(0f, 0f, 0f, 0.72f), new Vector2(1.5f, -1.5f));
        }

        if (coinsText != null)
        {
            coinsText.color = new Color(1f, 0.84f, 0.22f, 1f);
            coinsText.fontSize = 20;
            coinsText.fontStyle = FontStyle.Bold;
            coinsText.alignment = TextAnchor.UpperRight;
            SetHudTextRect(coinsText, new Vector2(194f, -18f), new Vector2(116f, 24f));
            AddTextShadow(coinsText, new Color(0f, 0f, 0f, 0.72f), new Vector2(1.5f, -1.5f));
        }

        if (dashCooldownText != null)
        {
            dashCooldownText.color = new Color(0.42f, 0.68f, 0.76f, 1f);
            dashCooldownText.fontSize = 16;
            dashCooldownText.alignment = TextAnchor.UpperLeft;
            SetHudTextRect(dashCooldownText, new Vector2(24f, -96f), new Vector2(260f, 24f));
            AddTextShadow(dashCooldownText, new Color(0f, 0f, 0f, 0.72f), new Vector2(1.5f, -1.5f));
        }

        if (weaponModeText != null)
        {
            weaponModeText.color = new Color(0.63f, 0.68f, 0.78f, 1f);
            weaponModeText.fontSize = 15;
            weaponModeText.alignment = TextAnchor.UpperLeft;
            SetHudTextRect(weaponModeText, new Vector2(24f, -119f), new Vector2(310f, 24f));
            AddTextShadow(weaponModeText, new Color(0f, 0f, 0f, 0.72f), new Vector2(1.5f, -1.5f));
        }

        if (sealNotificationText != null)
        {
            sealNotificationText.fontSize = 30;
            sealNotificationText.fontStyle = FontStyle.Bold;
            AddTextShadow(sealNotificationText, new Color(0f, 0f, 0f, 0.78f), new Vector2(2f, -2f));
        }

        if (hintText != null)
        {
            hintText.fontSize = 18;
            hintText.fontStyle = FontStyle.Bold;
            AddTextShadow(hintText, new Color(0f, 0f, 0f, 0.84f), new Vector2(1.6f, -1.6f));
        }
    }

    void EnsureHudChrome()
    {
        if (hudPanel == null) return;

        Image status = EnsurePanelImage(hudPanel.transform, "HudStatusBackplate",
            new Color(0.055f, 0.045f, 0.065f, 0.76f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(14f, -12f), new Vector2(326f, 138f));
        status.transform.SetAsFirstSibling();

        Image accent = EnsurePanelImage(status.transform, "AccentStrip",
            new Color(1f, 0.64f, 0.2f, 0.85f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 0f), new Vector2(4f, 138f));
        accent.raycastTarget = false;

        Transform compass = hudPanel.transform.Find("ExitCompassRow");
        if (compass != null)
        {
            RectTransform rt = compass.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-22f, -18f);
                rt.sizeDelta = new Vector2(92f, 86f);
            }

            Image compassBg = compass.GetComponent<Image>();
            if (compassBg == null)
            {
                compassBg = compass.gameObject.AddComponent<Image>();
            }
            if (panelSprite != null)
            {
                compassBg.sprite = panelSprite;
                compassBg.type = Image.Type.Sliced;
            }
            compassBg.color = new Color(0.055f, 0.045f, 0.065f, 0.72f);
            compassBg.raycastTarget = false;

            Transform arrow = compass.Find("ExitCompassArrow");
            if (arrow != null && compassArrowSprite != null)
            {
                Text arrowText = arrow.GetComponent<Text>();
                if (arrowText != null)
                {
                    arrowText.enabled = false;
                }

                Transform imageChild = arrow.Find("ExitCompassArrowImage");
                if (imageChild == null)
                {
                    GameObject imageObj = new GameObject("ExitCompassArrowImage");
                    imageObj.transform.SetParent(arrow, false);
                    imageChild = imageObj.transform;
                }

                GameObject imageGameObject = imageChild.gameObject;
                RectTransform imageRt = imageGameObject.GetComponent<RectTransform>();
                if (imageRt == null)
                {
                    imageRt = imageGameObject.AddComponent<RectTransform>();
                }
                imageRt.anchorMin = Vector2.zero;
                imageRt.anchorMax = Vector2.one;
                imageRt.offsetMin = Vector2.zero;
                imageRt.offsetMax = Vector2.zero;

                Image arrowImage = imageGameObject.GetComponent<Image>();
                if (arrowImage == null)
                {
                    arrowImage = imageGameObject.AddComponent<Image>();
                }
                arrowImage.sprite = compassArrowSprite;
                arrowImage.preserveAspect = true;
                arrowImage.color = new Color(1f, 0.82f, 0.15f, 1f);
                arrowImage.raycastTarget = false;
            }

            Text[] texts = compass.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                texts[i].fontStyle = FontStyle.Bold;
                AddTextShadow(texts[i], new Color(0f, 0f, 0f, 0.8f), new Vector2(1.4f, -1.4f));
            }
        }
    }

    void EnsureHintBackplate()
    {
        Transform existing = transform.Find("HintBackplate");
        if (existing != null)
        {
            hintBackplateImage = existing.GetComponent<Image>();
        }

        if (hintBackplateImage == null)
        {
            GameObject obj = new GameObject("HintBackplate");
            obj.transform.SetParent(transform, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 86f);
            rt.sizeDelta = new Vector2(820f, 42f);

            hintBackplateImage = obj.AddComponent<Image>();
            hintBackplateImage.raycastTarget = false;
        }

        if (panelSprite != null)
        {
            hintBackplateImage.sprite = panelSprite;
            hintBackplateImage.type = Image.Type.Sliced;
        }
        hintBackplateImage.color = new Color(0.055f, 0.045f, 0.065f, 0.68f);
        hintBackplateImage.gameObject.SetActive(false);
        if (hintText != null)
        {
            hintBackplateImage.transform.SetSiblingIndex(hintText.transform.GetSiblingIndex());
        }
    }

    Image EnsurePanelImage(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
    {
        Transform existing = parent.Find(name);
        GameObject obj = existing != null ? existing.gameObject : new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt == null)
        {
            rt = obj.AddComponent<RectTransform>();
        }
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;

        Image image = obj.GetComponent<Image>();
        if (image == null)
        {
            image = obj.AddComponent<Image>();
        }
        if (panelSprite != null && name != "AccentStrip")
        {
            image.sprite = panelSprite;
            image.type = Image.Type.Sliced;
        }
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static void SetHudTextRect(Text text, Vector2 anchoredPosition, Vector2 size)
    {
        RectTransform rt = text.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;
    }

    static void AddTextShadow(Text text, Color color, Vector2 distance)
    {
        if (text == null) return;
        Shadow shadow = text.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = text.gameObject.AddComponent<Shadow>();
        }

        shadow.effectColor = color;
        shadow.effectDistance = distance;
    }

    static void ApplyMenuPanelBackdrop(GameObject panel, Color backdrop)
    {
        if (panel == null) return;
        if (!panel.TryGetComponent<Image>(out var img)) return;
        img.color = backdrop;
    }

    void StyleButtonsUnder(GameObject root)
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
                Image img = g as Image;
                if (img != null && buttonSprite != null)
                {
                    img.sprite = buttonSprite;
                    img.type = Image.Type.Sliced;
                    img.color = new Color(0.92f, 0.68f, 0.22f, 1f);
                }
            }

            Transform textTr = buttons[i].transform.Find("Text");
            if (textTr != null && textTr.TryGetComponent<Text>(out var txt))
            {
                txt.color = new Color(0.96f, 0.92f, 0.98f, 1f);
                txt.fontStyle = FontStyle.Bold;
            }
        }
    }

    void ApplyFontToAllText()
    {
        Font font = GetRuntimeFont();
        if (font == null) return;

        Text[] texts = GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            texts[i].font = font;
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
