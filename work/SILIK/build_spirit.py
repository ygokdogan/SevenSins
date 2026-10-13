"""Build the SILIK spirit from the current KayKit source; run in Blender."""
import bpy, math, random, bmesh, json
from mathutils import Vector
from pathlib import Path
P=Path('/Users/ygokdogan/Developer/Unity/WesternSamurai/work/SILIK')
random.seed(12)
scene=bpy.context.scene
source=[o for o in scene.objects if o.name.startswith('character_skeleton_warrior_')]
assert len(source)>=5, 'Expected live KayKit source meshes'
archive=bpy.data.collections.new('REFERENCE • original KayKit (hidden)')
scene.collection.children.link(archive)
for o in source:
 for c in list(o.users_collection):c.objects.unlink(o)
 archive.objects.link(o)
 o.hide_set(False)
archive.hide_render=True
archive.hide_viewport=True
asset=bpy.data.collections.new('SILIK • Forgotten Spirit')
scene.collection.children.link(asset)
root=bpy.data.objects.new('SILIK_Root',None);asset.objects.link(root)
root.empty_display_size=.15
root['design']='Araf / Forgotten spirit. Original KayKit skull and proportions.'
root['palette']='Ash cloth with a trace of blue-green; warm chalk bone; aged iron.'

def mat(name,color,rough=.9,metal=0):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
 bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*color,1);bs.inputs['Roughness'].default_value=rough;bs.inputs['Metallic'].default_value=metal
 return m
cloth=mat('SILIK | ash • muted blue-grey',(.255,.285,.292))
cloth2=mat('SILIK | worn cloth light',(.305,.327,.331))
cloth3=mat('SILIK | cloth shadow',(.214,.235,.244))
lining=mat('SILIK | dark inner fabric',(.105,.119,.124))
bone=mat('SILIK | warm chalk',(.59,.575,.53))
socket=mat('SILIK | hollow charcoal',(.047,.057,.058))
eye=mat('SILIK | faint soul light',(.63,.70,.68))
eye.node_tree.nodes.get('Principled BSDF').inputs['Emission Color'].default_value=(.37,.47,.43,1)
eye.node_tree.nodes.get('Principled BSDF').inputs['Emission Strength'].default_value=.18
iron=mat('SILIK | dull old iron',(.16,.178,.17),.76,.36)

def mesh(name,vs,fs,mats,indices=None,thick=0):
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update()
 ob=bpy.data.objects.new(name,me);asset.objects.link(ob);ob.parent=root
 for m in mats:me.materials.append(m)
 for p in me.polygons:
  p.use_smooth=False
  if indices:p.material_index=indices[p.index%len(indices)]
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
 if thick:
  mod=ob.modifiers.new('Cloth edge thickness','SOLIDIFY');mod.thickness=thick;mod.offset=0;mod.use_rim=True
 return ob

def duplicate(part,name):
 src=next(o for o in source if ('_'+part+'.') in o.name)
 ob=src.copy();ob.data=src.data.copy();asset.objects.link(ob);ob.parent=None;ob.matrix_world=src.matrix_world.copy();ob.parent=root;ob.name=name;ob.hide_set(False)
 for i,m in enumerate(ob.data.materials):
  ob.data.materials[i]=socket if 'Black' in m.name else eye if 'Glow' in m.name else bone
 for p in ob.data.polygons:p.use_smooth=False
 return ob
