# Attack indicators

All indicator scripts, prefabs, sprites and editor tools live in this folder.

| AttackData Defense | Cue | Held block | Timed parry |
| --- | --- | --- | --- |
| Basic (default) | Gold diamond / ! | Stops | Stops |
| ParryOnly | Cyan diamond / sword and sparks | Bypasses | Stops |
| DodgeOnly | Red octagon / X | Bypasses | Bypasses |

Evading the attack's hit volume works for all three types. These rules do not introduce dodge invulnerability.

Select **Defense** on any melee or ranged AttackData asset. Old attacks remain Basic.
ShieldBearerAttackStyle exposes Bash Defense (default ParryOnly) and Sword Defense (default Basic).
The existing Archer Arc Attack uses DodgeOnly; straight arrows retain Basic.

EnemyAttackIndicator shows a cue above the collider, faces the player's aim camera,
fills its countdown ring during windup, and flashes white at the strike. It hides when
an attack exits or is interrupted by hit, parry, grapple, disable or death. Views are
created once per enemy and reused. Every sword combo hit gets its own cue.

For another attack state:

    enemy.AttackIndicator.Show(defense, windupDuration);
    enemy.AttackIndicator.UpdateRemaining(windupRemaining);
    enemy.AttackIndicator.Strike();
    // In Exit:
    enemy.AttackIndicator.Hide();

Copy the same defense into HitInfo.defense so cue and combat resolution match.

Unity menu: Tools > Western Samurai > Attack Indicators.
Use Build Indicator Assets to regenerate editable prefabs/sprites, and Validate
Indicators and Defense Rules to check categories and assets. The Shield Bearer
Play Mode validation also checks the actual bash and combo hit categories.
