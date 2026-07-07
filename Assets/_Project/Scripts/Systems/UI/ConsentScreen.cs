// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Consentimiento parental (primer arranque, bloqueante):
///  1) Resumen claro de qué datos se guardan y dónde (solo en este dispositivo).
///  2) Política de privacidad completa consultable en la propia app (scroll).
///  3) Puerta parental: una multiplicación que un niño pequeño no resuelve,
///     estándar en apps infantiles para verificar que hay un adulto delante.
/// La aceptación se guarda versionada: si la política cambia de versión,
/// se vuelve a pedir. Sin aceptación no se puede usar la aplicación.
/// </summary>
public class ConsentScreen : MonoBehaviour
{
    /// <summary>Versión de la política. Al cambiarla se re-solicita consentimiento.</summary>
    public const string POLICY_VERSION = "1.0";

    static ConsentScreen _current;

    /// <summary>True mientras el consentimiento está en pantalla (bloquea ESC).</summary>
    public static bool IsOpen => _current != null;

    System.Action _onAccepted;
    RectTransform _root;
    RectTransform _card;

    // Puerta parental
    int _a, _b;
    bool _gatePassed;
    TextMeshProUGUI _gateQuestion;
    TextMeshProUGUI _gateStatus;
    Button _acceptBtn;
    Image _acceptImg;

    public static void Show(System.Action onAccepted)
    {
        if (_current != null) return;
        KidUI.EnsureEventSystem();
        var go = new GameObject("ConsentScreen");
        _current = go.AddComponent<ConsentScreen>();
        _current._onAccepted = onAccepted;
        _current.Build();
    }

    void OnDestroy()
    {
        if (_current == this) _current = null;
    }

    void Build()
    {
        var cv = KidUI.MakeCanvas("ConsentCanvas", 940, transform);
        _root = cv.GetComponent<RectTransform>();
        KidUI.BuildSpaceBackground(_root, withPlanet: false);

        _card = KidUI.RoundImg(_root, "Card", new Color(0.055f, 0.075f, 0.15f, 0.98f),
                               new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                               Vector2.zero, new Vector2(1050f, 860f), 0.7f);
        var pill = KidUI.RoundImg(_card, "Top", KidUI.ACCENT,
                                  new Vector2(0.36f, 0.988f), new Vector2(0.64f, 0.995f),
                                  Vector2.zero, Vector2.zero, 4f);
        pill.GetComponent<Image>().raycastTarget = false;

        var title = KidUI.Txt(_card, "Title", "ANTES DE EMPEZAR", Color.white, 40,
                              new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.98f));
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 3f;

        KidUI.Txt(_card, "Sub", "Información para padres, madres y tutores", KidUI.DIM, 20,
                  new Vector2(0.05f, 0.855f), new Vector2(0.95f, 0.90f));

        // Resumen en lenguaje claro
        string resumen =
            "AttentiON es un juego educativo para entrenar la atención, la memoria y otras\n" +
            "funciones ejecutivas en niños de 3 a 10 años.\n\n" +
            "•  Guardamos: el nombre o apodo del niño, su avatar, su tramo de edad y sus\n" +
            "    resultados de juego (aciertos, tiempos, sesiones).\n" +
            "•  TODO se guarda únicamente en ESTE dispositivo. No hay internet, no hay nube,\n" +
            "    no se comparte nada con terceros y no hay publicidad.\n" +
            "•  El área del tutor (protegida por PIN) permite ver informes y BORRAR todos los\n" +
            "    datos del menor en cualquier momento.\n" +
            "•  Los informes son una herramienta complementaria de seguimiento y NO son un\n" +
            "    diagnóstico clínico.";
        var resT = KidUI.Txt(_card, "Resumen", resumen, Color.white, 19,
                             new Vector2(0.06f, 0.475f), new Vector2(0.94f, 0.845f));
        resT.alignment = TextAlignmentOptions.TopLeft;
        resT.lineSpacing = 20f;          // interlineado cómodo
        resT.paragraphSpacing = 12f;

