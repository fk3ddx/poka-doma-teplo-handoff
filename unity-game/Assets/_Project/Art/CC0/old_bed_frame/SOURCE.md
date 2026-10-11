# Old bed frame

Luca B / Poly Haven / CC0. https://polyhaven.com/a/old_bed_frame
Existing game-v2 GLB imported, oriented lengthwise, uniformly scaled to 2m, exported FBX with author UV and textures. Verified primary page 2026-10-11. No newly modelled furniture.

Textile geometry is the specifically authorized Claude review02 G2 exception (mattress, coverlet, three pillows), not authored Poly Haven furniture. Reproducible tools/art/prepare_bed_textiles.py. No lace veil yet. Materials use copied CC0 Poly Haven waffle_pique_cotton and quatrefoil_jacquard_fabric textures; originals/import settings untouched.

The copied diffuse maps use Unity TextureImporter R-to-RGB swizzle to retain woven detail without the source mustard/red dye, then the URP material supplies a muted linen/green tint. https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/textureimporter/swizzler
