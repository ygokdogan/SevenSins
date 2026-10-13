import bpy, math, bmesh, json
from mathutils import Vector, Matrix
from pathlib import Path
P=Path(__file__).parent
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(P.parent/'SILIK/SILIK_spirit.fbx'))
bpy.context.view_layer.update()
parts=[]
keep=['SILIK_Skull','SILIK_Jaw','SILIK_Hood']
for o in list(bpy.context.scene.objects):
 if o.type=='MESH' and any(o.name.startswith(s) for s in keep):
  o.shape_key_clear()
  mw=o.matrix_world.copy();o.parent=None
  for v in o.data.vertices:
   v.co=mw@v.co; x,y,z=v.co
   v.co=(x*1.18,y*1.12-.06,max(.04,z*.80+.28))
  o.matrix_world=Matrix.Identity(4);parts.append(o)
 else:bpy.data.objects.remove(o,do_unlink=True)
def mat(name,c,metal=0):
 m=bpy.data.materials.new('ZINCIR | '+name);m.diffuse_color=(*c,1);m.use_nodes=True
 b=m.node_tree.nodes.get('Principled BSDF');b.inputs['Base Color'].default_value=(*c,1);b.inputs['Roughness'].default_value=.86;b.inputs['Metallic'].default_value=metal
 return m
cloth=mat('burdened slate cloth',(.065,.080,.103));fold=mat('ash worn folds',(.078,.094,.116));dark=mat('deep cowl shadow',(.032,.045,.052));bone=mat('cold ash bone',(.36,.385,.38));socket=mat('unlit sockets',(.016,.022,.025));iron=mat('forged dark iron',(.065,.086,.096),.65);edge=mat('scuffed iron edges',(.23,.265,.27),.6);rust=mat('old umber rust',(.19,.105,.065),.25)
for o in parts:
 for i,m in enumerate(o.data.materials):
  n=m.name
  o.data.materials[i]=bone if 'chalk' in n else socket if 'charcoal' in n else dark if 'inner' in n else fold if 'light' in n else cloth
 if 'Skull' in o.name or 'Jaw' in o.name:
  center=Vector((0,0,1.17)); rot=Matrix.Rotation(math.radians(12),3,'X')
  for v in o.data.vertices:
   if 'Jaw' in o.name: v.co.z+=.052
   v.co=center+rot@(v.co-center)
 # Lower the upper opening to make the face look downcast.
 if 'Hood' in o.name:
  for v in o.data.vertices:
   if v.co.y<-.45 and v.co.z>1.35:v.co.z-=.085

def mesh(name,vs,fs,mats,ids=None):
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o)
 for m in mats:me.materials.append(m)
 for f in me.polygons:f.material_index=ids[f.index%len(ids)] if ids else 0
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free();return o
# Dense closed robe: broad base, shallow weighted folds, no floating tails.
vs=[];fs=[];ids=[];N=24;R=12
for j in range(R):
 t=j/(R-1);z=.035+t*.94
 radius=.80*(1-t)+.39*t
 for i in range(N):
  a=2*math.pi*i/N;rr=radius*(1+.045*math.cos(a*6))
  vs.append((rr*math.cos(a),rr*.70*math.sin(a)+.10,z+.018*math.sin(a*5)*(1-t)))
for j in range(R-1):
 for i in range(N):
  a=j*N+i;b=j*N+(i+1)%N;c=b+N;d=a+N
  fs.extend([(a,b,c),(a,c,d)]);ids.extend([1 if i in (4,5,16,17) else 0]*2)
fs.extend([tuple(reversed(range(N))),tuple((R-1)*N+i for i in range(N))]);ids.extend([0,0]);parts.append(mesh('Weighted closed mantle',vs,fs,[cloth,fold],ids))
# A broad slanted iron binding sunk into the lower cowl/upper mantle.
vs=[];fs=[]
for zoff in [-.045,.045]:
 for i in range(24):
  a=2*math.pi*i/24;vs.append((.60*math.cos(a),.41*math.sin(a)+.025,.68+.12*math.cos(a)+zoff))
