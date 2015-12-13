using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace VRInteraction
{
    public class VRIInteractables : MonoBehaviour
    {
        private static Dictionary<Collider, VRIInteractable> ColliderMapping;
        private static Dictionary<VRIInteractable, Collider[]> VRIInteractableMapping;

        private static bool Initialized = false;

        public static void Initialize()
        {
            ColliderMapping = new Dictionary<Collider, VRIInteractable>();
            VRIInteractableMapping = new Dictionary<VRIInteractable, Collider[]>();

            Initialized = true;
        }

        public static void Register(VRIInteractable interactable, Collider[] colliders)
        {
            VRIInteractableMapping.Add(interactable, colliders);

            for (int index = 0; index < colliders.Length; index++)
            {
                ColliderMapping.Add(colliders[index], interactable);
            }
        }

        public static void Deregister(VRIInteractable interactable)
        {
            VRIPlayer.DeregisterInteractable(interactable);

            ColliderMapping = ColliderMapping.Where(mapping => mapping.Value != interactable).ToDictionary(mapping => mapping.Key, mapping => mapping.Value);
            VRIInteractableMapping.Remove(interactable);
        }

        public static VRIInteractable GetInteractable(Collider collider)
        {
            VRIInteractable interactable;
            ColliderMapping.TryGetValue(collider, out interactable);
            return interactable;
        }
    }
}