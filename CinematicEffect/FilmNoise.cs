using System;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
[VolumeComponentMenu("Custom/Film Noise")]
public sealed class FilmNoise : VolumeComponent
{
    public BoolParameter enabledEffect = new(false);
    public ClampedFloatParameter intensity = new(0f, 0f, 1f);
    public ClampedIntParameter scratchFreq = new(200, 50, 300);
    public ClampedIntParameter speed = new(50, 30, 80);
    public ClampedIntParameter noiseFreq = new(30, 30, 100);
    public ClampedFloatParameter threshold = new(0.16f, 0.1f, 0.3f);
    public ColorParameter noiseColor = new(Color.white, false, false, true);
    public TextureParameter filmDartTexture = new(null);
    public ClampedIntParameter flipWidth = new(100, 1, 512);
    public ClampedIntParameter flipHeight = new(100, 1, 512);
    public ClampedFloatParameter flipSec = new(0.2f, 0.1f, 1f);
}