        KidUI.Btn(_card, "Leer la política de privacidad completa", KidUI.BTNC,
                  new Vector2(0.22f, 0.395f), new Vector2(0.78f, 0.455f),
                  ShowPolicy, 18f);

        // ---------------- Puerta parental
        var gate = KidUI.RoundImg(_card, "Gate", new Color(1f, 1f, 1f, 0.05f),
                                  new Vector2(0.06f, 0.155f), new Vector2(0.94f, 0.375f),
                                  Vector2.zero, Vector2.zero, 1.2f);
        gate.GetComponent<Image>().raycastTarget = false;

        KidUI.Txt(gate, "GateLbl", "VERIFICACIÓN DE ADULTO", KidUI.WARN, 16,
                  new Vector2(0.04f, 0.72f), new Vector2(0.96f, 0.95f)).fontStyle = FontStyles.Bold;

        _gateQuestion = KidUI.Txt(gate, "Q", "", Color.white, 26,
                                  new Vector2(0.04f, 0.34f), new Vector2(0.45f, 0.70f));
        _gateQuestion.fontStyle = FontStyles.Bold;
        _gateQuestion.alignment = TextAlignmentOptions.MidlineLeft;

        _gateStatus = KidUI.Txt(gate, "S", "Resuelve la operación para poder aceptar.",
                                KidUI.DIM, 15,
                                new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.32f));
        _gateStatus.alignment = TextAlignmentOptions.MidlineLeft;

        NewGateChallenge(gate);

        // ---------------- Aceptar / salir
        _acceptBtn = KidUI.Btn(_card, "SOY EL TUTOR Y ACEPTO", KidUI.GOOD,
                               new Vector2(0.08f, 0.035f), new Vector2(0.60f, 0.13f),
                               Accept, 24f);
        _acceptImg = _acceptBtn.GetComponent<Image>();
        SetAcceptEnabled(false);

        KidUI.Btn(_card, "Salir", KidUI.BAD,
                  new Vector2(0.66f, 0.035f), new Vector2(0.92f, 0.13f),
                  () =>
                  {
                      Application.Quit();
#if UNITY_EDITOR
                      UnityEditor.EditorApplication.isPlaying = false;
#endif
                  }, 20f);

        UITween.PopIn(_card, 0.35f, 0.9f);
    }

    // ---------------------------------------------------------------- Puerta parental

    void NewGateChallenge(RectTransform gate)
    {
        _a = Random.Range(6, 10);
        _b = Random.Range(6, 10);
        _gateQuestion.text = $"¿Cuánto es {_a} × {_b}?";

        // Tres respuestas: una correcta y dos distractores plausibles
        int correct = _a * _b;
        int[] answers = { correct, correct + Random.Range(2, 7), correct - Random.Range(2, 7) };
        // Baraja
        for (int i = 0; i < answers.Length; i++)
        {
            int j = Random.Range(i, answers.Length);
            (answers[i], answers[j]) = (answers[j], answers[i]);
        }

        // Limpia botones anteriores
        foreach (Transform t in gate)
            if (t.name.StartsWith("Btn_")) Destroy(t.gameObject);

        for (int i = 0; i < 3; i++)
        {
            float x0 = 0.50f + i * 0.16f;
            int val = answers[i];
            KidUI.Btn(gate, val.ToString(), KidUI.PANEL2,
                      new Vector2(x0, 0.34f), new Vector2(x0 + 0.14f, 0.70f),
                      () => OnGateAnswer(val, gate), 22f);
        }
    }

    void OnGateAnswer(int val, RectTransform gate)
    {
        if (_gatePassed) return;
        if (val == _a * _b)
        {
            _gatePassed = true;
            _gateStatus.text = "Verificado. Ya puedes aceptar.";
            _gateStatus.color = KidUI.GOOD;
            GameFeel.PlayPop();
            SetAcceptEnabled(true);
        }
        else
        {
            _gateStatus.text = "Respuesta incorrecta. Nueva operación.";
            _gateStatus.color = KidUI.BAD;
            GameFeel.PlayError();
            NewGateChallenge(gate);
        }
    }

    void SetAcceptEnabled(bool on)
    {
        if (_acceptBtn != null) _acceptBtn.interactable = on;
        if (_acceptImg != null)
            _acceptImg.color = on ? KidUI.GOOD
                                  : new Color(KidUI.GOOD.r, KidUI.GOOD.g, KidUI.GOOD.b, 0.30f);
    }

    void Accept()
    {
        if (!_gatePassed) return;
        if (ProfileManager.Instance != null)
            ProfileManager.Instance.GrantConsent();
        GameFeel.PlaySuccess();
        var cb = _onAccepted;
        Destroy(gameObject);
        cb?.Invoke();
    }

    // ---------------------------------------------------------------- Política completa

    void ShowPolicy()
    {
        // Visor compartido (el mismo que se abre desde el menú ESC).
        PolicyViewer.Show();
    }

    /// <summary>Texto completo (misma redacción que POLITICA_PRIVACIDAD.md en la raíz
    /// del proyecto — mantener ambos sincronizados si se edita).</summary>
    public static string PolicyText()
    {
        return
"POLÍTICA DE PRIVACIDAD DE AttentiON — versión " + POLICY_VERSION + @"

1. RESPONSABLE
AttentiON es una aplicación educativa para el entrenamiento de funciones ejecutivas
en niños de 3 a 10 años. El responsable del tratamiento es el titular de la aplicación
(contacto: unaifdezcobos@gmail.com).

2. QUÉ DATOS SE TRATAN
· Datos de perfil creados por el tutor: nombre o apodo del menor, avatar elegido y
  tramo de edad (3-5, 5-7 o 7-10 años).
· Datos de uso generados al jugar: sesiones (fecha, duración, dificultad) y
  resultados de los minijuegos (aciertos, errores, tiempos de reacción, puntuación).
· PIN del tutor: se guarda únicamente un resumen criptográfico (hash SHA-256),
  nunca el PIN en claro.
No se recogen: apellidos, imágenes, audio, localización, contactos, identificadores
publicitarios ni ningún otro dato del dispositivo.

3. DÓNDE SE GUARDAN
Todos los datos se almacenan EXCLUSIVAMENTE en este dispositivo, en la carpeta de
datos local de la aplicación. AttentiON funciona sin conexión: no envía ni recibe
datos por internet, no usa servidores, no usa servicios de análisis de terceros y
no muestra publicidad.

4. FINALIDAD
Los datos se usan únicamente para: (a) personalizar la experiencia del niño
(dificultad, progreso, logros) y (b) generar informes locales de seguimiento para
el tutor o profesional (Excel, HTML, CSV). Los informes son una herramienta
complementaria de observación y NO constituyen una evaluación clínica ni un
diagnóstico.

5. CONSENTIMIENTO
La aplicación requiere que un adulto (padre, madre o tutor legal) acepte esta
política antes del primer uso, mediante una verificación de adulto. El tratamiento
de los datos del menor se basa en este consentimiento.

6. DERECHOS Y CONTROL
Desde el área del tutor (protegida por PIN) se puede en todo momento:
· Consultar todos los datos de cada menor (informes).
· Exportar los datos (Excel/CSV/HTML).
· Borrar los datos de un menor concreto o TODA la base de datos.
El borrado es inmediato e irreversible. Al desinstalar la aplicación, los datos
locales se eliminan con ella.

7. CONSERVACIÓN
Los datos se conservan en el dispositivo hasta que el tutor los borre o desinstale
la aplicación. No existe copia externa.

8. MENORES
AttentiON está diseñada para ser usada por menores BAJO SUPERVISIÓN de un adulto.
El área del tutor y las acciones sensibles (informes, borrado) están protegidas
por PIN y verificación de adulto.

9. CAMBIOS EN ESTA POLÍTICA
Si esta política cambia, la aplicación volverá a solicitar la aceptación de un
adulto en el siguiente arranque, indicando la nueva versión.";
    }
}
