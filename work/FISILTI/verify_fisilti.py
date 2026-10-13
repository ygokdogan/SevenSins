import bpy,json
from pathlib import Path
p=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(p/'FISILTI.blend'))
meshes=[o for o in bpy.data.objects if o.type=='MESH' and o.parent and o.parent.name=='FISILTI']
assert len(meshes)==7,len(meshes)
for o in meshes:
 assert len(o.data.vertices)>0
 assert all(v.co.length<100 for v in o.data.vertices)
 assert not o.data.validate(),o.name
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(p/'FISILTI.fbx'))
meshes=[o for o in bpy.data.objects if o.type=='MESH']
assert len(meshes)==7
assert all(o.data.materials for o in meshes)
print('PASS: saved blend meshes valid; FBX reimport has all seven meshes and materials.')
print((p/'model_info.json').read_text())
