using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VRInteraction
{
    public class VRIPlayer : MonoBehaviour
    {
        public static VRIPlayer Instance;

        public VRIHand[] Hands;

        private Dictionary<Collider, VRIHand> ColliderToHandMapping;

        private void Awake()
        {
            Instance = this;
            VRIInteractables.Initialize();

            ColliderToHandMapping = new Dictionary<Collider, VRIHand>();
        }

        private void Start()
        {
            for (int index = 0; index < Hands.Length; index++)
            {
                Instance.ColliderToHandMapping.Add(Hands[index].GetComponent<Collider>(), Hands[index]);
            }
        }

        public VRIHand GetHand(Collider collider)
        {
            return ColliderToHandMapping[collider];
        }
    }
}