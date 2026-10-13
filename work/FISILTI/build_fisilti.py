import bpy, math, random, json
from pathlib import Path
from mathutils import Vector
OUT=Path(__file__).resolve().parent
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
for d in list(bpy.data.materials): bpy.data.materials.remove(d)
def mat(name,c,emission=0):
 m=bpy.data.materials.new(name); m.diffuse_color=(*([c]*3),1); m.use_nodes=True
 p=m.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value=(*([c]*3),1); p.inputs['Roughness'].default_value=.9
 if emission: p.inputs['Emission Color'].default_value=(c,c,c,1); p.inputs['Emission Strength'].default_value=emission
 return m
gray=mat('Ash gray',.29); dark=mat('Hood interior charcoal',.018); face=mat('Pale forgotten face',.72); hem=mat('Hem shadow',.12); orbmat=mat('Orb white',.85,1.7)
def mesh(name,vs,fs,mats):
 me=bpy.data.meshes.new(name); me.from_pydata(vs,[],fs); me.update(); ob=bpy.data.objects.new(name,me); bpy.context.collection.objects.link(ob)
 for m in mats: me.materials.append(m)
 # consistently recalculate normals
 bpy.context.view_layer.objects.active=ob; ob.select_set(True); bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.mesh.normals_make_consistent(inside=False); bpy.ops.object.mode_set(mode='OBJECT'); ob.select_set(False)
 return ob
# Hand-shaped hood profile: low opening, wide forehead and faceted rear crown.
# Clockwise profiles start at top centre, front is -Y.
outer=[(0,.00,2.64),(.58,.04,2.52),(.90,.02,2.10),(.91,-.08,1.40),(.65,-.20,1.11),(0,-.30,1.07),(-.65,-.20,1.11),(-.91,-.08,1.40),(-.90,.02,2.10),(-.58,.04,2.52)]
opening=[(0,-.56,2.01),(.55,-.52,1.99),(.69,-.50,1.77),(.66,-.49,1.30),(.44,-.52,1.11),(0,-.56,1.09),(-.44,-.52,1.11),(-.66,-.49,1.30),(-.69,-.50,1.77),(-.48,-.64,2.03)]
N=len(outer)
vs=opening+outer
fs=[]
for i in range(N):
 j=(i+1)%N
 if i==9: continue  # custom torn triangular panel below
 if i in (1,2,6,7):fs.extend([(i,j,N+j),(i,N+j,N+i)])
 else:fs.append((i,j,N+j,N+i))
# Tear in upper-left forehead, cut into actual topology, not a painted decoration.
# Panel is divided into a broad forehead triangle and a narrow folded triangle.
fs.append((9,0,10))
tri=[19,10,9]
A,B,C=[Vector(vs[k]) for k in tri]
# A narrow triangular opening follows the fold of the hood.
q=[A*.78+B*.10+C*.12, A*.25+B*.42+C*.33, A*.12+B*.10+C*.78]
# Widen middle point so the tear reads at game scale.
q[1].y-=.012
ids=[]
for v in q:ids.append(len(vs));vs.append(tuple(v))
for k in range(3):fs.append((tri[k],tri[(k+1)%3],ids[(k+1)%3],ids[k]))
# Recessed black triangle is visibly behind the torn fabric.
slitvs=[tuple(v+Vector((0,.075,0))) for v in q]
mesh('Hood_tear_recess',slitvs,[(0,1,2)],[dark])
# Large irregular rear planes create a hood volume, not a torus.
back=[]
for i,(x,y,z) in enumerate(outer):
 back.append((x*.80,.56,1.79+(z-1.79)*.85))
vs+=back
for i in range(N):
 j=(i+1)%N
 # back starts after three inserted tear vertices
 a=N+i;b=N+j;c=23+j;d=23+i
 if i in (1,3,6,8):fs.extend([(a,b,c),(a,c,d)])
 else:fs.append((a,b,c,d))
vs.append((0,.83,1.83));cap=len(vs)-1
for i in range(N):fs.append((23+i,23+(i+1)%N,cap))
hood=mesh('Hood_outer',vs,fs,[gray])
# Thick lip and recessed charcoal hood lining.
vs=list(opening)
for x,y,z in opening:vs.append((x*.94,y+.085,1.55+(z-1.55)*.96))
for x,y,z in opening:vs.append((x*.82,.25,1.55+(z-1.55)*.84))
fs=[]
for j in range(2):
 for i in range(N):fs.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
fs.append(tuple(range(2*N,3*N)))
lining=mesh('Hood_inner',vs,fs,[dark,hem])
for poly in lining.data.polygons:
 if poly.index<N:poly.material_index=1
bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=1,location=(0,-.245,1.565)); ob=bpy.context.object;ob.name='Faceless_head';ob.scale=(.505,.295,.465);ob.rotation_euler[1]=.16;ob.data.materials.append(face);ob.select_set(False)
# An uneven, folded, torn bell shroud. No shoulders, arms, sleeves or neck.
N=16
ends=[.28,.54,.42,.60,.39,.57,.45,.61,.38,.62,.43,.56,.34,.58,.46,.53]
vs=[]
for level in range(6):
 for i in range(N):
  a=2*math.pi*i/N
  if level==0:r=.235;z=1.20
  elif level==1:r=.37;z=.94+( .025 if i%3==0 else 0)
  elif level==2:r=.49;z=.81+( .035 if i%3==0 else 0)
  elif level==3:r=(.60 if i%2==0 else .47)*.97;z=ends[i]+.13
  elif level==4:r=.60 if i%2==0 else .47;z=ends[i]
  else:r=(.60 if i%2==0 else .47)-.045;z=ends[i]+.035
  # Front fold stays broad; back has less depth.
  vs.append((r*math.sin(a),-r*.63*math.cos(a)+.04,z))
fs=[];mi=[]
for j in range(5):
 for i in range(N):
  a=j*N+i;b=j*N+(i+1)%N;c=(j+1)*N+(i+1)%N;d=(j+1)*N+i
  if j in (1,2):fs.extend([(a,b,c),(a,c,d)]);mi.extend([0,0])
  else:fs.append((a,b,c,d));mi.append(1 if j>=3 else 0)
fs.extend([tuple(reversed(range(N))),tuple(range(5*N,6*N))]);mi.extend([0,1])
body=mesh('Armless_shroud',vs,fs,[gray,hem])
for poly,idx in zip(body.data.polygons,mi):poly.material_index=idx
# Single detached block hand: solid palm and small cuboid thumb, one mesh.
def cube(name,loc,scale,material):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(material);o.select_set(False);return o
palm=cube('Floating_square_hand',(1.12,-.39,.79),(.18,.18,.15),gray)
thumb=cube('Thumb',(1.065,-.345,.875),(.07,.075,.045),gray)
palm.select_set(True);thumb.select_set(True);bpy.context.view_layer.objects.active=palm;bpy.ops.object.join();palm.select_set(False)
bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=.205,location=(1.12,-.39,1.19));bpy.context.object.name='Orb';bpy.context.object.data.materials.append(orbmat);bpy.context.object.select_set(False)
model=[o for o in bpy.context.scene.objects if o.type=='MESH']
root=bpy.data.objects.new('FISILTI',None);bpy.context.collection.objects.link(root)
for o in model:o.parent=root
# Stage is separated from exportable character.
stage=bpy.data.collections.new('STUDIO_preview_only');bpy.context.scene.collection.children.link(stage)
def stage_move(o):
 for c in list(o.users_collection):c.objects.unlink(o)
 stage.objects.link(o)
ground=cube('Studio_floor',(0,0,-.045),(200,200,.08),mat('Studio charcoal',.045));stage_move(ground)
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
for name,loc,power,size in [('Key',(-3,-4,6),450,4),('Fill',(3,-4,2.6),240,4),('Rim',(2,3,5),500,3)]:
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.name=name;o.data.energy=power;o.data.shape='DISK';o.data.size=size;aim(o,(0,0,1));stage_move(o)
bpy.ops.object.camera_add(location=(3.0,-8,3.3));cam=bpy.context.object;aim(cam,(.20,0,1.39));cam.data.type='ORTHO';cam.data.ortho_scale=3.3;stage_move(cam);bpy.context.scene.camera=cam
sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=48;sc.cycles.use_denoising=True;sc.render.resolution_x=1000;sc.render.resolution_y=1000;sc.render.resolution_percentage=100;sc.world.color=(.18,.18,.18)
sc.view_settings.view_transform='AgX';sc.render.image_settings.file_format='PNG'
# Export model only, with individually editable parts.
bpy.ops.object.select_all(action='DESELECT')
for o in model:o.select_set(True)
root.select_set(True);bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.fbx(filepath=str(OUT/'FISILTI.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
for area in bpy.context.screen.areas:
 if area.type=='VIEW_3D':
  area.spaces.active.region_3d.view_rotation=cam.rotation_euler.to_quaternion();area.spaces.active.region_3d.view_distance=4.3;area.spaces.active.region_3d.view_location=Vector((.2,0,1.2));area.spaces.active.shading.type='MATERIAL'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'FISILTI.blend'))
sc.render.filepath=str(OUT/'FISILTI_preview.png');bpy.ops.render.render(write_still=True)
cam.location=(0,-8,1.7);aim(cam,(.2,0,1.39));sc.render.filepath=str(OUT/'FISILTI_front.png');bpy.ops.render.render(write_still=True)
stats={'mesh_objects':len(model),'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in model),'vertices':sum(len(o.data.vertices) for o in model),'rigged':False}
(OUT/'model_info.json').write_text(json.dumps(stats,indent=2));print(stats)
