using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Автоматически создаёт UI структуру при старте игры.
/// Добавь этот скрипт на пустой GameObject в сцене.
/// После запуска игры UI будет создан автоматически.
///
/// СТРУКТУРА UI:
/// - TopHUD: Ресурсы (Skulls, Meat, Wood, Gold) + Cargo индикатор
/// - BottomLeftHUD: HP бар, XP бар, кнопка Upgrades
/// - ArmySlots не создаются по умолчанию (армии пока нет в игре)
/// </summary>
public class UIAutoBuilder : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Создать UI при старте")]
    public bool buildOnStart = true;

    [Tooltip("Удалить этот компонент после создания UI")]
    public bool destroyAfterBuild = false;

    [Header("Colors")]
    public Color skullColor = new Color(0.9f, 0.9f, 0.9f);
    public Color meatColor = new Color(1f, 0.4f, 0.4f);
    public Color woodColor = new Color(0.6f, 0.4f, 0.2f);
    public Color goldColor = new Color(1f, 0.85f, 0f);
    public Color hpColor = new Color(0.8f, 0.2f, 0.2f);
    public Color xpColor = new Color(0.3f, 0.7f, 1f);

    private Canvas mainCanvas;
    private GameObject topHUD;
    private GameObject bottomLeftHUD;

    private void Start()
    {
        if (buildOnStart)
        {
            BuildUI();
        }
    }

    [ContextMenu("Build UI Now")]
    public void BuildUI()
    {
        Debug.Log("[UIAutoBuilder] Creating UI...");

        // Проверяем, есть ли уже Canvas
        mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas == null)
        {
            CreateMainCanvas();
        }
        else
        {
            Debug.Log("[UIAutoBuilder] Canvas already exists, using existing one");
        }

        // Создаём UI элементы
        CreateTopHUD();
        CreateBottomLeftHUD();
        CreateEventSystem();

        Debug.Log("[UIAutoBuilder] UI created successfully!");

        if (destroyAfterBuild)
        {
            Destroy(this);
        }
    }

    private void CreateMainCanvas()
    {
        GameObject canvasGO = new GameObject("MainCanvas");
        mainCanvas = canvasGO.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        Debug.Log("[UIAutoBuilder] Created MainCanvas with CanvasScaler (1920x1080)");
    }

    private void CreateEventSystem()
    {
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
            Debug.Log("[UIAutoBuilder] Created EventSystem with InputSystemUIInputModule");
        }
    }

    private void CreateTopHUD()
    {
        topHUD = CreatePanel("TopHUD", mainCanvas.transform);

        RectTransform rt = topHUD.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0, 70);

        Image bg = topHUD.GetComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.6f);

        HorizontalLayoutGroup layout = topHUD.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 10, 10);
        layout.spacing = 40;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        // Создаём элементы ресурсов (4 ресурса)
        CreateResourceDisplaySimple(topHUD.transform, "Skulls", "0", skullColor);
        CreateResourceDisplaySimple(topHUD.transform, "Meat", "0", meatColor);
        CreateResourceDisplaySimple(topHUD.transform, "Wood", "0", woodColor);
        CreateResourceDisplaySimple(topHUD.transform, "Gold", "0", goldColor);

        // Spacer
        CreateSpacer(topHUD.transform);

        // Cargo индикатор справа
        CreateCargoDisplay(topHUD.transform);

        // Добавляем RuntimeTopHUD скрипт
        topHUD.AddComponent<RuntimeTopHUD>();

        Debug.Log("[UIAutoBuilder] Created TopHUD with 4 resources + cargo");
    }

    private void CreateBottomLeftHUD()
    {
        bottomLeftHUD = CreatePanel("BottomLeftHUD", mainCanvas.transform);

        RectTransform rt = bottomLeftHUD.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(20, 20);
        rt.sizeDelta = new Vector2(300, 130);

        Image bg = bottomLeftHUD.GetComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.6f);

        VerticalLayoutGroup layout = bottomLeftHUD.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(15, 15, 10, 10);
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;

        // HP Bar
        CreateStatBar(bottomLeftHUD.transform, "HP", hpColor, 100, 100);

        // XP Bar
        CreateStatBar(bottomLeftHUD.transform, "XP", xpColor, 0, 100);

        // Level display
        CreateLevelDisplay(bottomLeftHUD.transform);

        // Кнопка Upgrades
        CreateUpgradeButton(bottomLeftHUD.transform);

        Debug.Log("[UIAutoBuilder] Created BottomLeftHUD with HP/XP bars and Upgrade button");
    }

    private GameObject CreatePanel(string name, Transform parent)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        panel.AddComponent<RectTransform>();
        panel.AddComponent<CanvasRenderer>();
        panel.AddComponent<Image>();

        return panel;
    }

    private void CreateResourceDisplaySimple(Transform parent, string resourceName, string defaultValue, Color iconColor)
    {
        GameObject container = new GameObject(resourceName + "Container");
        container.transform.SetParent(parent, false);

        RectTransform rt = container.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100, 50);

        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        // Иконка
        GameObject icon = new GameObject(resourceName + "Icon");
        icon.transform.SetParent(container.transform, false);
        RectTransform iconRT = icon.AddComponent<RectTransform>();
        iconRT.sizeDelta = new Vector2(36, 36);
        Image iconImg = icon.AddComponent<Image>();
        iconImg.color = iconColor;

        // Текст
        GameObject textGO = new GameObject(resourceName + "Text");
        textGO.transform.SetParent(container.transform, false);
        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.sizeDelta = new Vector2(60, 36);

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = defaultValue;
        tmp.fontSize = 28;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Left;
    }

    private void CreateSpacer(Transform parent)
    {
        GameObject spacer = new GameObject("Spacer");
        spacer.transform.SetParent(parent, false);
        RectTransform rt = spacer.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(10, 10);
        LayoutElement le = spacer.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;
    }

    private void CreateCargoDisplay(Transform parent)
    {
        GameObject container = new GameObject("CargoContainer");
        container.transform.SetParent(parent, false);

        RectTransform rt = container.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(180, 50);

        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        // Лейбл
        GameObject label = new GameObject("CargoLabel");
        label.transform.SetParent(container.transform, false);
        RectTransform labelRT = label.AddComponent<RectTransform>();
        labelRT.sizeDelta = new Vector2(70, 36);

        TextMeshProUGUI labelTmp = label.AddComponent<TextMeshProUGUI>();
        labelTmp.text = "Cargo:";
        labelTmp.fontSize = 20;
        labelTmp.color = new Color(0.8f, 0.8f, 0.8f);
        labelTmp.alignment = TextAlignmentOptions.Right;

        // Bar background
        GameObject barBg = new GameObject("CargoBarBg");
        barBg.transform.SetParent(container.transform, false);
        RectTransform barBgRT = barBg.AddComponent<RectTransform>();
        barBgRT.sizeDelta = new Vector2(100, 20);
        Image barBgImg = barBg.AddComponent<Image>();
        barBgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // Bar fill
        GameObject barFill = new GameObject("CargoBarFill");
        barFill.transform.SetParent(barBg.transform, false);
        RectTransform barFillRT = barFill.AddComponent<RectTransform>();
        barFillRT.anchorMin = new Vector2(0, 0);
        barFillRT.anchorMax = new Vector2(0, 1);
        barFillRT.pivot = new Vector2(0, 0.5f);
        barFillRT.anchoredPosition = Vector2.zero;
        barFillRT.sizeDelta = new Vector2(50, 0);

        Image barFillImg = barFill.AddComponent<Image>();
        barFillImg.color = new Color(0.4f, 0.8f, 0.4f);
    }

    private void CreateStatBar(Transform parent, string statName, Color barColor, int current, int max)
    {
        GameObject container = new GameObject(statName + "BarContainer");
        container.transform.SetParent(parent, false);

        RectTransform rt = container.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 28);

        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = true;

        // Лейбл
        GameObject label = new GameObject(statName + "Label");
        label.transform.SetParent(container.transform, false);
        RectTransform labelRT = label.AddComponent<RectTransform>();
        labelRT.sizeDelta = new Vector2(35, 0);

        TextMeshProUGUI labelTmp = label.AddComponent<TextMeshProUGUI>();
        labelTmp.text = statName;
        labelTmp.fontSize = 18;
        labelTmp.fontStyle = FontStyles.Bold;
        labelTmp.color = barColor;
        labelTmp.alignment = TextAlignmentOptions.Left;

        // Bar background
        GameObject barBg = new GameObject(statName + "BarBg");
        barBg.transform.SetParent(container.transform, false);
        RectTransform barBgRT = barBg.AddComponent<RectTransform>();
        barBgRT.sizeDelta = new Vector2(160, 0);
        LayoutElement barBgLE = barBg.AddComponent<LayoutElement>();
        barBgLE.flexibleWidth = 1;

        Image barBgImg = barBg.AddComponent<Image>();
        barBgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        // Bar fill
        GameObject barFill = new GameObject(statName + "BarFill");
        barFill.transform.SetParent(barBg.transform, false);
        RectTransform barFillRT = barFill.AddComponent<RectTransform>();
        barFillRT.anchorMin = new Vector2(0, 0);
        barFillRT.anchorMax = new Vector2(1, 1);
        barFillRT.offsetMin = new Vector2(2, 2);
        barFillRT.offsetMax = new Vector2(-2, -2);

        Image barFillImg = barFill.AddComponent<Image>();
        barFillImg.color = barColor;
        barFillImg.type = Image.Type.Filled;
        barFillImg.fillMethod = Image.FillMethod.Horizontal;
        barFillImg.fillAmount = max > 0 ? (float)current / max : 1f;

        // Text value
        GameObject valueText = new GameObject(statName + "Text");
        valueText.transform.SetParent(container.transform, false);
        RectTransform valueRT = valueText.AddComponent<RectTransform>();
        valueRT.sizeDelta = new Vector2(70, 0);

        TextMeshProUGUI valueTmp = valueText.AddComponent<TextMeshProUGUI>();
        valueTmp.text = $"{current}/{max}";
        valueTmp.fontSize = 16;
        valueTmp.color = Color.white;
        valueTmp.alignment = TextAlignmentOptions.Left;
    }

    private void CreateLevelDisplay(Transform parent)
    {
        GameObject container = new GameObject("LevelContainer");
        container.transform.SetParent(parent, false);

        RectTransform rt = container.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 24);

        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 5;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;

        // Level label
        GameObject label = new GameObject("LvLabel");
        label.transform.SetParent(container.transform, false);
        RectTransform labelRT = label.AddComponent<RectTransform>();
        labelRT.sizeDelta = new Vector2(50, 0);

        TextMeshProUGUI labelTmp = label.AddComponent<TextMeshProUGUI>();
        labelTmp.text = "Level:";
        labelTmp.fontSize = 16;
        labelTmp.color = new Color(0.7f, 0.7f, 0.7f);
        labelTmp.alignment = TextAlignmentOptions.Left;

        // Level value
        GameObject value = new GameObject("LvText");
        value.transform.SetParent(container.transform, false);
        RectTransform valueRT = value.AddComponent<RectTransform>();
        valueRT.sizeDelta = new Vector2(30, 0);

        TextMeshProUGUI valueTmp = value.AddComponent<TextMeshProUGUI>();
        valueTmp.text = "1";
        valueTmp.fontSize = 20;
        valueTmp.fontStyle = FontStyles.Bold;
        valueTmp.color = new Color(0.3f, 1f, 0.3f);
        valueTmp.alignment = TextAlignmentOptions.Left;
    }

    private void CreateUpgradeButton(Transform parent)
    {
        GameObject buttonGO = new GameObject("UpgradeButton");
        buttonGO.transform.SetParent(parent, false);

        RectTransform rt = buttonGO.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 30);

        Image buttonImg = buttonGO.AddComponent<Image>();
        buttonImg.color = new Color(0.2f, 0.5f, 0.8f);

        Button button = buttonGO.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.5f, 0.8f);
        colors.highlightedColor = new Color(0.3f, 0.6f, 0.9f);
        colors.pressedColor = new Color(0.15f, 0.4f, 0.7f);
        button.colors = colors;

        button.onClick.AddListener(OnUpgradeButtonClicked);

        // Текст
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);

        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "UPGRADES";
        tmp.fontSize = 18;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    private void OnUpgradeButtonClicked()
    {
        Debug.Log("[UIAutoBuilder] Upgrade button clicked!");

        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnOpenModal("upgrade", null);
        }
    }
}
