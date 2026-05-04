using System.Collections.Generic;
using UnityEngine;

public class StopAndGoZoneManager : MonoBehaviour
{

    [System.Serializable]
    public struct SafeZone
    {
        public float startAngle;
        public float spanAngle;
    }

    [Header("Zones")]
    public List<SafeZone> zones = new List<SafeZone>
    {
        new SafeZone { startAngle = 60f, spanAngle = 60f }
    };

    public bool IsInZone(float angleDeg)
    {
        angleDeg = NormalizeAngle(angleDeg);

        foreach (var z in zones)
        {
            float start = NormalizeAngle(z.startAngle);
            float end   = NormalizeAngle(z.startAngle + z.spanAngle);

            if (AngleBetween(angleDeg, start, end))
                return true;
        }
        return false;
    }

    public int GetZoneIndex(float angleDeg)
    {
        angleDeg = NormalizeAngle(angleDeg);
        for (int i = 0; i < zones.Count; i++)
        {
            float start = NormalizeAngle(zones[i].startAngle);
            float end   = NormalizeAngle(zones[i].startAngle + zones[i].spanAngle);
            if (AngleBetween(angleDeg, start, end)) return i;
        }
        return -1;
    }

    static float NormalizeAngle(float a)
    {
        a %= 360f;
        if (a < 0) a += 360f;
        return a;
    }

    static bool AngleBetween(float a, float start, float end)
    {

        if (end >= start)
            return a >= start && a <= end;
        else
            return a >= start || a <= end;
    }
}
