// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SELECTOR DE DISTRITOS — sustituye visualmente el "¿Qué deseas trabajar hoy?"
/// (EasyMenu / MediumMenu / HardMenu) sin tocar las escenas: overlay opaco por
/// código (lo muestra WorldNavRouter al cargar esas escenas).
///
/// Cada tarjeta es un distrito de Attentia con su arte temático: en ruinas si
/// aún no tiene sectores revividos, y con vida (color, luces) si los tiene.
/// El progreso de su Fuente Cognitiva se muestra con 5 puntos.
/// </summary>
public class DistrictPickScreen : MonoBehaviour
{
    static DistrictPickScreen _current;
    public static bool IsOpen => _current != null;

    RectTransform _root;

    public static void Show()
    {
        if (_current != null) return;
        KidUI.EnsureEventSystem();
        var go = new GameObject("DistrictPick");
        _current = go.AddComponent<DistrictPickScreen>();
        _current.Build();
    }

    void OnDestroy()
    {
        if (_current == this) _current = null;
    }

    // ================================================================ BUILD

    void Build()
    {
        var cv = KidUI.MakeCanvas("DistrictPickCanvas", 400, transform);
        _root = cv.GetComponent<RectTransform>();

        KidUI.BuildSpaceBackground(_root, withPlanet: false);

        KidUI.Btn(_root, "◀  PLANETA", KidUI.BTNC,
                  new Vector2(0.015f, 0.925f), new Vector2(0.135f, 0.975f),
                  () => SceneLoader.GoToMainMenu(), 16f);

        var title = KidUI.Txt(_root, "Title", "¿A QUÉ DISTRITO VIAJAMOS?", Color.white, 44,
                              new Vector2(0.16f, 0.905f), new Vector2(0.84f, 0.985f));
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 4f;

        KidUI.Txt(_root, "Sub", "Cada distrito espera recuperar su energía",
                  KidUI.DIM, 18, new Vector2(0.16f, 0.862f), new Vector2(0.84f, 0.905f));

        string profileId = GameCatalog.ActiveProfileId;

        Vector2[] centers =
        {
            new Vector2(0.185f, 0.60f), new Vector2(0.50f, 0.60f), new Vector2(0.815f, 0.60f),
            new Vector2(0.345f, 0.235f), new Vector2(0.655f, 0.235f)
        };
        // Orden visual fijo: Memoria, Atención, Planificación, Emocional, Impulsos
        int[] order = { (int)MinigameCategory.Memory, (int)MinigameCategory.Attention,
                        (int)MinigameCategory.Planning, (int)MinigameCategory.EmotionalManagement,
                        (int)MinigameCategory.ImpulseControl };
        for (int i = 0; i < 5; i++)
            BuildDistrictCard(order[i], centers[i], i, profileId);
    }

    void BuildDistrictCard(int cat, Vector2 center, int idx, string profileId)
    {
        var d = GameCatalog.Get(cat);
        Color col = GameCatalog.CatColor(cat);
        int sumaReto = ChallengeSystem.SumaDistrito(profileId, cat);   // 0-20
        bool alive = sumaReto > 0;
        int rangoMedio = Mathf.RoundToInt(sumaReto / 5f);              // 0-4
        Color rankCol = ChallengeSystem.ColorRango(rangoMedio);

        var holder = new GameObject("District_" + cat);
        holder.transform.SetParent(_root, false);
        var rt = holder.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = center;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(520f, 330f);

        // Marco con el color del RANGO MEDIO del distrito
        if (alive)
        {
            var edge = KidUI.RoundImg(rt, "Edge", new Color(rankCol.r, rankCol.g, rankCol.b, 0.45f),
                Vector2.zero, Vector2.one, Vector2.zero, new Vector2(8f, 8f), 1.0f);
            DistrictArt.NoRay(edge);
        }

        var bg = KidUI.RoundImg(rt, "Bg",
            alive ? new Color(0.07f, 0.10f, 0.20f, 0.97f) : new Color(0.055f, 0.065f, 0.115f, 0.97f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 1.0f);

        // Emblema temático del distrito (mismo arte que sus sectores)
        var art = KidUI.Img(bg, "Art", Color.clear,
                            new Vector2(0.06f, 0.40f), new Vector2(0.94f, 0.94f),
                            Vector2.zero, Vector2.zero);
        art.GetComponent<Image>().raycastTarget = false;
        DistrictArt.Sector(art, cat, alive, col, 0);

        // Nombre narrativo + función ejecutiva
        var name = KidUI.Txt(bg, "Name", d.nombre, alive ? col : new Color(0.72f, 0.78f, 0.90f),
                             25, new Vector2(0.05f, 0.235f), new Vector2(0.95f, 0.40f));
        name.fontStyle = FontStyles.Bold;
        var func = KidUI.Txt(bg, "Func",
                             MinigameResultData.CategoryDisplayName((MinigameCategory)cat),
                             KidUI.DIM, 15, new Vector2(0.05f, 0.155f), new Vector2(0.95f, 0.245f));

        // Reto acumulado de la Fuente Cognitiva (0-20): mini-barra + texto.
        // Con 20/20 la Fuente es Legendaria (estrella dorada).
        var barBg = KidUI.RoundImg(bg, "BarBg", new Color(1f, 1f, 1f, 0.10f),
            new Vector2(0.30f, 0.065f), new Vector2(0.76f, 0.115f),
            Vector2.zero, Vector2.zero, 3f);
        DistrictArt.NoRay(barBg);
        float frac = Mathf.Clamp01(sumaReto / 20f);
        var barFill = KidUI.RoundImg(barBg, "BarFill", alive ? rankCol : KidUI.DIM,
            new Vector2(0f, 0f), new Vector2(Mathf.Max(0.04f, frac), 1f),
            Vector2.zero, Vector2.zero, 3f);
        DistrictArt.NoRay(barFill);
        var barT = KidUI.Txt(bg, "BarT", $"{sumaReto}/20", alive ? rankCol : KidUI.DIM, 15,
            new Vector2(0.78f, 0.04f), new Vector2(0.98f, 0.13f));
        barT.fontStyle = FontStyles.Bold;
        if (sumaReto >= 20)
            DistrictArt.Star(bg, new Vector2(0.90f, 0.885f), 34f, new Color(1f, 0.82f, 0.12f));

        var btn = holder.AddComponent<Button>();
        btn.targetGraphic = bg.GetComponent<Image>();
        MinigameCategory captured = (MinigameCategory)cat;
        btn.onClick.AddListener(() =>
        {
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayClick();
            SceneLoader.LoadCategorySelector(captured);
        });
        ButtonJuice.Attach(holder);

        UITween.PopIn(rt, 0.38f, 0.82f, 0.06f * idx);
    }
}
