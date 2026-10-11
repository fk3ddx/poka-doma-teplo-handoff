using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
namespace Ataka.Tests
{
    public class RetroRasterTests
    {
        [TestCase(RasterStyle.Standard,1080,.5f)]
        [TestCase(RasterStyle.Standard,2160,.25f)]
        [TestCase(RasterStyle.Standard,900,.6f)]
        [TestCase(RasterStyle.Clear,1080,2f/3f)]
        [TestCase(RasterStyle.Retro,1080,1f/3f)]
        [TestCase(RasterStyle.Standard,480,1f)]
        public void RenderHeightIsIndependentOfDisplayResolution(RasterStyle style,int height,float scale)
        { Assert.AreEqual(scale,RetroRaster.Scale(style,height),.00001f); }
        [Test] public void OldSettingsDefaultTo540AndChosenStylePersists()
        {
            var path=Path.GetTempFileName();
            try {
                File.WriteAllText(path,"{\"quality\":0}");var s=GameSettingsStore.Load(path);
                Assert.AreEqual(RasterStyle.Standard,s.rasterStyle);
                s.rasterStyle=RasterStyle.Retro;GameSettingsStore.Save(path,s);
                Assert.AreEqual(RasterStyle.Retro,GameSettingsStore.Load(path).rasterStyle);
            } finally {File.Delete(path);}
        }
        [Test] public void RetroShaderExistsWithoutCompilationErrors()
        {
            var shader=Shader.Find("Ataka/SubtleVHS");Assert.NotNull(shader);
            Assert.IsFalse(ShaderUtil.ShaderHasError(shader));
            var material=new Material(shader);
            try {Assert.AreEqual(64,material.GetFloat("_ColorLevels"));Assert.Greater(material.GetFloat("_DitherStrength"),0);}
            finally {Object.DestroyImmediate(material);}
        }
    }
}
