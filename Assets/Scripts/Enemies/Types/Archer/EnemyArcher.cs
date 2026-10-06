using Enemies.Combat.States;
using Enemies.Core;
using Enemies.States;
using UnityEngine;

namespace Enemies.Combat
{
    [RequireComponent(typeof(ArcherRangedAttackStyle))]
    public class EnemyArcher : Core.EnemyBase
    {
        protected override void ConfigureStates(EnemyCombatStates states)
        {
            states.Approach = new EnemyApproachState();
            states.Engage = new EnemyStalkState();
        }
    }
}