head=duplicate('head','SILIK_Skull • original KayKit')
jaw=duplicate('jaw','SILIK_Jaw • original KayKit')
eyes=duplicate('eyes','SILIK_Eyes')
# Keep the source ribs only, removing the old armour/belt from the copy.
src=next(o for o in source if '_body.' in o.name)
faces=[p for p in src.data.polygons if 'White' in src.data.materials[p.material_index].name]
ids=sorted({i for p in faces for i in p.vertices});remap={v:i for i,v in enumerate(ids)}
ribs=mesh('SILIK_Ribcage • original KayKit',[src.matrix_world@src.data.vertices[i].co for i in ids],[[remap[i] for i in p.vertices] for p in faces],[bone])
# A close-fitting cloth shell. The front opening is deliberately irregular.
inner=[(0,1.405),(.28,1.405),(.445,1.29),(.475,1.055),(.39,.80),(.25,.69),(0,.66),(-.25,.70),(-.43,.81),(-.485,1.065),(-.44,1.30),(-.275,1.43)]
outer=[(.015,1.79),(.36,1.715),(.57,1.475),(.585,1.075),(.525,.72),(.28,.625),(.01,.64),(-.30,.65),(-.555,.76),(-.585,1.105),(-.56,1.50),(-.325,1.745)]
n=len(inner);vs=[]
for i,(x,z) in enumerate(inner):vs.append((x,-.485+(.05 if i in (4,5,6,7,8) else 0),z))
for i,(x,z) in enumerate(outer):vs.append((x,-.21 if i not in (0,1,11) else -.10,z))
for i,(x,z) in enumerate(outer):vs.append((x*.92,.24,1.205+(z-1.205)*.94))
for i,(x,z) in enumerate(outer):vs.append((x*.57,.475,1.205+(z-1.205)*.59))
vs.append((0,.55,1.21));fs=[];mi=[]
for ring in range(3):
 for i in range(n):
  j=(i+1)%n;a=ring*n+i;b=ring*n+j;c=(ring+1)*n+j;d=(ring+1)*n+i
  # small triangular rents reveal the darker cloth lining, not a regular pattern
  if (ring,i) in [(0,10),(1,3)]:
   fs.append((a,b,d));mi.append(0)
  else:
   fs.extend([(a,b,c),(a,c,d)]);mi.extend([random.choices([0,1,2],[8,1,1])[0],0])
for i in range(n):fs.append((36+i,36+(i+1)%n,48));mi.append(0)
hood=mesh('SILIK_Hood • torn faceted cowl',vs,fs,[cloth,cloth2,cloth3,lining],mi,.018)
hood.modifiers[0].material_offset=3;hood.modifiers[0].material_offset_rim=3
# Folded front edge forms a dark shadow inside the cowl.
rv=[]
for x,y,z in vs[:12]:rv.append((x,y,z))
for x,y,z in vs[:12]:rv.append((x*.98,y+.045,1.17+(z-1.17)*.98))
mesh('SILIK_Hood inner hem',rv,[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)],[lining],thick=.007)
# Torn fabric strips, each with an off-centre fold ridge.
def panel(name,rows,material=cloth,thick=.012):
 verts=[]
 for left,mid,right in rows:verts.extend([left,mid,right])
 faces=[]
 for r in range(len(rows)-1):
  a=r*3;b=a+3
  faces.extend([(a,b,b+1),(a,b+1,a+1),(a+1,b+1,b+2),(a+1,b+2,a+2)])
 return mesh(name,verts,faces,[material,material if 'fading' in material.name else cloth2,material if 'fading' in material.name else cloth3],[0,0,0,1,0,0,2,0],thick)
# Front opening leaves three original ribs exposed.
panel('SILIK_Mantle left',[
 [(-.10,-.19,.73),(-.24,-.28,.735),(-.35,-.17,.70)],
 [(-.18,-.315,.57),(-.36,-.35,.55),(-.47,-.20,.50)],
 [(-.225,-.33,.31),(-.37,-.37,.25),(-.505,-.21,.285)],
 [(-.24,-.28,.245),(-.355,-.31,.10),(-.435,-.21,.22)]])
panel('SILIK_Mantle right',[
 [(.09,-.18,.72),(.25,-.30,.715),(.365,-.14,.68)],
 [(.09,-.33,.52),(.29,-.375,.51),(.46,-.21,.49)],
 [(.16,-.36,.25),(.35,-.40,.22),(.48,-.21,.265)],
 [(.18,-.32,.125),(.30,-.34,.015),(.43,-.20,.16)]],cloth3)
