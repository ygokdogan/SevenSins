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
N=12
# Cross-sections around the hood's forward-facing oval opening. Shell wraps behind head.
rings=[(.56,.62,-.48,1.53),(.77,.79,-.32,1.59),(.73,.75,.23,1.61),(.46,.52,.60,1.59),(.07,.10,.73,1.59)]
vs=[]
for rx,rz,y,z in rings:
 for i in range(N):
  a=2*math.pi*i/N; vs.append((rx*math.sin(a),y,z+rz*math.cos(a)))
fs=[]
for j in range(len(rings)-1):
 for i in range(N):
  a=j*N+i;b=j*N+(i+1)%N;c=(j+1)*N+(i+1)%N;d=(j+1)*N+i
  if j in (1,2) and i%3==0: fs.extend([(a,b,c),(a,c,d)])
  else: fs.append((a,b,c,d))
fs.append(tuple(range(4*N,5*N)))
hood=mesh('Hood_outer',vs,fs,[gray])
# Black recessed lining, with closed back behind pale face.
vs=[]
for rx,rz,y,z in [(.56,.62,-.482,1.53),(.52,.58,-.20,1.53),(.43,.49,.32,1.53)]:
 for i in range(N):
  a=2*math.pi*i/N;vs.append((rx*math.sin(a),y,z+rz*math.cos(a)))
fs=[]
for j in range(2):
 for i in range(N):fs.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
fs.append(tuple(range(2*N,3*N)))
mesh('Hood_inner',vs,fs,[dark])
bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=1,location=(0,-.275,1.51)); ob=bpy.context.object;ob.name='Faceless_head';ob.scale=(.435,.29,.49);ob.data.materials.append(face);ob.select_set(False)
# Compact armless bell shroud with alternating torn points and a dark folded underside.
vs=[]
for level in range(4):
 for i in range(N):
  a=2*math.pi*i/N
  if level==0: r=.20;z=1.01
  elif level==1:r=.34;z=.76
  elif level==2:r=.49;z=.36 if i%2==0 else .52
  else:r=.40;z=(.36 if i%2==0 else .52)+.035
  vs.append((r*math.sin(a),r*.72*math.cos(a)+.06,z))
fs=[]
for j in range(3):
 for i in range(N):
  a=j*N+i;b=j*N+(i+1)%N;c=(j+1)*N+(i+1)%N;d=(j+1)*N+i
  if j==1:fs.extend([(a,b,c),(a,c,d)])
  else:fs.append((a,b,c,d))
fs.extend([tuple(reversed(range(N))),tuple(range(3*N,4*N))])
body=mesh('Armless_shroud',vs,fs,[gray,hem])
for p in body.data.polygons:
 if p.index>=36:p.material_index=1
# Single detached block hand: solid palm and small cuboid thumb, one mesh.
def cube(name,loc,scale,material):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(material);o.select_set(False);return o
palm=cube('Floating_square_hand',(1.01,-.30,.92),(.18,.18,.15),gray)
thumb=cube('Thumb',( .955,-.255,1.01),(.07,.075,.055),gray)
palm.select_set(True);thumb.select_set(True);bpy.context.view_layer.objects.active=palm;bpy.ops.object.join();palm.select_set(False)
bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=.205,location=(1.01,-.30,1.30));bpy.context.object.name='Orb';bpy.context.object.data.materials.append(orbmat);bpy.context.object.select_set(False)
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
for name,loc,power,size in [('Key',(-3,-4,6),450,4),('Fill',(4,-2,3),180,3),('Rim',(2,3,5),500,3)]:
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.name=name;o.data.energy=power;o.data.shape='DISK';o.data.size=size;aim(o,(0,0,1));stage_move(o)
bpy.ops.object.camera_add(location=(3.2,-7,3.0));cam=bpy.context.object;aim(cam,(.25,0,1.2));cam.data.type='ORTHO';cam.data.ortho_scale=3.35;stage_move(cam);bpy.context.scene.camera=cam
sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=32;sc.render.resolution_x=1000;sc.render.resolution_y=1000;sc.render.resolution_percentage=100;sc.world.color=(.18,.18,.18)
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
cam.location=(0,-7,1.65);aim(cam,(.2,0,1.2));sc.render.filepath=str(OUT/'FISILTI_front.png');bpy.ops.render.render(write_still=True)
stats={'mesh_objects':len(model),'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in model),'vertices':sum(len(o.data.vertices) for o in model),'rigged':False}
(OUT/'model_info.json').write_text(json.dumps(stats,indent=2));print(stats)
