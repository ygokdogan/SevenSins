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
cloth=mat('slate mourning cloth',(.077,.085,.104));light=mat('ash cloth planes',(.093,.100,.116));shade=mat('heavy fold shadow',(.057,.064,.08));lining=mat('unlit hood interior',(.018,.022,.029));bone=mat('old ivory bone',(.45,.423,.355));bonehi=mat('worn bone planes',(.50,.475,.41));bonesh=mat('bone recess',(.285,.269,.228));black=mat('empty eye sockets',(.012,.013,.014));iron=mat('blackened iron',(.058,.055,.048),.55);wear=mat('worn iron bevels',(.115,.11,.098),.6);rust=mat('quiet umber rust',(.135,.081,.046),.2)
body=[];weapon=[]
def mesh(name,vs,fs,mats,ids=None):
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update();ob=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(ob)
 for m in mats:me.materials.append(m)
 for p in me.polygons:p.material_index=ids[p.index%len(ids)] if ids else 0;p.use_smooth=False
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free();return ob
# Hand drawn cross-sections: the brow projects forward; the crown folds back into a hump.
# clockwise around front opening starting at top center
opening=[(0,1.64),(.26,1.59),(.43,1.47),(.515,1.28),(.48,1.07),(.37,.89),(.19,.80),(0,.77),(-.20,.80),(-.39,.91),(-.49,1.10),(-.515,1.31),(-.43,1.49),(-.26,1.60)]
outer=[(.035,2.08),(.33,2.045),(.59,1.85),(.72,1.53),(.68,1.20),(.51,.96),(.25,.87),(0,.84),(-.27,.86),(-.54,1.00),(-.68,1.23),(-.72,1.56),(-.58,1.85),(-.29,2.055)]
n=len(opening);vs=[]
for i,(x,z) in enumerate(opening):vs.append((x,-.725+(.11 if z<1 else .02 if abs(x)>.45 else 0),z))
for i,(x,z) in enumerate(outer):vs.append((x,-.24 if z>1.9 else -.16 if abs(x)>.60 else -.32,z))
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
for x,y,z in front:lip.append((x*.93,y+.19,1.21+(z-1.21)*.95))
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
  if row==0:r=radii[i];z=.025+(.025 if i%4==0 else 0)
  elif row==1:r=.74+(.055 if i%3==0 else -.015);z=.40+(.045*math.sin(2*a))
  elif row==2:r=.63+(.07 if i%3==0 else 0);z=.88+.03*math.cos(a)
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
# A smaller purpose-built skull with sad sloping eye sockets. No smile or separate jaw.
# Skull face is a constrained triangulated surface with real eye/nose holes.
outline=[(0,1.57),(.15,1.55),(.255,1.49),(.31,1.39),(.335,1.27),(.315,1.13),(.255,1.065),(.20,1.035),(.18,.96),(.14,.948),(.115,.985),(.09,.945),(.045,.94),(.032,.985),(-.008,.942),(-.05,.95),(-.065,.99),(-.11,.957),(-.147,.968),(-.16,1.022),(-.22,1.05),(-.28,1.12),(-.323,1.25),(-.305,1.39),(-.245,1.50),(-.13,1.56)]
# Each upper edge slopes downward toward the outside, giving a resigned expression.
eyeR=[(.065,1.335),(.12,1.315),(.19,1.292),(.26,1.285),(.282,1.249),(.268,1.18),(.225,1.144),(.16,1.139),(.102,1.17),(.074,1.235)]
eyeL=[(-x,z+.008) for x,z in reversed(eyeR)]
nose=[(-.029,1.179),(.001,1.206),(.035,1.179),(.052,1.107),(.007,1.093),(-.044,1.112)]
loops=[outline,eyeR,eyeL,nose];coords=[];edges=[];loopids=[]
for loop in loops:
 ix=list(range(len(coords),len(coords)+len(loop)));loopids.append(ix);coords.extend(loop);edges.extend((ix[j],ix[(j+1)%len(ix)]) for j in range(len(ix)))
