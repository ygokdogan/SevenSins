using System.Collections.Generic;
using Combat;
using Core;
using Enemies.Combat;
using Enemies.Core;
using Enemies.Movement;
using UnityEngine;
using Utilities;

namespace Enemies.AttackStyles.Samurai
{
    public class SamuraiAttackState : IState<EnemyBase>
    {
        private readonly SamuraiMeleeAttackStyle style;
        private readonly HashSet<IDamageable> hitThisAttack = new HashSet<IDamageable>();
        private readonly MeleeLunge lunge = new();
        
        private bool active;
        private bool startedOutsideAttackBox;
        private bool strikeStarted;
        
        private float telegraphRemaining;
        private float telegraphStopDistance;
        
        public SamuraiAttackState(SamuraiMeleeAttackStyle style) => this.style = style;

        public void Enter(EnemyBase enemy)
        {
            active = true;
            hitThisAttack.Clear();

            enemy.IsAttackWindowOpen = false;
            enemy.IsAnimationFinished = false;
            
            enemy.Movement.Stop();
            enemy.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.None);
            
            telegraphRemaining = style.AttackData.telegraphDuration;
            enemy.AttackIndicator.Show(style.AttackData.defense, telegraphRemaining);
            
            lunge.Reset();
            
            strikeStarted = false;
            startedOutsideAttackBox = !MeleeHitUtility.IsOverlapping(style.HitCollider, enemy.Perception.TargetCollider);
            
            telegraphStopDistance = Mathf.Max(style.DesiredDistance(enemy),
                enemy.HorizontalDistance - style.TelegraphAdvance);
            
            if (enemy.Perception.Target != null)
                enemy.Movement.MoveTo(enemy.Perception.Target.position, enemy.Config.walkSpeed, telegraphStopDistance);
            
            if (style.AttackData != null && !string.IsNullOrEmpty(style.AttackData.telegraphAnimation))
                enemy.AnimationManager?.PlayAnimation(style.AttackData.telegraphAnimation);
        }

        public void Tick(EnemyBase enemy, float dt)
        {
            if (enemy.Perception.Target == null)
            {
                enemy.RequestTransition(enemy.States.Idle);
                return;
            }
            
            enemy.Movement.FaceTarget(enemy.Perception.Target, dt);
            if (!strikeStarted)
            {
                if (enemy.Perception.Target != null)
                    enemy.Movement.MoveTo(enemy.Perception.Target.position, enemy.Config.walkSpeed, telegraphStopDistance);
                
                telegraphRemaining -= dt;
                enemy.AttackIndicator.UpdateRemaining(telegraphRemaining);
                
                if (telegraphRemaining <= 0f)
                {
                    strikeStarted = true;
                    
                    enemy.AttackIndicator.Strike();
                    enemy.Movement.Stop();
                    enemy.AnimationManager.PlayAnimation(style.AttackData.animationName, style.AttackData.crossFadeDuration);
                    
                    lunge.Begin(enemy, style.HitCollider, style.LungeDistance, 0.3f, startedOutsideAttackBox);
                }
                return;
            }
            
            bool lunging = lunge.IsActive;
            lunge.Tick(enemy, dt, style.LungeSpeed, style.DesiredDistance(enemy));
            
            if (enemy.IsAttackWindowOpen || lunging)
                PerformHitDetection(enemy);

            if (!active) return;
            if (enemy.IsAnimationFinished)
            {
                style.FinishAttack(enemy);
            }
        }

        public void Exit(EnemyBase enemy)
        {
            active = false;
            lunge.Reset();
            
            enemy.AttackIndicator.Hide();
            enemy.Movement.Stop();
            
            enemy.IsAttackWindowOpen = false;
            enemy.IsAnimationFinished = false;
            
            hitThisAttack.Clear();
        }
        
        private void PerformHitDetection(EnemyBase enemy)
        {
            var attack = style.AttackData;
            MeleeHitUtility.Hit(style.HitCollider, style.HittableLayers, attack.ToHitInfo(), enemy.gameObject, hitThisAttack,
                info => active);
        }
    }
}
