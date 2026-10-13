import bpy, math, random, json, hashlib
from mathutils import Vector, Matrix, Quaternion
from pathlib import Path
out=Path('/Users/ygokdogan/Developer/Unity/WesternSamurai/work/ZincirSurukleyen_KayKit')
refs=list(bpy.context.scene.objects)
def signature(o):
    return {'matrix':[list(r) for r in o.matrix_world], 'verts':[list(v.co) for v in o.data.vertices] if o.type=='MESH' else [],'polygons':[list(p.vertices) for p in o.data.polygons] if o.type=='MESH' else []}
before={o.name:signature(o) for o in refs}
bpy.ops.wm.save_as_mainfile(filepath=str(out/'Reference_session_backup.blend'),copy=True)
col=bpy.data.collections.new('ZINCIR SURUKLEYEN • KayKit'); bpy.context.scene.collection.children.link(col)
def mat(name,c,metal=0):
 m=bpy.data.materials.new(name); m.diffuse_color=(*c,1); m.use_nodes=True
 b=m.node_tree.nodes.get('Principled BSDF'); b.inputs['Base Color'].default_value=(*c,1); b.inputs['Metallic'].default_value=metal; b.inputs['Roughness'].default_value=.83
 return m
cloth=[mat('ZS • Slate '+str(i),c) for i,c in enumerate([(.105,.125,.15),(.123,.144,.173),(.09,.108,.134),(.14,.162,.19)])]
inside=mat('ZS • Hood shadow',(.016,.02,.025)); bone=mat('ZS • Warm ivory',(.68,.63,.51)); iron=mat('ZS • Forged iron',(.095,.087,.075),.65); edge=mat('ZS • Worn edges',(.18,.16,.125),.55); rust=mat('ZS • Muted rust',(.16,.085,.043),.3)
root=bpy.data.objects.new('ZS_Character',None); col.objects.link(root); root.location=(2.55,0,0)
weapon=bpy.data.objects.new('ZS_Weapon_Chain',None); col.objects.link(weapon); weapon.parent=root
handroot=bpy.data.objects.new('ZS_Hand_R',None); col.objects.link(handroot); handroot.parent=root
created=[]
def mesh(name,vs,fs,mats,parent=root,ids=None):
 me=bpy.data.meshes.new(name); me.from_pydata(vs,[],fs); me.materials.clear()
 for m in mats: me.materials.append(m)
 me.update(); o=bpy.data.objects.new(name,me); col.objects.link(o); o.parent=parent
 for p in me.polygons: p.use_smooth=False; p.material_index=ids[p.index] if ids else 0
 created.append(o); return o
# Clean broad cloth planes with alternating inward folds at the hem.
n=16; vs=[]
for k,(z,rx,ry) in enumerate([(.035,.76,.53),(.26,.68,.47),(.8,.48,.35),(1.09,.38,.29)]):
 for j in range(n):
  a=2*math.pi*j/n; f=(1 if j%2==0 else .90) if k<2 else 1
  vs.append((rx*math.cos(a)*f,ry*math.sin(a)*f+.055,z+(.036*(j%2) if k==0 else 0)))
fs=[]; ids=[]
for k in range(3):
 for j in range(n):
  a=k*n+j;b=k*n+(j+1)%n;c=b+n;d=a+n
  fs.append((a,b,c,d)); ids.append([0,1,0,2,0,1,0,3][j%8])
