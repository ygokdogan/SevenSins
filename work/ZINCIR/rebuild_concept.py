import bpy,bmesh,math,json,random
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.geometry import delaunay_2d_cdt
P=Path(__file__).parent
random.seed(5)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
for m in list(bpy.data.materials):bpy.data.materials.remove(m)
def mat(name,col,metal=0):
 m=bpy.data.materials.new('ZINCIR | '+name);m.diffuse_color=(*col,1);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*col,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=.84;return m
cloth=mat('slate mourning cloth',(.042,.047,.060));light=mat('ash cloth planes',(.051,.057,.071));shade=mat('heavy fold shadow',(.034,.039,.051));lining=mat('unlit hood interior',(.018,.022,.029));bone=mat('old ivory bone',(.53,.495,.41));bonehi=mat('worn bone planes',(.50,.475,.41));bonesh=mat('bone recess',(.285,.269,.228));black=mat('empty eye sockets',(.012,.013,.014));iron=mat('blackened iron',(.058,.055,.048),.55);wear=mat('worn iron bevels',(.115,.11,.098),.6);rust=mat('quiet umber rust',(.135,.081,.046),.2)
body=[];weapon=[]
def mesh(name,vs,fs,mats,ids=None):
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update();ob=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(ob)
 for m in mats:me.materials.append(m)
 for p in me.polygons:p.material_index=ids[p.index%len(ids)] if ids else 0;p.use_smooth=False
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free();return ob
# Hand drawn cross-sections: the brow projects forward; the crown folds back into a hump.
# clockwise around front opening starting at top center
opening=[(0,1.64),(.26,1.59),(.43,1.47),(.515,1.28),(.48,1.07),(.37,.89),(.19,.80),(0,.77),(-.20,.80),(-.39,.91),(-.49,1.10),(-.515,1.31),(-.43,1.49),(-.26,1.60)]
outer=[(.035,2.00),(.36,1.94),(.65,1.83),(.79,1.51),(.68,1.20),(.51,.96),(.25,.87),(0,.84),(-.27,.86),(-.54,1.00),(-.68,1.23),(-.79,1.53),(-.65,1.83),(-.33,1.945)]
n=len(opening);vs=[]
for i,(x,z) in enumerate(opening):vs.append((x,-.725+(.11 if z<1 else .02 if abs(x)>.45 else 0),z))
for i,(x,z) in enumerate(outer):vs.append((x,-.29 if z>1.9 else -.12 if abs(x)>.60 else -.32,z))
for i,(x,z) in enumerate(outer):vs.append((x*.97,.28,1.43+(z-1.43)*.97))
for i,(x,z) in enumerate(outer):vs.append((x*.62,.57,1.43+(z-1.43)*.65))
vs.append((0,.69,1.46));fs=[];ids=[]
for r in range(3):
 for i in range(n):
  a=r*n+i;b=r*n+(i+1)%n;c=b+n;d=a+n
  fs.extend([(a,b,d),(b,c,d)]);ids.extend([0,1 if (i+r*3)%9==2 else 0])
for i in range(n):fs.append((3*n+i,3*n+(i+1)%n,4*n));ids.append(0)
hood=mesh('Sculpted bowed hood',vs,fs,[cloth,light,shade],ids)
# Continuous thickness and softly worn silhouette, while broad faces stay flat.
bpy.context.view_layer.objects.active=hood
mod=hood.modifiers.new('Cloth shell thickness','SOLIDIFY');mod.thickness=.024;mod.offset=-1;bpy.ops.object.modifier_apply(modifier=mod.name)
mod=hood.modifiers.new('Soft worn hood edges','BEVEL');mod.width=.012;mod.segments=2;mod.limit_method='ANGLE';mod.angle_limit=.42;bpy.ops.object.modifier_apply(modifier=mod.name)
body.append(hood)
# Narrow thick lip and deep dark return wall, deliberately continuous and not torn.
front=vs[:n];lip=[]
for x,y,z in front:lip.append((x,y,z))
for x,y,z in front:lip.append((x*.965,y+.035,1.21+(z-1.21)*.965))
for x,y,z in front:lip.append((x*.93,y+.12,1.21+(z-1.21)*.95))
fs=[];ids=[]
for r in range(2):
 for i in range(n):fs.append((r*n+i,r*n+(i+1)%n,(r+1)*n+(i+1)%n,(r+1)*n+i));ids.append(0 if r==0 else 1)
