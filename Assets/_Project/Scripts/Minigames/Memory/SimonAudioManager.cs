using UnityEngine;

public class SimonAudioManager : MonoBehaviour
{

    private const int   SAMPLE_RATE = 44100;
    private const float TONE_DURATION  = 0.55f;
    private const float SUCCESS_DURATION = 0.80f;
    private const float FAIL_DURATION    = 0.70f;

    private static readonly float[] COLOR_FREQS = { 261.6f, 329.6f, 392.0f, 523.3f };

    private AudioClip[] _colorClips;
    private AudioClip   _successClip;
    private AudioClip   _failClip;
    private AudioSource _source;

    private void Awake()
    {
        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.volume      = 0.65f;

        GenerateAllClips();
    }

    public void PlayColor(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= _colorClips.Length) return;
        _source.PlayOneShot(_colorClips[colorIndex]);
    }

    public void PlaySuccess() => _source.PlayOneShot(_successClip);

    public void PlayFail() => _source.PlayOneShot(_failClip);

    private void GenerateAllClips()
    {
        _colorClips = new AudioClip[4];
        for (int i = 0; i < 4; i++)
            _colorClips[i] = GenerateTone(COLOR_FREQS[i], TONE_DURATION, 0.55f, WaveShape.Sine);

        _successClip = GenerateSuccess();
        _failClip    = GenerateFail();
    }

    private enum WaveShape { Sine, Square, Triangle }

    private static AudioClip GenerateTone(float freq, float duration, float amplitude,
                                           WaveShape shape = WaveShape.Sine)
    {
        int   samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] data  = new float[samples];

        float attackTime  = 0.02f;
        float releaseTime = 0.10f;

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SAMPLE_RATE;

            float env;
            if (t < attackTime)
                env = t / attackTime;
            else if (t > duration - releaseTime)
                env = (duration - t) / releaseTime;
            else
                env = 1f;

            float wave = shape switch
            {
                WaveShape.Square   => Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t)),
                WaveShape.Triangle => 2f * Mathf.Abs(2f * (freq * t - Mathf.Floor(freq * t + 0.5f))) - 1f,
                _                  => Mathf.Sin(2f * Mathf.PI * freq * t),
            };

            data[i] = wave * env * amplitude;
        }

        var clip = AudioClip.Create($"tone_{freq:F0}Hz", samples, 1, SAMPLE_RATE, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip GenerateSuccess()
    {
        float[] freqs    = { 523.3f, 659.3f, 784.0f };
        float   noteLen  = 0.18f;
        float   total    = SUCCESS_DURATION;
        int     samples  = Mathf.RoundToInt(SAMPLE_RATE * total);
        float[] data     = new float[samples];

        for (int n = 0; n < freqs.Length; n++)
        {
            float start = n * noteLen;
            int   s0    = Mathf.RoundToInt(start * SAMPLE_RATE);
            int   s1    = Mathf.Min(Mathf.RoundToInt((start + noteLen * 1.4f) * SAMPLE_RATE), samples);

            for (int i = s0; i < s1; i++)
            {
                float t   = (float)(i - s0) / SAMPLE_RATE;
                float env = Mathf.Clamp01(1f - t / (noteLen * 1.4f));
                data[i]  += Mathf.Sin(2f * Mathf.PI * freqs[n] * t) * env * 0.4f;
            }
        }

        var clip = AudioClip.Create("success", samples, 1, SAMPLE_RATE, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip GenerateFail()
    {
        int     samples = Mathf.RoundToInt(SAMPLE_RATE * FAIL_DURATION);
        float[] data    = new float[samples];

        float baseFreq = 220f;

        for (int i = 0; i < samples; i++)
        {
            float t    = (float)i / SAMPLE_RATE;
            float env  = Mathf.Clamp01(1f - t / FAIL_DURATION) * 0.5f;
            float freq = Mathf.Lerp(baseFreq, baseFreq * 0.55f, t / FAIL_DURATION);

            float w1 = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t));
            float w2 = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * 1.41f * t));

            data[i] = (w1 * 0.5f + w2 * 0.5f) * env * 0.45f;
        }

        var clip = AudioClip.Create("fail", samples, 1, SAMPLE_RATE, false);
        clip.SetData(data, 0);
        return clip;
    }
}
