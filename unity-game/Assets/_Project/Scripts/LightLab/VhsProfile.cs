using UnityEngine;
namespace Ataka.LightLab {
 [CreateAssetMenu(menuName="Ataka/VHS profile")]
 public sealed class VhsProfile : ScriptableObject {
  public float lens=.08f, lumaBlur=1.5f, chromaBlur=8, chromaShift=3, ghostPixels=9, ghostWeight=.08f, aberration=2, grain=.032f, jitter=.4f, scanlines=.025f, disruption;
 }
}
