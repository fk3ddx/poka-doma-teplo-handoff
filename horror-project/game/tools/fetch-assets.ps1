$ErrorActionPreference = 'Stop'
$gameRoot = Split-Path $PSScriptRoot -Parent
$assetRoot = Join-Path $gameRoot 'assets'
$manifest = @()
foreach ($id in @('Television_01','Sofa_01','CoffeeTable_01','vintage_cabinet_01','potted_plant_02','electric_stove','vintage_electric_kettle','SchoolChair_01','wall_clock')) {
  $data = Invoke-RestMethod -Uri "https://api.polyhaven.com/files/$id"
  $entry = $data.gltf.'1k'.gltf
  $dir = Join-Path $assetRoot $id
  New-Item -ItemType Directory -Force -Path $dir | Out-Null
  Invoke-WebRequest -Uri $entry.url -OutFile (Join-Path $dir 'model.gltf')
  foreach ($item in $entry.include.PSObject.Properties) {
    $dest = Join-Path $dir $item.Name
    New-Item -ItemType Directory -Force -Path (Split-Path $dest -Parent) | Out-Null
    Invoke-WebRequest -Uri $item.Value.url -OutFile $dest
  }
  $manifest += @{id=$id;source="https://polyhaven.com/a/$id";license='CC0';resolution='1k';type='gltf'}
  Write-Output "Downloaded model $id"
}
foreach ($id in @('wood_floor_worn','wood_table_001')) {
  $data = Invoke-RestMethod -Uri "https://api.polyhaven.com/files/$id"
  $dir=Join-Path $assetRoot $id
  New-Item -ItemType Directory -Force -Path $dir | Out-Null
  foreach ($map in @('Diffuse','nor_gl','Rough')) {
    $entry=$data.$map.'1k'.jpg
    if ($entry) { Invoke-WebRequest -Uri $entry.url -OutFile (Join-Path $dir "$map.jpg") }
  }
  $manifest += @{id=$id;source="https://polyhaven.com/a/$id";license='CC0';resolution='1k';type='texture'}
  Write-Output "Downloaded texture $id"
}
$manifest | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $assetRoot 'polyhaven-manifest.json') -Encoding utf8
foreach ($rel in @('loaders/GLTFLoader.js','utils/BufferGeometryUtils.js','geometries/RoundedBoxGeometry.js','postprocessing/EffectComposer.js','postprocessing/RenderPass.js','postprocessing/ShaderPass.js','postprocessing/Pass.js','shaders/CopyShader.js','shaders/LuminosityHighPassShader.js','postprocessing/UnrealBloomPass.js','postprocessing/OutputPass.js','shaders/OutputShader.js','postprocessing/SSAOPass.js','shaders/SSAOShader.js','math/SimplexNoise.js','postprocessing/MaskPass.js')) {
 $dest = Join-Path $gameRoot "vendor/addons/$rel"
 New-Item -ItemType Directory -Force -Path (Split-Path $dest -Parent) | Out-Null
 try { Invoke-WebRequest -Uri "https://cdn.jsdelivr.net/npm/three@0.180.0/examples/jsm/$rel" -OutFile $dest } catch { Write-Output "Unavailable addon $rel" }
}
