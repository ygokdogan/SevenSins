import bpy,math,bmesh,json
from mathutils import Vector
from pathlib import Path
P=Path('/Users/ygokdogan/Developer/Unity/WesternSamurai/work/SILIK')
a=bpy.data.collections['SILIK • Forgotten Spirit']
hood=bpy.data.objects['SILIK_Hood • torn faceted cowl']
# Fill most of the missing triangle; preserve a small angular tear.
me=hood.data
bm=bmesh.new();bm.from_mesh(me);bm.verts.ensure_lookup_table()
for ia,ic,id in [(10,23,22),(15,28,27)]:
 bm.verts.ensure_lookup_table()
 va,vc,vd=[bm.verts[i] for i in (ia,ic,id)]
 cen=(va.co+vc.co+vd.co)/3
 hole=[bm.verts.new(cen+(v.co-cen)*.27) for v in (va,vc,vd)]
 outside=[va,vc,vd]
 for j in range(3):
  f=bm.faces.new((outside[j],outside[(j+1)%3],hole[(j+1)%3],hole[j]));f.material_index=0
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
# Smaller soul pupils while retaining the source geometry.
e=bpy.data.objects['SILIK_Eyes'];bm=bmesh.new();bm.from_mesh(e.data)
unvisited=set(bm.verts)
while unvisited:
 seed=unvisited.pop();comp={seed};todo=[seed]
 while todo:
  v=todo.pop()
  for edge in v.link_edges:
   v2=edge.other_vert(v)
   if v2 in unvisited:unvisited.remove(v2);comp.add(v2);todo.append(v2)
 center=sum((v.co for v in comp),Vector())/len(comp)
 for v in comp:v.co=center+(v.co-center)*.55
bm.to_mesh(e.data);bm.free()
bpy.data.objects['SILIK_Ribcage • original KayKit'].location.y-=.105
# Swing the break to the lowest part of the link for an unmistakable open end.
o=bpy.data.objects['SILIK_Chain broken end'];c=Vector((.435,-.317,.391))
for v in o.data.vertices:
 d=v.co-c
 v.co=c+Vector((-d.x,-d.y,-d.z))
# Better asymmetric torn hood tips, below each cheek.
for idx,delta in [(4,-.04),(8,-.045),(16,-.055),(20,-.075),(28,-.02),(32,-.05)]:
 me.vertices[idx].co.z+=delta
# Store a useful material viewport in every saved workspace.
for screen in bpy.data.screens:
 for ar in screen.areas:
  if ar.type=='VIEW_3D':
   sp=ar.spaces.active;sp.show_region_ui=False;sp.overlay.show_floor=False;sp.overlay.show_axis_x=False;sp.overlay.show_axis_y=False
   sp.shading.type='MATERIAL';sp.region_3d.view_distance=5.5;sp.region_3d.view_location=(0,0,.82)
   sp.region_3d.view_rotation=bpy.context.scene.camera.rotation_euler.to_quaternion();sp.region_3d.view_perspective='ORTHO'
bpy.ops.wm.save_as_mainfile(filepath=str(P/'SILIK_spirit.blend'))
# Export only the new character at the origin, excluding source and studio.
bpy.ops.object.select_all(action='DESELECT')
root=bpy.data.objects['SILIK_Root'];root.location.x=0
for ob in a.objects:ob.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.fbx(filepath=str(P/'SILIK_spirit.fbx'),use_selection=True,object_types={'MESH','EMPTY'},use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y')
root.location.x=1.10
# Render the final standalone character.
s=bpy.context.scene;bpy.data.collections['KAYKIT • Original reference'].hide_render=True
cam=s.camera;cam.location=(3.25,-5.8,2.25);cam.rotation_euler=(Vector((1.10,0,.75))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=2.65
s.cycles.samples=32;s.render.resolution_x=1000;s.render.resolution_y=1200
bpy.ops.render.render(write_still=True)
