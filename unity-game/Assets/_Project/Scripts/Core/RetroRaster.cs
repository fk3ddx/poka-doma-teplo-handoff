using UnityEngine;
namespace Ataka
{
    public enum RasterStyle { Standard = 0, Clear = 1, Retro = 2 }
    public static class RetroRaster
    {
        public static int TargetHeight(RasterStyle style) => style == RasterStyle.Clear ? 720 : style == RasterStyle.Retro ? 360 : 540;
        public static float Scale(RasterStyle style, int outputHeight)
        {
            if (outputHeight <= 0) return 1f;
            // Never supersample small windows. URP supports scales from .1 to 2.
            return Mathf.Clamp(TargetHeight(style) / (float)outputHeight, .1f, 1f);
        }
    }
}
