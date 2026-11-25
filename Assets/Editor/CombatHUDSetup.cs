using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CombatHUDSetup
{
    // Must match the GameObject names in your scene exactly
    private const string BossObjectName = "Boss";
    private const string PlayerObjectName = "NightshadeFinal";

    [MenuItem("GameObject/Setup Combat HUD", false, 0)]
    private static void Setup()
    {
        GameObject hud = GameObject.Find("CombatHUD");
        if (hud == null)
        {
            Debug.LogError("CombatHUD not found in the scene.");
            return;
        }

        // Canvas, screen-attached
        Canvas canvas = hud.GetComponent<Canvas>();
        if (canvas == null) canvas = hud.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = hud.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = hud.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (hud.GetComponent<GraphicRaycaster>() == null)
            hud.AddComponent<GraphicRaycaster>();

        // Boss: top, red. Nightshade: bottom, green.
        SetupBar(hud.transform, "Boss", "BOSS", true,
            new Color(0.85f, 0.1f, 0.1f), BossObjectName);

        SetupBar(hud.transform, "Nightshade", "NIGHTSHADE", false,
            new Color(0.2f, 0.8f, 0.25f), PlayerObjectName);

        EditorSceneManager.MarkSceneDirty(hud.scene);
        Debug.Log("Combat HUD set up.");
    }

    private static void SetupBar(Transform hud, string prefix, string displayName,
        bool top, Color fillColor, string targetObjectName)
    {
        RectTransform panel = hud.Find(prefix + "Panel") as RectTransform;
        if (panel == null)
        {
            Debug.LogError(prefix + "Panel not found.");
            return;
        }

        RectTransform nameRT = panel.Find(prefix + "Name") as RectTransform;
        RectTransform bgRT = panel.Find(prefix + "HealthBackground") as RectTransform;
        RectTransform fillRT = bgRT != null ? bgRT.Find(prefix + "HealthFill") as RectTransform : null;
        RectTransform hpRT = panel.Find(prefix + "HP") as RectTransform;

        if (nameRT == null || bgRT == null || fillRT == null || hpRT == null)
        {
            Debug.LogError(prefix + ": one of the child objects is missing or misnamed.");
            return;
        }

        // Panel anchored to top-center or bottom-center of screen
        float y = top ? 1f : 0f;
        panel.anchorMin = new Vector2(0.5f, y);
        panel.anchorMax = new Vector2(0.5f, y);
        panel.pivot = new Vector2(0.5f, y);
        panel.sizeDelta = new Vector2(900f, 70f);
        panel.anchoredPosition = new Vector2(0f, top ? -40f : 40f);

        // Name: top strip of the panel
        nameRT.anchorMin = new Vector2(0f, 1f);
        nameRT.anchorMax = new Vector2(1f, 1f);
        nameRT.pivot = new Vector2(0.5f, 1f);
        nameRT.offsetMin = new Vector2(0f, -30f);
        nameRT.offsetMax = Vector2.zero;

        TMP_Text nameText = nameRT.GetComponent<TMP_Text>();
        if (nameText != null)
        {
            nameText.text = displayName;
            nameText.fontSize = 24;
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            nameText.color = Color.white;
            nameText.raycastTarget = false;
        }

        // Background: below the name
        Stretch(bgRT, 0, 0, 0, 34);
        Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        Image bgImage = bgRT.GetComponent<Image>();
        if (bgImage == null) bgImage = bgRT.gameObject.AddComponent<Image>();
        bgImage.sprite = sprite;
        bgImage.type = Image.Type.Simple;
        bgImage.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);
        bgImage.raycastTarget = false;

        // Fill: must be Image.Type.Filled WITH a sprite, otherwise fillAmount does nothing.
        Stretch(fillRT, 3, 3, 3, 3);
        Image fillImage = fillRT.GetComponent<Image>();
        if (fillImage == null) fillImage = fillRT.gameObject.AddComponent<Image>();
        fillImage.sprite = sprite;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 1f;
        fillImage.color = fillColor;
        fillImage.raycastTarget = false;

        // HP text: centered over the bar
        Stretch(hpRT, 0, 0, 0, 34);
        TMP_Text hpText = hpRT.GetComponent<TMP_Text>();
        if (hpText != null)
        {
            hpText.text = "100 / 100";
            hpText.fontSize = 20;
            hpText.alignment = TextAlignmentOptions.Center;
            hpText.color = Color.white;
            hpText.raycastTarget = false;
        }

        // Wire HealthBarUI
        HealthBarUI bar = panel.GetComponent<HealthBarUI>();
        if (bar == null) bar = panel.gameObject.AddComponent<HealthBarUI>();

        Health health = null;
        GameObject targetGO = GameObject.Find(targetObjectName);
        if (targetGO != null) health = targetGO.GetComponentInChildren<Health>();

        SerializedObject so = new SerializedObject(bar);

        // Only overwrite target if we actually found one,
        // so a manually assigned target never gets wiped.
        SerializedProperty targetProp = so.FindProperty("target");
        if (health != null)
            targetProp.objectReferenceValue = health;
        else if (targetProp.objectReferenceValue == null)
            Debug.LogWarning($"{prefix}: no Health found on '{targetObjectName}'. Drag it into HealthBarUI > Target manually.");

        so.FindProperty("fillImage").objectReferenceValue = fillImage;
        so.FindProperty("healthText").objectReferenceValue = hpText;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(bar);
    }

    private static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }
}