# Radial back and flank panels with irregular tapered hems.
for k,ang in enumerate([35,76,117,158,199,240]):
 a=math.radians(ang);w=math.radians(27)
 rows=[]
 for r,(radius,z) in enumerate([(.21,.73),(.405,.54),(.43,.25),(.35,.08)]):
  row=[]
  for j,off in enumerate([-w,0,w]):
   rr=radius+(.032 if j==1 else 0);zz=z
   if r==3:zz += ([.055,-.10,.09][j]+.035*math.sin(k*2.8))
   row.append((rr*math.cos(a+off),rr*math.sin(a+off),zz))
  rows.append(row)
 panel('SILIK_Back rag %02d'%k,rows,cloth if k%2 else cloth3)
# A diagonal lower cloth fragment breaks the waistline.
panel('SILIK_Loose front wrap',[
 [(-.25,-.27,.29),(-.07,-.31,.27),(.18,-.27,.23)],
 [(-.27,-.32,.135),(-.035,-.36,.10),(.16,-.30,.07)],
 [(-.19,-.30,.065),(-.025,-.32,-.02),(.12,-.27,.02)]],cloth2)
# Distinct fragmented tails use a real vertical alpha fade in Blender.
fades=[]
for k in range(3):
 m=mat('SILIK | fading cloth %d'%k,(.30+k*.028,.335+k*.028,.34+k*.028))
 nt=m.node_tree;bs=nt.nodes.get('Principled BSDF');geo=nt.nodes.new('ShaderNodeNewGeometry');sep=nt.nodes.new('ShaderNodeSeparateXYZ');rng=nt.nodes.new('ShaderNodeMapRange')
 rng.inputs['From Min'].default_value=-.40;rng.inputs['From Max'].default_value=.12;rng.inputs['To Min'].default_value=.03;rng.inputs['To Max'].default_value=.94;rng.clamp=True
 nt.links.new(geo.outputs['Position'],sep.inputs[0]);nt.links.new(sep.outputs['Z'],rng.inputs['Value']);nt.links.new(rng.outputs[0],bs.inputs['Alpha'])
 m.surface_render_method='DITHERED';fades.append(m)
for k,(cx,cy,tip) in enumerate([(-.24,-.04,-.25),(.02,-.14,-.37),(.22,.07,-.24)]):
 rows=[[(cx-.11,cy,.075),(cx,cy-.04,.10),(cx+.10,cy,.035)],[(cx-.065,cy-.015,-.07),(cx+.025,cy-.04,-.11),(cx+.11,cy+.01,-.055)],[(cx+.045,cy+.015,tip+.055),(cx+.085,cy+.005,tip),(cx+.105,cy+.025,tip+.085)]]
 panel('SILIK_Fading wisp %d'%(k+1),rows,fades[k],.006)
# Small low-poly oval links with actual open ends on the last link.
def link(name,center,rx,rz,tube,twist=0,broken=False):
 seg=10;cross=4;verts=[];faces=[]
 start=math.radians(43) if broken else 0;span=math.radians(275) if broken else 2*math.pi
 rings=seg+1 if broken else seg
 for i in range(rings):
  t=start+span*i/seg
  for j in range(cross):
   q=2*math.pi*j/cross
   xx=(rx+tube*math.cos(q))*math.sin(t);yy=tube*math.sin(q);zz=(rz+tube*math.cos(q))*math.cos(t)
   verts.append((center[0]+xx*math.cos(twist)-yy*math.sin(twist),center[1]+xx*math.sin(twist)+yy*math.cos(twist),center[2]+zz))
 for i in range(seg):
  ni=(i+1)%rings
  for j in range(cross):faces.append((i*cross+j,ni*cross+j,ni*cross+(j+1)%cross,i*cross+(j+1)%cross))
 if broken:faces.extend([tuple(range(cross-1,-1,-1)),tuple((rings-1)*cross+j for j in range(cross))])
 return mesh(name,verts,faces,[iron])
link('SILIK_Shoulder anchor',(.365,-.26,.675),.056,.062,.017,.2)
link('SILIK_Chain link 01',(.397,-.285,.581),.031,.062,.013,1.12)
link('SILIK_Chain link 02',(.417,-.306,.487),.035,.061,.014,.08)
link('SILIK_Chain broken end',(.435,-.317,.391),.037,.051,.014,.12,True)
# Separate pivot-friendly pieces, without claiming a completed animation rig.
for ob in list(asset.objects):
 if ob.type=='MESH':
  ob['asset_role']='character mesh';ob['style']='KayKit compatible low-poly'
