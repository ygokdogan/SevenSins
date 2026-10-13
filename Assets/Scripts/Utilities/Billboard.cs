using System;
using UnityEngine;

namespace Utilities
{
    public class Billboard : MonoBehaviour
    {
        private Camera cam;
        
        private void Start() => cam = Camera.main;

        private void LateUpdate()
        {
            if (cam == null) return;
            
            transform.rotation = cam.transform.rotation;
        }
    }
}