body.append(mesh('Thick hood opening',lip,fs,[shade,lining],ids))
body.append(mesh('Hood darkness',[(x*.94,-.38,1.21+(z-1.21)*.94) for x,z in opening],[tuple(range(n))],[lining]))
# Broad, irregular mantle planes; no cone and no dense surface noise.
N=18
angles=[2*math.pi*i/N for i in range(N)]
radii=[.88,.90,.86,.93,.87,.91,.86,.91,.87,.88,.85,.93,.89,.92,.85,.90,.86,.91]
verts=[]
for row in range(4):
 for i,a in enumerate(angles):
  if row==0:r=radii[i]*1.08;z=.018+(.042 if i%4==0 else 0)
  elif row==1:r=.77+(.075 if i%3==0 else -.035);z=.40+(.045*math.sin(2*a))
  elif row==2:r=.68+(.075 if i%3==0 else -.01);z=.88+.03*math.cos(a)
  else:r=.42;z=1.19+.06*math.sin(a)
  # broad ridge offsets remain consistent between rows
  y=r*.66*math.sin(a)+.08
  if row==0 and math.sin(a)<-.5:y-=.025
  verts.append((r*math.cos(a),y,z))
faces=[];ids=[]
for r in range(3):
 for i in range(N):
  a=r*N+i;b=r*N+(i+1)%N;c=b+N;d=a+N
  faces.append((a,b,c,d));ids.append(1 if i in (3,9,14) else 2 if i in (4,10) else 0)
faces.extend([tuple(reversed(range(N))),tuple(3*N+i for i in range(N))]);ids.extend([0,0]);body.append(mesh('Broad weighted mantle',verts,faces,[cloth,light,shade],ids))
# One simple shoulder fold joins hood to body, particularly visible on weapon side.
vs=[(.31,-.32,1.20),(.64,-.20,1.14),(.80,-.13,1.03),(.76,-.48,.91),(.56,-.60,.81),(.42,-.52,.97),(.56,.29,1.15),(.73,.30,.99),(.76,.16,.85)]
fs=[(0,1,5),(1,2,3,5),(3,4,5),(1,6,7,2),(2,7,8,3)]
o=mesh('Single heavy shoulder fold',vs,fs,[cloth,light,shade],[0,0,2,1,0]);mod=o.modifiers.new('Cloth thickness','SOLIDIFY');mod.thickness=.025;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name);body.append(o)
# Volumetric skull: actual cranium, cheek bones, maxilla and cut-through sockets.
def ellipsoid(name,loc,scale,material,seg=48,rings=32):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=seg,ring_count=rings,radius=1,location=loc);o=bpy.context.object;o.name=name;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(material);return o
skparts=[ellipsoid('Skull crown',(0,-.49,1.30),(.314,.244,.253),bone),ellipsoid('Maxilla',(0,-.579,1.075),(.172,.151,.126),bone),ellipsoid('Nose bridge',(0,-.65,1.205),(.052,.081,.118),bone)]
for sign in [-1,1]:
 skparts.append(ellipsoid('Cheekbone',(sign*.232,-.558,1.155),(.109,.128,.09),bone))