def inside(p,poly):
 c=False;x,y=p
 for i in range(len(poly)):
  x1,y1=poly[i];x2,y2=poly[(i+1)%len(poly)]
  if (y1>y)!=(y2>y) and x<(x2-x1)*(y-y1)/(y2-y1)+x1:c=not c
 return c
# Extra bone landmarks produce meaningful sculpted forehead planes.
coords.extend([(-.16,1.445),(0,1.49),(.16,1.44),(0,1.36),(0,1.265),(-.30,1.17),(.29,1.16),(-.07,1.07),(.075,1.064)])
v2,e2,f2,*_=delaunay_2d_cdt([Vector(v) for v in coords],edges,[],0,1e-6)
def depth(x,z):
 # cheeks and forehead project; sockets recess behind their rims
 return -.665+.15*(abs(x)/.34)**2+(.035 if z>1.5 else 0)-(.015 if 1.36<z<1.5 else 0)
vs=[(v.x,depth(v.x,v.y),v.y) for v in v2];faces=[];ids=[]
for f in f2:
 c=sum((v2[i] for i in f),Vector((0,0)))/len(f)
 if inside(c,outline) and not any(inside(c,l) for l in loops[1:]):faces.append(f);ids.append(1 if c.y>1.39 and c.x<0 else 0)
skull=mesh('Small sorrowful skull face',vs,faces,[bone,bonehi],ids);body.append(skull)
# Socket inner bevels and charcoal recess floors.
for k,loop in enumerate([eyeR,eyeL,nose]):
 center=sum((Vector(p) for p in loop),Vector((0,0)))/len(loop);vs=[];fs=[];n=len(loop)
 for r in range(3):
  for x,z in loop:
   xx=center.x+(x-center.x)*(1 if r==0 else .83 if r==1 else .68);zz=center.y+(z-center.y)*(1 if r==0 else .85 if r==1 else .68)
   vs.append((xx,depth(x,z)+[0,.027,.10][r],zz))
 for r in range(2):
  for j in range(n):fs.append((r*n+j,r*n+(j+1)%n,(r+1)*n+(j+1)%n,(r+1)*n+j))
 fs.append(tuple(2*n+i for i in range(n)));ids=[0]*n+[1]*n+[1]
 body.append(mesh('Socket bevel %d'%k,vs,fs,[bonesh,black],ids))
# Cranium side shell, opaque at all angles.
vs=[(x,depth(x,z),z) for x,z in outline]+[(x*.91,-.38,1.28+(z-1.28)*.93) for x,z in outline];n=len(outline)
fs=[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]+[tuple(n+i for i in range(n))]
body.append(mesh('Cranium sides',vs,fs,[bone]))
# Tilt the entire skull down into the hood.
for ob in body:
 if any(k in ob.name for k in ['skull','Socket','Cranium']):
  pivot=Vector((0,-.49,1.29));rot=Matrix.Rotation(math.radians(13),3,'X')
  for v in ob.data.vertices:v.co=pivot+rot@(v.co-pivot)
# Simple mitten-like ivory fist: bevelled mass + broad thumb, not round beads.
def bevelbox(name,loc,scale,material,bev=.06):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(material)
 mod=o.modifiers.new('Broad planar bevel','BEVEL');mod.width=bev;mod.segments=1;bpy.ops.object.modifier_apply(modifier=mod.name);return o
