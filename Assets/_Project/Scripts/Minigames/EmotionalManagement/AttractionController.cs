using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AttractionController : MonoBehaviour
{

    public struct Stimulus
    {
        public Vector2       canvasPos;
        public float         influenceRadius;
        public float         strength;
        public float         contactRadius;
        public RectTransform visual;
    }

    public List<Stimulus> Stimuli { get; private set; } = new List<Stimulus>();

    public void BuildStimuli(RectTransform parent, List<Vector2> positions,
                             float strength, float influenceRadius, float contactRadius)
    {
        Stimuli.Clear();

        foreach (var pos in positions)
        {

            var go = new GameObject("Stimulus");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = Vector2.one * contactRadius * 2f;

            var haloGO = new GameObject("Halo");
            haloGO.transform.SetParent(rt, false);
            var haloRT = haloGO.AddComponent<RectTransform>();
            haloRT.anchorMin = new Vector2(0.5f, 0.5f);
            haloRT.anchorMax = new Vector2(0.5f, 0.5f);
            haloRT.sizeDelta = Vector2.one * influenceRadius * 2f;
            haloRT.anchoredPosition = Vector2.zero;
            var haloImg = haloGO.AddComponent<Image>();
            haloImg.sprite = AttractionUIController.MakeCircleSprite(128);
            haloImg.color  = new Color(0.90f, 0.18f, 0.18f, 0.07f);

            var ringGO = new GameObject("Ring");
            ringGO.transform.SetParent(rt, false);
            var ringRT = ringGO.AddComponent<RectTransform>();
            ringRT.anchorMin = new Vector2(0.5f, 0.5f);
            ringRT.anchorMax = new Vector2(0.5f, 0.5f);
            ringRT.sizeDelta = Vector2.one * contactRadius * 2.5f;
            ringRT.anchoredPosition = Vector2.zero;
            var ringImg = ringGO.AddComponent<Image>();
            ringImg.sprite = AttractionUIController.MakeCircleSprite(128);
            ringImg.color  = new Color(0.90f, 0.20f, 0.20f, 0.28f);

            var bodyImg = go.AddComponent<Image>();
            bodyImg.sprite = AttractionUIController.MakeCircleSprite(128);
            bodyImg.color  = new Color(0.92f, 0.18f, 0.22f, 0.95f);

            var glowGO = new GameObject("Glow");
            glowGO.transform.SetParent(rt, false);
            var glowRT = glowGO.AddComponent<RectTransform>();
            glowRT.anchorMin = new Vector2(0.5f, 0.5f);
            glowRT.anchorMax = new Vector2(0.5f, 0.5f);
            glowRT.sizeDelta = Vector2.one * contactRadius * 0.5f;
            glowRT.anchoredPosition = new Vector2(-contactRadius * 0.18f, contactRadius * 0.18f);
            var glowImg = glowGO.AddComponent<Image>();
            glowImg.sprite = AttractionUIController.MakeCircleSprite(64);
            glowImg.color  = new Color(1f, 0.55f, 0.55f, 0.55f);

            Stimuli.Add(new Stimulus
            {
                canvasPos       = pos,
                influenceRadius = influenceRadius,
                strength        = strength,
                contactRadius   = contactRadius,
                visual          = rt
            });
        }
    }

    public Vector2 CalculateTotalForce(Vector2 cursorCanvasPos)
    {
        Vector2 total = Vector2.zero;

        foreach (var s in Stimuli)
        {
            Vector2 toStimulus = s.canvasPos - cursorCanvasPos;
            float   dist       = toStimulus.magnitude;

            if (dist > s.influenceRadius || dist < 0.5f) continue;

            float t         = 1f - (dist / s.influenceRadius);
            float magnitude = s.strength * t * t;

            total += toStimulus.normalized * magnitude;
        }

        return total;
    }

    public bool IsTouchingAny(Vector2 cursorCanvasPos, float cursorRadius)
    {
        foreach (var s in Stimuli)
        {
            if (Vector2.Distance(cursorCanvasPos, s.canvasPos) < s.contactRadius + cursorRadius)
                return true;
        }
        return false;
    }

    public float GetDangerLevel(Vector2 cursorCanvasPos)
    {
        float maxDanger = 0f;
        foreach (var s in Stimuli)
        {
            float dist   = Vector2.Distance(cursorCanvasPos, s.canvasPos);
            float danger = 1f - Mathf.Clamp01(dist / s.influenceRadius);
            if (danger > maxDanger) maxDanger = danger;
        }
        return maxDanger;
    }
}
