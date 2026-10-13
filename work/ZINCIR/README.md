# Zincir Sürükleyen — onaylanan sade konsepte göre yeniden modelleme

`approved_concept.png` tasarım referansıdır. `ZINCIR_preview.png` gerçek Blender modelinin renderıdır.

- `ZINCIR.blend`: düzenlenebilir güncel model.
- `ZINCIR_spirit.fbx`: boş ZINCIR_Root altında ZINCIR_Body ve ZINCIR_HandChain.
- `ZINCIR_spirit_weapon.fbx`: aynı root ve ayrı el + zincir mesh'i.

Bu revizyon rigsizdir. Önceki rigli model ve açık oturumun yedeği `archive_rigged` klasöründe korunmuştur. Kol, bacak, doku, animasyon ve shape key yoktur. El/silah pivotu kavrama noktasındadır.

## Model
- Gövde: 6.360 üçgen yüz. Kapüşon, küçük yorgun kafatası, geniş kumaş gövde tek mesh.
- El + zincir: 874 yüz / 1.636 üçgen. Sade yumruk, 7 kalın halka, köşeli demir ağırlık tek ayrı mesh.
- Kapüşon, yüz ve kumaş geometrisi baştan oluşturuldu. Geniş kumaş yüzeyleri, aşağı düşen göz yuvaları, kapüşon içindeki küçük yüz ve yere oturan etek esas alındı.

## Düz renkli materyaller
Hepsi `ZINCIR | ` önekini taşır:
- slate mourning cloth, ash cloth planes, heavy fold shadow, unlit hood interior
- old ivory bone, worn bone planes, bone recess, empty eye sockets
- blackened iron, worn iron bevels, quiet umber rust

## Export ve kontrol
Blender FBX 7.4; -Z forward, Y up, global_scale=1, apply_unit_scale=True, FBX_SCALE_NONE. Referans SILIK_spirit.fbx ile birim/eksen metadatası karşılaştırılmıştır. İki FBX yeniden içe aktarılarak mesh adları, parent düzeni, yüz sayıları, doku/rig/animasyon yokluğu doğrulanmıştır. Unity içinde oyun testi yapılmamıştır.

Yeniden üretim: Blender ile `rebuild_concept.py` veya `build_zincir.py`. Export kontrolü: `verify_zincir.py`.

## Son revizyon
Poligon bütçesi kaldırıldı. Kafatası hacimlerden şekillendirilerek göz ve burun oyukları açıldı; kapüşon/yüz birlikte öne eğildi, siluet genişletilip basıklaştırıldı. Karşılaştırma: `comparison.html`. Konseptin birebir kopyası olduğu iddia edilmez; görüntüdeki yorum farkları gerçek render üzerinden değerlendirilmelidir.