hand=bevelbox('Simple clenched hand',(.755,-.46,.965),(.205,.17,.24),bone,.055);hand.rotation_euler=(.1,.15,-.15);weapon.append(hand)
weapon.append(bevelbox('Single thumb',(.685,-.515,.957),(.075,.075,.115),bone,.035))
# Chunky rectangular-section oval links. Chamfered ring corners read at FPS distance.
def link(name,c,d,twist=0):
 d=Vector(d).normalized();u=d.cross(Vector((0,1,0))).normalized();v=d.cross(u);u,v=u*math.cos(twist)+v*math.sin(twist),-u*math.sin(twist)+v*math.cos(twist)
 # 12 points around capsule, with thick bevelled rectangular section
 path=[];norms=[]
 for i in range(12):
  a=2*math.pi*i/12;path.append((.09*math.cos(a),.152*math.sin(a)));norms.append(Vector((math.cos(a),math.sin(a))).normalized())
 section=[(-.025,-.022),(-.017,-.032),(.018,-.032),(.032,-.018),(.032,.018),(.018,.032),(-.017,.032),(-.025,.022)]
 vs=[];fs=[];ids=[]
 for i,(xx,zz) in enumerate(path):
  normal=norms[i]
  for off,dep in section:vs.append(Vector(c)+u*(xx+normal.x*off*1.3)+d*(zz+normal.y*off*1.3)+v*dep*1.3)
 for i in range(12):
  for j in range(8):fs.append((i*8+j,((i+1)%12)*8+j,((i+1)%12)*8+(j+1)%8,i*8+(j+1)%8));ids.append(1 if j in [1,5] else 2 if (i*8+j)%37==0 else 0)
 return mesh(name,vs,fs,[iron,wear,rust],ids)
points=[(.82,-.46,.78),(.92,-.44,.55),(1.065,-.415,.345),(1.285,-.39,.205),(1.53,-.365,.155),(1.78,-.34,.17)]
for i,p in enumerate(points):
 d=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)])
 weapon.append(link('Heavy link %02d'%i,p,d,(i%2)*math.pi/2+.20))
weight=bevelbox('Blunt prison weight',(2.13,-.31,.295),(.59,.49,.59),iron,.105);weight.rotation_euler.z=.15
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
root=bpy.data.objects.new('ZINCIR_Root',None);bpy.context.collection.objects.link(root)
for o in [body,hand]:o.parent=root
# Triangulate selected large surfaces for reliable identical FBX rendering, no visual microdetail.
for o in [body,hand]:
 while o.data.uv_layers:o.data.uv_layers.remove(o.data.uv_layers[0])
root['design']='Approved simplified concept: bowed hood, small sorrowful skull, broad grounded mantle. Static revision, no rig.'
# Freeze body triangulation so cloth and socket shading survive import unchanged.
bm=bmesh.new();bm.from_mesh(body.data);bmesh.ops.triangulate(bm,faces=list(bm.faces),quad_method='BEAUTY',ngon_method='BEAUTY');bm.to_mesh(body.data);bm.free()
# Character export
bpy.ops.object.select_all(action='DESELECT')
for o in [root,body,hand]:o.select_set(True)
opts=dict(use_selection=True,object_types={'EMPTY','MESH'},use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_NONE')
bpy.ops.export_scene.fbx(filepath=str(P/'ZINCIR_spirit.fbx'),**opts);body.select_set(False);bpy.ops.export_scene.fbx(filepath=str(P/'ZINCIR_spirit_weapon.fbx'),**opts)
report={o.name:{'faces':len(o.data.polygons),'triangles':sum(len(f.vertices)-2 for f in o.data.polygons),'materials':sorted({o.data.materials[f.material_index].name for f in o.data.polygons})} for o in [body,hand]};(P/'mesh_report.json').write_text(json.dumps(report,indent=2))
# Warm neutral presentation closely matching the approved concept's framing.
scene=bpy.context.scene
bpy.ops.object.camera_add(location=(3.1,-6.5,3.0));cam=bpy.context.object;cam.rotation_euler=(Vector((.63,0,1.0))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=3.9;scene.camera=cam
for loc,power,size in [((-3,-4,6),480,4),((4,-2,3),110,4),((1,3,5),350,4)]:
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=power;o.data.size=size;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.01));floor=bpy.context.object;floor.name='Studio ground';floor.data.materials.append(mat('studio warm grey',(.29,.275,.25)))
scene.world.color=(.19,.19,.19);scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True;scene.render.resolution_x=1450;scene.render.resolution_y=1200;scene.render.resolution_percentage=100;scene.view_settings.view_transform='AgX';scene.render.filepath=str(P/'ZINCIR_preview.png')
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