for i in range(24):fs.append((i,(i+1)%24,(i+1)%24+24,i+24))
o=mesh('Burden binding',vs,fs,[iron,rust],[0,0,0,0,1]);mod=o.modifiers.new('Solid iron','SOLIDIFY');mod.thickness=.025
bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.modifier_apply(modifier=mod.name);o.select_set(False);bpy.data.objects.remove(o,do_unlink=True)
# Closed mouth is partly veiled by a heavy diagonal fold.
unused=mesh('Overlapping chest fold',[(-.47,-.40,.82),(.38,-.46,.64),(.42,-.42,.49),(-.50,-.40,.66),(-.06,-.51,.64)],[(0,1,4),(1,2,4),(2,3,4),(3,0,4)],[cloth,fold],[0,1,0,0]);bpy.data.objects.remove(unused,do_unlink=True)
def link(name,c,d,rx=.085,rz=.125,tube=.022,twist=0):
 d=Vector(d).normalized();u=d.cross(Vector((0,1,0))).normalized();v=d.cross(u);u,v=u*math.cos(twist)+v*math.sin(twist),-u*math.sin(twist)+v*math.cos(twist)
 vs=[];fs=[]
 for i in range(12):
  a=2*math.pi*i/12
  for j in range(4):
   b=2*math.pi*j/4;vs.append(Vector(c)+u*((rx+tube*math.cos(b))*math.cos(a))+d*((rz+tube*math.cos(b))*math.sin(a))+v*tube*math.sin(b))
 for i in range(12):
  for j in range(4):fs.append((i*4+j,((i+1)%12)*4+j,((i+1)%12)*4+(j+1)%4,i*4+(j+1)%4))
 return mesh(name,vs,fs,[iron,edge,rust],[0,1,0,0,0,0,2,0,0,0])
# Thick attachment rings imply an old burden; weapon remains independently movable.
# No extra body hardware
weapon=[]
# Floating bony fist, no arm, matching Silik's separate hand convention.
def ico(name,loc,scale,material,sub=1):
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc);o=bpy.context.object;o.name=name;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(material);return o
weapon.append(ico('Clenched ash palm',(-.86,-.35,.64),(.13,.10,.125),bone,2))
# Grouped fingers are part of the simple palm mass
weapon.append(ico('Thumb',(-.77,-.42,.64),(.044,.04,.061),bone))
# Eleven genuine interlocked oval links follow a slack curve toward the ground.
points=[(-.91,-.37,.53),(-.98,-.36,.37),(-1.095,-.34,.245),(-1.255,-.32,.18),(-1.435,-.30,.155),(-1.61,-.28,.15),(-1.79,-.26,.15),(-1.97,-.24,.15),(-2.14,-.22,.16)]
for i,p in enumerate(points):
 d=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)])
 weapon.append(link('Chain %02d'%i,p,d,twist=(i%2)*math.pi/2))
# Blunt faceted prison weight, intentionally larger than the fist.
weapon.append(ico('Dragged iron weight',(-2.43,-.20,.275),(.34,.27,.275),iron,2))
weapon.append(link('Weight collar',(-2.19,-.21,.20),(1,0,0),.083,.115,.027,0))
# Broad scuffed band around weight.
vs=[];fs=[]
for x in [-2.49,-2.39]:
 for i in range(12):
  a=2*math.pi*i/12;vs.append((x,-.20+.268*math.cos(a),.275+.278*math.sin(a)))
for i in range(12):fs.append((i,(i+1)%12,(i+1)%12+12,i+12))
weapon.append(mesh('Weight wear band',vs,fs,[edge,rust],[0,0,1,0,0]))
for ob in weapon:
 if ob.name.startswith('Chain '): group='Chain_%02d'%(int(ob.name.split()[-1])+1)
 elif ob.name=='Weight collar':group='Chain_10'
 elif ob.name in ['Dragged iron weight','Weight wear band']:group='Weight'
 else:group='Grip'
 vg=ob.vertex_groups.new(name=group);vg.add(list(range(len(ob.data.vertices))),1.0,'REPLACE')