bpy.ops.object.select_all(action='DESELECT')
for o in skparts:o.select_set(True)
bpy.context.view_layer.objects.active=skparts[0];bpy.ops.object.join();skull=bpy.context.object;skull.name='Volumetric sorrowful skull';skull.data.materials.append(black)
rem=skull.modifiers.new('Sculpt union','REMESH');rem.mode='VOXEL';rem.voxel_size=.009;bpy.ops.object.modifier_apply(modifier=rem.name)
sm=skull.modifiers.new('Bone smoothing','SMOOTH');sm.factor=1.1;sm.iterations=4;bpy.ops.object.modifier_apply(modifier=sm.name)
# Sloping upper lids and rounded lower sockets carve a tired, resigned expression.
eye=[(.047,1.346),(.087,1.316),(.15,1.29),(.235,1.285),(.287,1.303),(.30,1.258),(.284,1.187),(.245,1.142),(.18,1.122),(.11,1.145),(.065,1.205)]
for sign in [-1,1]:
 loop=[(sign*x,z) for x,z in eye];n=len(loop);vs=[(x,y,z) for y in [-1.05,-.40] for x,z in loop];fs=[tuple(reversed(range(n))),tuple(n+i for i in range(n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
 cut=mesh('Eye socket cutter',vs,fs,[bone,bonesh],[1]);bpy.context.view_layer.objects.active=cut
 mod=cut.modifiers.new('Rounded socket contour','BEVEL');mod.width=.016;mod.segments=3;bpy.ops.object.modifier_apply(modifier=mod.name)
 bpy.context.view_layer.objects.active=skull;mod=skull.modifiers.new('Carved socket','BOOLEAN');mod.operation='DIFFERENCE';mod.object=cut;bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cut,do_unlink=True)
 # Deep charcoal well, not a painted surface.
 well=ellipsoid('Dark socket depth',(sign*.171,-.495,1.227),(.134,.04,.118),black,24,16);body.append(well)
loop=[(-.045,1.1),(.045,1.1),(.018,1.183),(-.006,1.206),(-.023,1.17)];n=len(loop)
cut=mesh('Nasal cutter',[(x,y,z) for y in [-.90,-.43] for x,z in loop],[tuple(reversed(range(n))),tuple(n+i for i in range(n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)],[black])
bpy.context.view_layer.objects.active=skull;mod=skull.modifiers.new('Nasal cavity','BOOLEAN');mod.operation='DIFFERENCE';mod.object=cut;bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cut,do_unlink=True)
body.append(ellipsoid('Nose darkness',(0,-.44,1.139),(.044,.015,.065),black,16,12))
bpy.context.view_layer.objects.active=skull
mod=skull.modifiers.new('Subtle bone bevel','BEVEL');mod.width=.006;mod.segments=2;mod.limit_method='ANGLE';mod.angle_limit=.5;bpy.ops.object.modifier_apply(modifier=mod.name)
mod=skull.modifiers.new('Sculpted planar bone','DECIMATE');mod.ratio=.06;bpy.ops.object.modifier_apply(modifier=mod.name);body.append(skull)
# Small blunt upper teeth, no smile or lower jaw.
for i,x in enumerate([-.127,-.067,0,.067,.127]):
 ob=ellipsoid('Blunt tooth',(x,-.655,1.015+(abs(x)*.13)),(.028,.054,.06 if i%2 else .048),bone,10,8);body.append(ob)
for ob in body:
 if ob.name.startswith(('Volumetric','Dark socket','Nose darkness','Blunt tooth')):
  bpy.context.view_layer.update();mw=ob.matrix_world.copy();ob.data.transform(mw);ob.matrix_world=Matrix.Identity(4)
  pivot=Vector((0,-.43,1.26));rot=Matrix.Rotation(math.radians(15),3,'X')
  for v in ob.data.vertices:v.co=pivot+rot@(v.co-pivot)
# Lean the hood and skull together over the chest, matching the bowed side view.
for ob in body:
 if ob.name not in ['Broad weighted mantle','Single heavy shoulder fold']:
  bpy.context.view_layer.update();mw=ob.matrix_world.copy();ob.data.transform(mw);ob.matrix_world=Matrix.Identity(4)
  pv=Vector((0,-.1,1.1));rr=Matrix.Rotation(math.radians(9),3,'X')
  for v in ob.data.vertices:v.co=pv+rr@(v.co-pv)
# Simple mitten-like ivory fist: bevelled mass + broad thumb, not round beads.
def bevelbox(name,loc,scale,material,bev=.06):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(material)
 mod=o.modifiers.new('Broad planar bevel','BEVEL');mod.width=bev;mod.segments=1;bpy.ops.object.modifier_apply(modifier=mod.name);return o
bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=1,location=(.765,-.49,.965));hand=bpy.context.object;hand.name='Simple clenched hand';hand.scale=(.127,.115,.147);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);hand.data.materials.append(bone);weapon.append(hand)
weapon.append(ellipsoid('Single thumb',(.70,-.555,.961),(.06,.045,.078),bone,12,8))
# Chunky rectangular-section oval links. Chamfered ring corners read at FPS distance.
def link(name,c,d,twist=0):
 d=Vector(d).normalized();u=d.cross(Vector((0,1,0))).normalized();v=d.cross(u);u,v=u*math.cos(twist)+v*math.sin(twist),-u*math.sin(twist)+v*math.cos(twist)
 # 12 points around capsule, with thick bevelled rectangular section
 path=[];norms=[]
 for i in range(12):
  a=2*math.pi*i/12;path.append((.098*math.cos(a),.16*math.sin(a)));norms.append(Vector((math.cos(a),math.sin(a))).normalized())
 section=[(-.025,-.022),(-.017,-.032),(.018,-.032),(.032,-.018),(.032,.018),(.018,.032),(-.017,.032),(-.025,.022)]
 vs=[];fs=[];ids=[]
 for i,(xx,zz) in enumerate(path):
  normal=norms[i]
  for off,dep in section:vs.append(Vector(c)+u*(xx+normal.x*off*1.65)+d*(zz+normal.y*off*1.65)+v*dep*1.65)
 for i in range(12):
  for j in range(8):fs.append((i*8+j,((i+1)%12)*8+j,((i+1)%12)*8+(j+1)%8,i*8+(j+1)%8));ids.append(1 if j in [1,5] else 2 if (i*8+j)%37==0 else 0)
 return mesh(name,vs,fs,[iron,wear,rust],ids)
