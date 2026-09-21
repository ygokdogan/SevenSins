using System.Collections;
using Combat;
using UnityEngine;

namespace Enemies
{
    public class CombatDummy : MonoBehaviour, IDamageable
    {

        private CharacterController characterController;

        public void TakeHit(HitInfo hitInfo)
        {
            // 5. Sound
            
        }

        
    }
}