using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleMenu : MonoBehaviour
{
    void Awake()
    {
        EnsureCamera();
        EnsureEventSystem();
        BuildUi();
    }

    void BuildUi()
    {
        var canvasGo = new GameObject("TitleCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        CreateLabel(canvasGo.transform, "Title", "LEAN 60", new Vector2(0.5f, 0.72f), 56, TextAnchor.MiddleCenter);
        CreateLabel(canvasGo.transform, "Subtitle", "Select a difficulty", new Vector2(0.5f, 0.58f), 26, TextAnchor.MiddleCenter);

        CreateDifficultyButton(canvasGo.transform, "Normal", new Vector2(0.5f, 0.42f), GameSession.Difficulty.Normal);
        CreateDifficultyButton(canvasGo.transform, "Hard", new Vector2(0.5f, 0.28f), GameSession.Difficulty.Hard);
    }

    void CreateDifficultyButton(Transform parent, string label, Vector2 anchor, GameSession.Difficulty difficulty)
    {
        var go = new GameObject(label + "Button");
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.18f);
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        var chosen = difficulty;
        button.onClick.AddListener(() =>
        {
            GameSession.Selected = chosen;
            GameSession.HasChosen = true;
            SceneManager.LoadScene("Level");
        });

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(280f, 72f);

        CreateLabel(go.transform, "Label", label, new Vector2(0.5f, 0.5f), 32, TextAnchor.MiddleCenter);
    }

    static void CreateLabel(Transform parent, string name, string text, Vector2 anchor, int fontSize, TextAnchor alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var ui = go.AddComponent<Text>();
        ui.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (ui.font == null)
            ui.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
        ui.fontSize = fontSize;
        ui.alignment = alignment;
        ui.color = Color.white;
        ui.text = text;
        ui.raycastTarget = false;
        var rt = ui.rectTransform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(900f, 80f);
    }

    static void EnsureCamera()
    {
        if (Camera.main != null)
            return;

        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        go.AddComponent<AudioListener>();
    }

    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }
}