points=[(.84,-.49,.79),(.945,-.485,.565),(1.095,-.48,.36),(1.31,-.49,.205),(1.56,-.515,.13),(1.81,-.54,.135),(2.05,-.53,.205)]
for i,p in enumerate(points):
 d=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)])
 weapon.append(link('Heavy link %02d'%i,p,d,(i%2)*math.pi/2+.20))
weight=bevelbox('Blunt prison weight',(2.43,-.50,.29),(.65,.61,.61),iron,.105);weight.rotation_euler.z=.15
# A deliberately lopsided hewn block, not a pristine bevelled cube.
for v in weight.data.vertices:
 x,y,z=v.co
 v.co.x+=.055*z+.022*y
 v.co.z+=.065*x-.035*y
bpy.context.view_layer.update()
low=min((weight.matrix_world@v.co).z for v in weight.data.vertices);weight.location.z-=low

weight.data.materials.append(wear);weight.data.materials.append(rust)
for p in weight.data.polygons:
 if len(p.vertices)==4 and p.area<.06:p.material_index=1
 elif p.index in [9,15]:p.material_index=2
weapon.append(weight)
# Join two asset meshes only.
def join(obs,name,pivot):
 bpy.ops.object.select_all(action='DESELECT')
 for ob in obs:ob.select_set(True)
 bpy.context.view_layer.objects.active=obs[0];bpy.ops.object.join();o=bpy.context.object;o.name=name;bpy.context.scene.cursor.location=pivot;bpy.ops.object.origin_set(type='ORIGIN_CURSOR');return o
body=join(body,'ZINCIR_Body',(0,0,0));hand=join(weapon,'ZINCIR_HandChain',(.755,-.46,.965))
# Match the approved three-quarter silhouette: wider shoulders and low, bowed mass.
for ob in [body,hand]:
 mw=ob.matrix_world.copy()
 for v in ob.data.vertices:
  w=mw@v.co;w.x*=1.08;w.z*=.84
  v.co=mw.inverted()@w

