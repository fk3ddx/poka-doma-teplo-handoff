"""Distinct veneer/solid timber families; names survive native .blend export."""
for room,objects in groups.items():
 for obj in objects:
  key=obj.data.materials[0].name
  if key not in ('Wood','WoodLight'):continue
  unit_name=obj.get('furniture_unit','')
  if unit_name=='Wall unit':new='WoodWalnutTrim' if key=='WoodLight' else 'WoodWalnut'
  elif unit_name=='Coffee table':new='WoodTeak'
  elif unit_name in ('Soviet armchair','Sofa','Shawl chair'):new='WoodBeech'
  elif room in ('Hall','HallDrawer'):new='WoodAged'
  else:continue
  obj.data.materials[0]=mat(new)