fs.extend([tuple(reversed(range(n))),tuple(range(3*n,4*n))]);ids.extend([2,0])
mesh('ZS_Body_Robe',vs,fs,cloth,ids=ids)
# Hood is a closed thick shell around a true face opening.
outline=[(0,2.06),(.34,2.025),(.55,1.83),(.66,1.48),(.53,1.16),(.30,1.005),(0,.965),(-.30,1.005),(-.53,1.16),(-.66,1.48),(-.55,1.83),(-.34,2.025)]
opening=[(0,1.84),(.26,1.79),(.43,1.64),(.48,1.43),(.38,1.20),(.22,1.12),(0,1.075),(-.22,1.12),(-.38,1.20),(-.48,1.43),(-.43,1.64),(-.26,1.79)]
vs=[]
for ring in range(5):
 for j in range(12):
  x,z=(opening if ring<2 else outline)[j]
  if ring==0: y=-.555
  elif ring==1: y=-.34; x*=.99
  elif ring==2: y=-.50 if j in (0,1,11) else -.49
  elif ring==3: y=.20; x*=.95; z=1.49+(z-1.49)*1.05
  else: y=.51; x*=.64; z=1.48+(z-1.49)*.72
  vs.append((x,y,z))
fs=[]; ids=[]
def bridge(a,b,inner=False):
 for j in range(12):
  fs.append((a*12+j,a*12+(j+1)%12,b*12+(j+1)%12,b*12+j)); ids.append(4 if inner else [0,1,0,2,1,0,2,0,1,2,0,1][j])
bridge(0,1,True); bridge(2,0); bridge(3,2); bridge(4,3)
fs.append(tuple(range(48,60)));ids.append(2)
# Dark inset behind the skull, leaving a deep open hood.
vs.append((0,.38,1.46))
for j in range(12):fs.append((12+j,12+(j+1)%12,60));ids.append(4)
mesh('ZS_Hood',vs,fs,cloth+[inside],ids=ids)
# Duplicate only the reference skull meshes; original geometry, materials and transforms stay intact.
for part in ['head','jaw']:
 src=bpy.data.objects['character_skeleton_warrior_'+part]
 verts=[]
 for v in src.data.vertices:
  p=src.matrix_world@v.co; p=Vector((p.x*.72,p.y*.72-.24,(p.z-1.20)*.72+1.43)); verts.append(tuple(p))
 mats=[inside if m.name=='Black' else bone for m in src.data.materials]
 mesh('ZS_Skull_'+part,verts,[tuple(p.vertices) for p in src.data.polygons],mats,ids=[p.material_index for p in src.data.polygons])
def ico(name,pos,scale,material,parent=root,sub=1):
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1)
 o=bpy.context.object; o.name=name
 for c in list(o.users_collection):c.objects.unlink(o)
 col.objects.link(o);o.parent=parent;o.location=pos;o.scale=scale;o.data.materials.append(material);created.append(o);return o
def segment(name,a,b,r,material,parent=root,vertices=6):
 a,b=Vector(a),Vector(b);d=b-a
 bpy.ops.mesh.primitive_cone_add(vertices=vertices,radius1=r,radius2=r*.86,depth=d.length)
 o=bpy.context.object;o.name=name
 for c in list(o.users_collection):c.objects.unlink(o)
 col.objects.link(o);o.parent=parent;o.location=(a+b)/2;o.rotation_euler=d.to_track_quat('Z','Y').to_euler();o.data.materials.append(material);created.append(o);return o
# One cloth sleeve and a clearly separate clenched skeletal hand.
ico('ZS_Sleeve_R',(.48,.005,.96),(.28,.29,.34),cloth[0],sub=1)
ico('ZS_Sleeve_Opening',(.655,-.175,.855),(.16,.08,.19),inside)
ico('ZS_Palm',(.73,-.235,.83),(.135,.115,.16),bone,handroot,sub=1)
for j in range(3):
 x=.666+j*.062
 segment('ZS_Finger_%02d_A'%j,(x,-.322,.88),(x+.012,-.35,.793),.035,bone,handroot)
 segment('ZS_Finger_%02d_B'%j,(x+.012,-.35,.793),(x+.024,-.282,.758),.03,bone,handroot)
