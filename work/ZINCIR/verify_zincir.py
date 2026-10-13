import bpy,json
from pathlib import Path
from io_scene_fbx import parse_fbx
P=Path(__file__).parent
def settings(p):
 root,_=parse_fbx.parse(str(p));keys=['UpAxis','UpAxisSign','FrontAxis','FrontAxisSign','CoordAxis','CoordAxisSign','UnitScaleFactor','OriginalUnitScaleFactor']
 return {c.props[0].decode():c.props[-1] for e in root.elems if e.id==b'GlobalSettings' for q in e.elems if q.id==b'Properties70' for c in q.elems if c.props[0].decode() in keys}
ref=settings(P.parent/'SILIK/SILIK_spirit.fbx');out={}
for name,names in [('ZINCIR_spirit.fbx',{'ZINCIR_Root','ZINCIR_Body','ZINCIR_HandChain'}),('ZINCIR_spirit_weapon.fbx',{'ZINCIR_Root','ZINCIR_HandChain'})]:
 for ob in list(bpy.data.objects):bpy.data.objects.remove(ob,do_unlink=True)
 bpy.ops.import_scene.fbx(filepath=str(P/name));obs=list(bpy.context.scene.objects)
 assert {o.name for o in obs}==names
 assert settings(P/name)==ref
 meshes={}
 for ob in obs:
  assert ob.type in {'MESH','EMPTY'} and ob.animation_data is None
  if ob.type!='MESH':continue
  assert ob.parent.name=='ZINCIR_Root' and ob.data.shape_keys is None
  assert all(p.area>1e-10 for p in ob.data.polygons)
  assert not any(n.type=='TEX_IMAGE' for m in ob.data.materials for n in m.node_tree.nodes)
  assert all(m.name.startswith('ZINCIR | ') for m in ob.data.materials)
  meshes[ob.name]={'faces':len(ob.data.polygons),'triangles':sum(len(p.vertices)-2 for p in ob.data.polygons)}
 out[name]={'verified':True,'meshes':meshes,'rig':False,'reference_axis_and_unit_match':True}
(P/'validation.json').write_text(json.dumps(out,indent=2));print(json.dumps(out,indent=2))
