import bpy,json,math
from mathutils import Vector,Quaternion
from pathlib import Path
out=Path('/Users/ygokdogan/Developer/Unity/WesternSamurai/work/ZincirSurukleyen_KayKit')
handparent=bpy.data.objects['ZS_Hand_R'];handparent.name='ZS_Hand_Pivot'
hands=list(handparent.children)
bpy.ops.object.select_all(action='DESELECT')
for o in hands:o.select_set(True)
bpy.context.view_layer.objects.active=hands[0];bpy.ops.object.join();hand=bpy.context.object;hand.name='ZS_Hand_R'
# Put the hand and weapon pivot at the grip while preserving visible placement.
for parent,pos in [(handparent,(.73,-.235,.83)),(bpy.data.objects['ZS_Weapon_Chain'],(.79,-.25,.71))]:
 kids=[(o,o.matrix_world.copy()) for o in parent.children];parent.location=pos;bpy.context.view_layer.update()
 for o,m in kids:o.matrix_world=m
 handparent.empty_display_size=.07
bpy.data.objects['ZS_Character'].empty_display_size=.08
bpy.data.objects['ZS_Weapon_Chain'].empty_display_size=.08
bpy.ops.object.select_all(action='DESELECT')
for screen in bpy.data.screens:
 for area in screen.areas:
  for space in area.spaces:
   if space.type=='VIEW_3D':
    space.overlay.show_relationship_lines=False;space.overlay.show_extras=False
    space.shading.show_cavity=False;space.shading.show_shadows=True
    space.region_3d.view_rotation=Quaternion((0,0,1),math.radians(-12)) @ Quaternion((1,0,0),math.radians(77))
    space.region_3d.view_location=(2.05,0,1.02);space.region_3d.view_distance=6.9
# Validate original reference against build-time snapshot, plus separated hand and weapon.
assert all(signature(o)==before[o.name] for o in refs)
newmeshes=[o for o in col.objects if o.type=='MESH']
report={'reference_unchanged':True,'hand_object':hand.name,'weapon_parent':'ZS_Weapon_Chain','chain_links':8,'mesh_objects':len(newmeshes),'vertices':sum(len(o.data.vertices) for o in newmeshes),'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in newmeshes)}
(out/'validation.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(out/'ZincirSurukleyen_KayKit.blend'))
print(report)
