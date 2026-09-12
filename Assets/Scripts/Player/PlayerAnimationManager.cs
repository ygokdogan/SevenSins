using System;
using Input;
using Player.IK;
using UnityEngine;

namespace Player
{
    public class PlayerAnimationManager : MonoBehaviour
    {
        private bool sheathed = true;
        
        private Animator anim;
        private InputHandler input;

        private void Awake()
        {
            input = GetComponentInParent<InputHandler>();
            anim = GetComponent<Animator>();

            sheathed = true;
        }

        private void Update()
        {
            if (input.SheatheInput)
            {
                input.UseSheatheInput();
                if (sheathed)
                {
                    HandIKManager.SetWeight(1f);
                    anim.Play("Unsheathe");
                }
                else
                {
                    HandIKManager.SetWeight(0f);
                    anim.Play("Sheathe");
                }
                sheathed = !sheathed;
                
            }
        }
    }
}