segment('ZS_Thumb',(.825,-.24,.875),(.80,-.35,.817),.044,bone,handroot)
# Flattened octagonal links alternate their planes, interlocked along a hanging-to-ground arc.
points=[(.79,-.25,.71),(.91,-.27,.52),(1.08,-.28,.35),(1.29,-.29,.225),(1.52,-.30,.145),(1.77,-.29,.12),(2.01,-.25,.13),(2.22,-.19,.18)]
for i,p in enumerate(points):
 prev=Vector(points[max(0,i-1)]);nxt=Vector(points[min(len(points)-1,i+1)]); axis=(nxt-prev).normalized()
 u=Vector((0,1,0));u=(u-axis*u.dot(axis)).normalized();v=axis.cross(u).normalized()
 angle=math.radians(18 if i%2==0 else 108); side=u*math.cos(angle)+v*math.sin(angle); normal=axis.cross(side)
 verts=[]
 for j in range(8):
  t=2*math.pi*j/8; center=Vector(p)+axis*(.158*math.cos(t))+side*(.101*math.sin(t)); radial=(axis*math.cos(t)+side*math.sin(t)).normalized()
  for k in range(4):
   q=math.pi/4+k*math.pi/2;verts.append(tuple(center+.034*(radial*math.cos(q)+normal*math.sin(q))))
 faces=[]
 for j in range(8):
  for k in range(4):faces.append((j*4+k,((j+1)%8)*4+k,((j+1)%8)*4+(k+1)%4,j*4+(k+1)%4))
 mesh('ZS_Chain_Link_%02d'%(i+1),verts,faces,[iron,edge,rust],parent=weapon,ids=[1 if j%9==0 else 2 if j%13==0 else 0 for j in range(32)])
ball=ico('ZS_Weight', (2.52,-.15,.255),(.32,.29,.29),iron,weapon,sub=2)
# Decimate creates broad irregular facets like the concept's forged weight.
bpy.context.view_layer.objects.active=ball;ball.select_set(True)
mod=ball.modifiers.new('Broad low poly facets','DECIMATE');mod.ratio=.30;bpy.ops.object.modifier_apply(modifier=mod.name)
ball.data.materials.append(edge);ball.data.materials.append(rust)
random.seed(22)
for p in ball.data.polygons:p.material_index=random.choices([0,1,2],[.82,.11,.07])[0]
segment('ZS_Weight_Collar',(2.23,-.19,.19),(2.35,-.175,.23),.085,iron,weapon,8)
# Recalculate the normals on new geometry only, apply scale and keep useful individual origins.
bpy.ops.object.select_all(action='DESELECT')
for o in created:o.select_set(True)
bpy.context.view_layer.objects.active=created[0]
bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
for o in created:
 o['asset']='Zincir Surukleyen';o['style']='KayKit low poly';
root['notes']='Independent enemy beside unchanged KayKit reference. Hand and chain weapon are separate hierarchies.'
assert all(signature(o)==before[o.name] for o in refs),'Reference modified!'
report={'reference_unchanged':True,'objects':len(created),'vertices':sum(len(o.data.vertices) for o in created),'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in created)}
(out/'validation.json').write_text(json.dumps(report,indent=2))
# View both characters from the front, with a small elevated angle.
for screen in bpy.data.screens:
 for area in screen.areas:
  for space in area.spaces:
   if space.type=='VIEW_3D':
    space.shading.type='SOLID';space.shading.color_type='MATERIAL';space.shading.light='STUDIO';space.shading.show_shadows=True;space.shading.show_cavity=True;space.shading.cavity_type='BOTH'
    space.overlay.show_floor=False;space.overlay.show_axis_x=False;space.overlay.show_axis_y=False
    space.region_3d.view_rotation=Quaternion((1,0,0),math.radians(78));space.region_3d.view_location=(2.1,0,1.02);space.region_3d.view_distance=7.3;space.region_3d.view_perspective='ORTHO'
bpy.ops.object.select_all(action='DESELECT');bpy.context.view_layer.objects.active=None
bpy.ops.wm.save_as_mainfile(filepath=str(out/'ZincirSurukleyen_KayKit.blend'))
print('COMPLETE',report)