# Presentation collection; excluded from FBX character export.
studio=bpy.data.collections.new('STUDIO • preview only');scene.collection.children.link(studio)
def studio_obj(name,data,loc):
 ob=bpy.data.objects.new(name,data);studio.objects.link(ob);ob.location=loc;return ob
def aim(ob,point):ob.rotation_euler=(Vector(point)-ob.location).to_track_quat('-Z','Y').to_euler()
cam=studio_obj('SILIK_PreviewCamera',bpy.data.cameras.new('SILIK_PreviewCamera'),(2.45,-5.9,2.55));aim(cam,(0,0,.72));cam.data.type='ORTHO';cam.data.ortho_scale=2.75;scene.camera=cam
for name,loc,power,size in [('Key',(-3,-4,6),420,4),('Fill',(4,-2,3),240,4),('Rim',(1,3,4),430,3)]:
 data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;ob=studio_obj(name,data,loc);aim(ob,(0,0,.8))
floor=mat('SILIK | studio backdrop',(.19,.21,.22))
me=bpy.data.meshes.new('ground');me.from_pydata([(-200,-200,-.45),(200,-200,-.45),(200,200,-.45),(-200,200,-.45)],[],[(0,1,2,3)]);me.materials.append(floor);studio_obj('Ground • preview only',me,(0,0,0))
scene.world=bpy.data.worlds.new('SILIK neutral world');scene.world.use_nodes=True;scene.world.node_tree.nodes.get('Background').inputs[0].default_value=(.28,.30,.32,1);scene.world.node_tree.nodes.get('Background').inputs[1].default_value=.45
scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True
scene.render.resolution_x=1200;scene.render.resolution_y=1400;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.filepath=str(P/'SILIK_spirit_preview.png')
scene.view_settings.view_transform='AgX'
# Keep viewport focused on character rather than studio equipment.
for ob in studio.objects:ob.hide_set(True)
for area in (bpy.context.screen.areas if bpy.context.screen else []):
 if area.type=='VIEW_3D':
  sp=area.spaces.active;sp.show_region_ui=False;sp.show_region_toolbar=True;sp.overlay.show_floor=False;sp.overlay.show_axis_x=False;sp.overlay.show_axis_y=False
  sp.shading.type='MATERIAL';sp.shading.studiolight_rotate_z=.35
  sp.region_3d.view_rotation=cam.rotation_euler.to_quaternion();sp.region_3d.view_distance=3.65;sp.region_3d.view_location=(0,0,.72);sp.region_3d.view_perspective='ORTHO'
bpy.ops.object.select_all(action='DESELECT')
bpy.context.view_layer.objects.active=head
# Place the independent spirit beside the completely preserved source.
archive.name='KAYKIT • Original reference'
archive.hide_viewport=False
archive.hide_render=False
original_root=bpy.data.objects.new('KAYKIT_Reference_Root',None);archive.objects.link(original_root)
for ob in source:
 if ob.parent not in source:
  mw=ob.matrix_world.copy();ob.parent=original_root;ob.matrix_world=mw
original_root.location.x=-1.10
root.location.x=1.10
cam.location=(3.4,-8,3.0);aim(cam,(0,0,.80));cam.data.ortho_scale=4.85
for area in (bpy.context.screen.areas if bpy.context.screen else []):
 if area.type=='VIEW_3D':
  area.spaces.active.region_3d.view_distance=6.5
  area.spaces.active.region_3d.view_location=(0,0,.78)
  area.spaces.active.region_3d.view_rotation=cam.rotation_euler.to_quaternion()
bpy.ops.wm.save_as_mainfile(filepath=str(P/'SILIK_spirit.blend'))
triangles=sum(len(p.vertices)-2 for o in asset.objects if o.type=='MESH' for p in o.data.polygons)
open(P/'spirit_build_report.json','w').write(json.dumps({'triangles_before_thickness':triangles,'meshes':len([o for o in asset.objects if o.type=='MESH']),'source_preserved':True,'file':bpy.data.filepath},indent=2))
print('SILIK_BUILD_OK',triangles,'base triangles')
