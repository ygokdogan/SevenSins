using System;
using UnityEngine;

namespace Player.IK
{
    public class HandIKManager : MonoBehaviour
    {
        public Transform leftHandPivot;
        public Transform rightHandPivot;
        
        public Transform leftHandHoldTarget;
        public Transform leftHandSheatheTarget;
        
        public Transform rightHandHoldTarget;
        public Transform rightHandSheatheTarget;
        
        [Range(0, 1)] private static float targetWeight = 0f; // 1-Hold, 0-Sheath
        public float followSpeed = 15f;

        private void LateUpdate()
        {
            if (!leftHandHoldTarget || !leftHandSheatheTarget) return;
            
            Vector3 leftTargetPos = Vector3.Lerp(leftHandSheatheTarget.position, leftHandHoldTarget.position, targetWeight);
            Quaternion leftTargetRot = Quaternion.Lerp(leftHandSheatheTarget.rotation, leftHandHoldTarget.rotation, targetWeight);
            
            Vector3 rightTargetPos = Vector3.Lerp(rightHandSheatheTarget.position, rightHandHoldTarget.position, targetWeight);
            Quaternion rightTargetRot = Quaternion.Lerp(rightHandSheatheTarget.rotation, rightHandHoldTarget.rotation, targetWeight);
            
            leftHandPivot.position = Vector3.Lerp(leftHandPivot.position, leftTargetPos, Time.deltaTime * followSpeed);
            leftHandPivot.rotation = Quaternion.Slerp(leftHandPivot.rotation, leftTargetRot, Time.deltaTime * followSpeed);
            rightHandPivot.position = Vector3.Lerp(rightHandPivot.position, rightTargetPos, Time.deltaTime * followSpeed);
            rightHandPivot.rotation = Quaternion.Slerp(rightHandPivot.rotation, rightTargetRot, Time.deltaTime * followSpeed);
        }
        
        public static void SetWeight(float weight) => targetWeight = weight;
    }
}