def join(obs,name,pivot):
 bpy.ops.object.select_all(action='DESELECT')
 for o in obs:o.select_set(True)
 bpy.context.view_layer.objects.active=obs[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
 bpy.context.scene.cursor.location=pivot;bpy.ops.object.origin_set(type='ORIGIN_CURSOR');return o
body=join(parts,'ZINCIR_Body',(0,0,0));hand=join(weapon,'ZINCIR_HandChain',(-.86,-.35,.64))
root=bpy.data.objects.new('ZINCIR_Root',None);bpy.context.collection.objects.link(root)
for o in [body,hand]:o.parent=root
root['design']='Seven Sins / Araf / Zincir Surukleyen. Grounded burden, unlit soul.'
hand['pivot']='Grip bone controls whole weapon. Chain_01..10 and Weight use rigid single-bone weights.'
# Exportable FK rig, separate from the static body.
arm=bpy.data.armatures.new('ZINCIR_ChainRig');rig=bpy.data.objects.new('ZINCIR_ChainRig',arm);bpy.context.collection.objects.link(rig);rig.parent=root;rig.show_in_front=True;arm.display_type='OCTAHEDRAL'
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
grip=arm.edit_bones.new('Grip');grip.head=(-.86,-.35,.64);grip.tail=(-.86,-.35,.82)
centers=[Vector(p) for p in points]+[Vector((-2.19,-.21,.20))]
# Joints sit at link contacts, preserving rigid metal links during rotation.
joints=[Vector((-.88,-.36,.63))]+[(centers[i-1]+centers[i])*.5 for i in range(1,len(centers))]+[Vector((-2.29,-.20,.235))]
parent=grip
for i in range(10):
 b=arm.edit_bones.new('Chain_%02d'%(i+1));b.head=joints[i];b.tail=joints[i+1];b.parent=parent;b.use_connect=i>0;parent=b
b=arm.edit_bones.new('Weight');b.head=joints[-1];b.tail=(-2.57,-.20,.275);b.parent=parent;b.use_connect=True
bpy.ops.object.mode_set(mode='OBJECT')
for pb in rig.pose.bones:
 pb.rotation_mode='XYZ'
 if pb.name!='Grip':pb.lock_location=(True,True,True)
 pb.lock_scale=(True,True,True)
 pb['usage']='Rotate FK; local Y twists link, X/Z bends. Alt+R restores rest pose.'
mod=hand.modifiers.new('Rigid chain skin','ARMATURE');mod.object=rig;mod.use_vertex_groups=True
rig['controls']='Grip > Chain_01 ... Chain_10 > Weight; FK, no simulation, no baked animation.'
rig['rest']='Ground dragging pose. Rotate Grip for swing; rotate links for follow-through.'

for o in [body,hand]:
 for p in o.data.polygons:p.use_smooth=False
 while o.data.uv_layers: o.data.uv_layers.remove(o.data.uv_layers[0])
export=dict(use_selection=True,object_types={'EMPTY','MESH','ARMATURE'},use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_NONE',use_custom_props=True)
bpy.ops.object.select_all(action='DESELECT')
for o in [root,body,hand,rig]:o.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(P/'ZINCIR_spirit.fbx'),**export)
body.select_set(False);bpy.ops.export_scene.fbx(filepath=str(P/'ZINCIR_spirit_weapon.fbx'),**export)
report={o.name:{'faces':len(o.data.polygons),'triangles':sum(len(p.vertices)-2 for p in o.data.polygons),'vertices':len(o.data.vertices),'materials':sorted({o.data.materials[p.material_index].name for p in o.data.polygons})} for o in [body,hand]}
(P/'mesh_report.json').write_text(json.dumps(report,indent=2))
# Studio is excluded from both FBXs.
scene=bpy.context.scene
bpy.ops.object.camera_add(location=(3,-7,3.1));cam=bpy.context.object;cam.rotation_euler=(Vector((-.72,0,.83))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=4.3;scene.camera=cam
for loc,power,size in [((-3,-4,6),650,4),((4,-2,3),350,4),((0,3,4),700,3)]:
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=power;o.data.shape='DISK';o.data.size=size;o.rotation_euler=(Vector((-.5,0,.7))-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.012));bpy.context.object.name='Studio floor';bpy.context.object.data.materials.append(mat('preview floor',(.135,.15,.16)))
scene.world.color=(.18,.18,.18);scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True;scene.render.resolution_x=1500;scene.render.resolution_y=1100;scene.render.resolution_percentage=100;scene.view_settings.view_transform='AgX';scene.render.image_settings.file_format='PNG';scene.render.filepath=str(P/'ZINCIR_preview.png')
# Organize the file and present a useful material-colored viewport.
asset=bpy.data.collections.new('ZINCIR - Character and FK rig');scene.collection.children.link(asset)
studio=bpy.data.collections.new('STUDIO - Preview only');scene.collection.children.link(studio)
for ob in list(scene.objects):
 dest=asset if ob in [root,body,hand,rig] else studio
 for c in list(ob.users_collection):c.objects.unlink(ob)
 dest.objects.link(ob)
 if dest==studio:ob.hide_set(True)
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   sp=area.spaces.active;sp.shading.type='SOLID';sp.shading.color_type='MATERIAL';sp.overlay.show_floor=False
   sp.region_3d.view_rotation=cam.rotation_euler.to_quaternion();sp.region_3d.view_distance=5;sp.region_3d.view_location=(-.75,0,.8);sp.region_3d.view_perspective='ORTHO'
bpy.ops.object.select_all(action='DESELECT');body.select_set(True);bpy.context.view_layer.objects.active=body
bpy.ops.wm.save_as_mainfile(filepath=str(P/'ZINCIR.blend'));bpy.ops.render.render(write_still=True)
print('REPORT',json.dumps(report))
