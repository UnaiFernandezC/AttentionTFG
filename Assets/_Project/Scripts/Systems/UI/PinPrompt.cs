// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Modal de PIN de 4 dígitos para el área de tutor (teclado numérico en pantalla,
/// apto también para pantallas táctiles). Si aún no hay PIN configurado, guía la
/// creación (introducir + confirmar). Todo construido por código.
/// </summary>
public class PinPrompt : MonoBehaviour
{
    const int PIN_LENGTH = 4;

    System.Action _onSuccess;
    System.Action _onCancel;

    GameObject _root;
    TextMeshProUGUI _titleT;
    TextMeshProUGUI _msgT;
    Image[] _dots;

    string _entered = "";
    string _firstPin;          // en modo creación: primer PIN introducido
    bool _creating;
    bool _confirming;

    static PinPrompt _current;

    /// <summary>True si hay un modal de PIN en pantalla (lo usa el menú de pausa
    /// para no reaccionar a ESC mientras tanto).</summary>
    public static bool IsOpen => _current != null;

    public static void Show(System.Action onSuccess, System.Action onCancel = null)
    {
        bool creating = ProfileManager.Instance == null || !ProfileManager.Instance.HasTutorPin;
        ShowInternal(onSuccess, onCancel, creating);
    }

    /// <summary>Fuerza el flujo de creación (usado para "Cambiar PIN": el PIN antiguo
    /// solo se sustituye cuando el nuevo queda confirmado).</summary>
    public static void ShowCreate(System.Action onSuccess, System.Action onCancel = null)
    {
        ShowInternal(onSuccess, onCancel, forceCreate: true);
    }

    static void ShowInternal(System.Action onSuccess, System.Action onCancel, bool forceCreate)
    {
        if (_current != null) return;
        KidUI.EnsureEventSystem();
        var go = new GameObject("PinPrompt");
        var p = go.AddComponent<PinPrompt>();
        _current = p;
        p._onSuccess = onSuccess;
        p._onCancel = onCancel;
        p._creating = forceCreate;
        p.Build();
    }

    void OnDestroy()
    {
        if (_current == this) _current = null;
    }

    void Build()
    {
        var cv = KidUI.MakeCanvas("PinCanvas", 900, transform);
        var R = cv.GetComponent<RectTransform>();
        _root = cv.gameObject;

        KidUI.Img(R, "Dim", new Color(0, 0, 0, 0.80f), Vector2.zero, Vector2.one,
                  Vector2.zero, Vector2.zero);

        var card = KidUI.RoundImg(R, "Card", new Color(0.055f, 0.075f, 0.15f, 0.98f),
                                  new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                  Vector2.zero, new Vector2(520f, 700f), 0.9f);
        var topEdge = KidUI.RoundImg(card, "Top", KidUI.WARN,
                                     new Vector2(0.32f, 0.985f), new Vector2(0.68f, 0.993f),
                                     Vector2.zero, Vector2.zero, 4f);
        topEdge.GetComponent<Image>().raycastTarget = false;

        _titleT = KidUI.Txt(card, "Title",
            _creating ? "CREA UN PIN DE TUTOR" : "ZONA DE ADULTOS",
            Color.white, 30, new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.98f));
        _titleT.fontStyle = FontStyles.Bold;

        _msgT = KidUI.Txt(card, "Msg",
            _creating ? "Elige un PIN de 4 digitos para proteger\nlos datos del menor."
                      : "Introduce el PIN de 4 digitos.",
            KidUI.DIM, 20, new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.90f));

        // Puntos del PIN (círculos)
        _dots = new Image[PIN_LENGTH];
        for (int i = 0; i < PIN_LENGTH; i++)
        {
            float xc = 0.335f + i * 0.11f;
            var d = KidUI.CircleAt(card, "Dot" + i, KidUI.BTNC,
                                   new Vector2(xc, 0.75f), 36f);
            _dots[i] = d.GetComponent<Image>();
        }

        // Teclado 1-9, borrar, 0, cancelar
        string[] keys = { "1","2","3","4","5","6","7","8","9","<","0","X" };
        for (int i = 0; i < keys.Length; i++)
        {
            int row = i / 3, col = i % 3;
            float x0 = 0.10f + col * 0.28f;
            float y0 = 0.52f - row * 0.135f;
            string k = keys[i];
            Color c = k == "X" ? KidUI.BAD : k == "<" ? KidUI.BTNC : KidUI.PANEL2;
            KidUI.Btn(card, k == "<" ? "BORRAR" : k == "X" ? "SALIR" : k, c,
                      new Vector2(x0, y0), new Vector2(x0 + 0.24f, y0 + 0.115f),
                      () => OnKey(k), k == "<" || k == "X" ? 18f : 30f);
        }

        UITween.FadeIn(_root, 0.20f);
        UITween.PopIn(card, 0.30f, 0.88f);
    }

    void OnKey(string k)
    {
        if (k == "X") { Close(); _onCancel?.Invoke(); return; }
        if (k == "<")
        {
            if (_entered.Length > 0) _entered = _entered.Substring(0, _entered.Length - 1);
            RefreshDots();
            return;
        }

        if (_entered.Length >= PIN_LENGTH) return;
        _entered += k;
        RefreshDots();
        if (_entered.Length == PIN_LENGTH) Submit();
    }

    void RefreshDots()
    {
        for (int i = 0; i < PIN_LENGTH; i++)
            _dots[i].color = i < _entered.Length ? KidUI.WARN : KidUI.BTNC;
    }

    void Submit()
    {
        var pm = ProfileManager.Instance;

        if (_creating)
        {
            if (!_confirming)
            {
                _firstPin = _entered;
                _entered = "";
                _confirming = true;
                _msgT.text = "Repite el PIN para confirmarlo.";
                _msgT.color = KidUI.ACCENT;
                RefreshDots();
                return;
            }
            if (_entered == _firstPin)
            {
                if (pm != null) pm.SetTutorPin(_entered);
                Close();
                _onSuccess?.Invoke();
            }
            else
            {
                _entered = "";
                _confirming = false;
                _firstPin = "";
                _msgT.text = "No coinciden. Vuelve a empezar.";
                _msgT.color = KidUI.BAD;
                RefreshDots();
            }
            return;
        }

        if (pm != null && pm.VerifyTutorPin(_entered))
        {
            Close();
            _onSuccess?.Invoke();
        }
        else
        {
            _entered = "";
            _msgT.text = "PIN incorrecto. Intentalo de nuevo.";
            _msgT.color = KidUI.BAD;
            RefreshDots();
        }
    }

    void Close() => Destroy(gameObject);
}
