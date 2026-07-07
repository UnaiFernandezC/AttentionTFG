// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fondo espacial animado para la pantalla inicial y TODOS los selectores
/// (categorías y minijuegos). Está colocado en esas escenas como
/// "BackgroundController"; dibuja el espacio (KidUI.BuildSpaceBackground) en un
/// canvas por detrás de la UI y oculta los antiguos fondos "Fondo_*.png" a
/// pantalla completa. Añade polvo espacial a la deriva y estrellas fugaces.
/// </summary>
public class MainMenuBackgroundController : MonoBehaviour
{
    // Campos conservados por compatibilidad con los valores serializados en escena.
    [Header("Partículas (polvo espacial)")]
    public int   particleCount    = 26;
    public float particleMinSpeed = 8f;
    public float particleMaxSpeed = 24f;

    [Header("Nodos (obsoleto, sin uso)")]
    public int  nodeCount           = 11;
    public float nodeConnectionDist = 0.32f;

    [Header("Escáner (obsoleto, sin uso)")]
    public float scanPeriod = 10f;

    [Header("Estrellas fugaces")]
    public float shootingStarInterval = 4.5f;

    const float RW = 1920f, RH = 1080f;

    RectTransform _root;
    RectTransform[] _dust;
    float[] _dustSpeed;
    float[] _dustDriftX;
    float _nextStar;

    void Awake()
    {
        Build();
        _nextStar = Time.unscaledTime + Random.Range(1.5f, Mathf.Max(2f, shootingStarInterval));
    }

    void Start()
    {
        HideLegacyBackgrounds();
    }

    void Build()
    {
        var cGO = new GameObject("BG_Canvas");
        cGO.transform.SetParent(transform, false);
        var cv = cGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = -10;                       // siempre detrás de la UI de la escena
        var sc = cGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(RW, RH);
        sc.matchWidthOrHeight  = 0.5f;
        _root = cv.GetComponent<RectTransform>();

        KidUI.BuildSpaceBackground(_root);

        // Polvo espacial: puntos tenues que derivan hacia arriba
        int n = Mathf.Clamp(particleCount, 8, 40);
        _dust = new RectTransform[n];
        _dustSpeed = new float[n];
        _dustDriftX = new float[n];
        for (int i = 0; i < n; i++)
        {
            float size = Random.Range(6f, 18f);
            var d = KidUI.CircleAt(_root, "Dust" + i,
                new Color(0.65f, 0.75f, 1f, Random.Range(0.05f, 0.14f)),
                new Vector2(Random.value, Random.value), size);
            d.GetComponent<Image>().raycastTarget = false;
            _dust[i] = d;
            _dustSpeed[i] = Random.Range(particleMinSpeed, particleMaxSpeed);
            _dustDriftX[i] = Random.Range(-6f, 6f);
        }
    }

    void Update()
    {
        // Deriva del polvo (con envoltura vertical)
        if (_dust != null)
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < _dust.Length; i++)
            {
                var rt = _dust[i];
                if (rt == null) continue;
                var a = rt.anchorMin;
                a.y += _dustSpeed[i] * dt / RH;
                a.x += _dustDriftX[i] * dt / RW;
                if (a.y > 1.05f) { a.y = -0.05f; a.x = Random.value; }
                if (a.x > 1.05f) a.x = -0.05f;
                if (a.x < -0.05f) a.x = 1.05f;
                rt.anchorMin = rt.anchorMax = a;
            }
        }

        // Estrellas fugaces
        if (Time.unscaledTime >= _nextStar && _root != null)
        {
            StartCoroutine(ShootingStar());
            _nextStar = Time.unscaledTime + Random.Range(0.6f, 1.4f) * Mathf.Max(2f, shootingStarInterval);
        }
    }

    IEnumerator ShootingStar()
    {
        var go = new GameObject("ShootingStar");
        go.transform.SetParent(_root, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(Random.Range(0.3f, 1.05f), Random.Range(0.75f, 1.05f));
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(Random.Range(90f, 170f), 3f);
        rt.localRotation = Quaternion.Euler(0, 0, Random.Range(-38f, -28f));
        var img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.sprite = KidUI.RoundedSprite;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 6f;

        float dur = Random.Range(0.5f, 0.8f);
        float t = 0f;
        Vector2 vel = new Vector2(-Random.Range(700f, 1100f), -Random.Range(420f, 660f));
        Vector2 pos = Vector2.zero;
        while (t < dur)
        {
            if (rt == null) yield break;
            float dt = Time.unscaledDeltaTime;
            t += dt;
            pos += vel * dt;
            rt.anchoredPosition = pos;
            float p = t / dur;
            float alpha = p < 0.25f ? p / 0.25f : 1f - (p - 0.25f) / 0.75f;
            img.color = new Color(1f, 1f, 1f, alpha * 0.85f);
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    /// <summary>
    /// Oculta los fondos antiguos de la escena (imágenes "Fondo_*" que cubren casi
    /// toda la pantalla) para que se vea el nuevo fondo espacial. Los elementos
    /// pequeños (tarjetas de botón, logos) no se tocan.
    /// </summary>
    void HideLegacyBackgrounds()
    {
        foreach (var img in FindObjectsOfType<Image>())
        {
            if (img == null || img.sprite == null) continue;
            if (!img.sprite.name.StartsWith("Fondo")) continue;
            var canvas = img.canvas;
            if (canvas == null || canvas.transform.IsChildOf(transform)) continue;

            var canvasRT = canvas.GetComponent<RectTransform>();
            Vector2 cs = canvasRT.rect.size;
            Vector2 s = img.rectTransform.rect.size;
            if (cs.x <= 0f || cs.y <= 0f) continue;

            if (s.x >= cs.x * 0.85f && s.y >= cs.y * 0.85f)
            {
                img.enabled = false;
                Debug.Log($"[BackgroundController] Fondo antiguo ocultado: {img.name} ({img.sprite.name})");
            }
        }
    }
}