root=bpy.data.objects.new('ZINCIR_Root',None);bpy.context.collection.objects.link(root)
for o in [body,hand]:o.parent=root
# Triangulate selected large surfaces for reliable identical FBX rendering, no visual microdetail.
for o in [body,hand]:
 while o.data.uv_layers:o.data.uv_layers.remove(o.data.uv_layers[0])
root['design']='Approved simplified concept: bowed hood, small sorrowful skull, broad grounded mantle. Static revision, no rig.'
# Freeze body triangulation so cloth and socket shading survive import unchanged.
bm=bmesh.new();bm.from_mesh(body.data);bmesh.ops.triangulate(bm,faces=list(bm.faces),quad_method='BEAUTY',ngon_method='BEAUTY');bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=0.0001);bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=0.0001);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(body.data);bm.free()
# Character export
bpy.ops.object.select_all(action='DESELECT')
for o in [root,body,hand]:o.select_set(True)
opts=dict(use_selection=True,object_types={'EMPTY','MESH'},use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_NONE')
bpy.ops.export_scene.fbx(filepath=str(P/'ZINCIR_spirit.fbx'),**opts);body.select_set(False);bpy.ops.export_scene.fbx(filepath=str(P/'ZINCIR_spirit_weapon.fbx'),**opts)
report={o.name:{'faces':len(o.data.polygons),'triangles':sum(len(f.vertices)-2 for f in o.data.polygons),'materials':sorted({o.data.materials[f.material_index].name for f in o.data.polygons})} for o in [body,hand]};(P/'mesh_report.json').write_text(json.dumps(report,indent=2))
# Warm neutral presentation closely matching the approved concept's framing.
scene=bpy.context.scene
bpy.ops.object.camera_add(location=(3.75,-7.0,2.7));cam=bpy.context.object;cam.rotation_euler=(Vector((.85,0,.85))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=4.15;scene.camera=cam
for loc,power,size in [((-3,-4,6),480,4),((4,-2,3),110,4),((1,3,5),350,4)]:
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=power;o.data.size=size;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.01));floor=bpy.context.object;floor.name='Studio ground';floor.data.materials.append(mat('studio warm grey',(.42,.40,.36)))
scene.world.use_nodes=True;scene.world.node_tree.nodes.get('Background').inputs[0].default_value=(.50,.47,.43,1);scene.world.node_tree.nodes.get('Background').inputs[1].default_value=.45;scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True;scene.render.resolution_x=1550;scene.render.resolution_y=1150;scene.render.resolution_percentage=100;scene.view_settings.view_transform='AgX';scene.render.filepath=str(P/'ZINCIR_preview.png')
asset=bpy.data.collections.new('ZINCIR - Concept rebuild');scene.collection.children.link(asset);studio=bpy.data.collections.new('STUDIO - Preview only');scene.collection.children.link(studio)
for ob in list(scene.objects):
 dest=asset if ob in [root,body,hand] else studio
 for c in list(ob.users_collection):c.objects.unlink(ob)
 dest.objects.link(ob)
 if dest==studio:ob.hide_set(True)
for screen in bpy.data.screens:
 for ar in screen.areas:
  if ar.type=='VIEW_3D':
   sp=ar.spaces.active;sp.shading.type='SOLID';sp.shading.color_type='MATERIAL';sp.overlay.show_floor=False;sp.overlay.show_axis_x=False;sp.overlay.show_axis_y=False;sp.region_3d.view_rotation=cam.rotation_euler.to_quaternion();sp.region_3d.view_distance=4.8;sp.region_3d.view_location=(.6,0,1);sp.region_3d.view_perspective='ORTHO'
bpy.ops.object.select_all(action='DESELECT');body.select_set(True);bpy.context.view_layer.objects.active=body
bpy.ops.wm.save_as_mainfile(filepath=str(P/'ZINCIR.blend'));bpy.ops.render.render(write_still=True);print(json.dumps(report))
