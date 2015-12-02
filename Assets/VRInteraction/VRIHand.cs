using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace VRInteraction
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class VRIHand : MonoBehaviour
    {
        private Valve.VR.EVRButtonId HoldButton = Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger;
        public bool HoldButtonDown = false;
        public bool HoldButtonUp = false;
        public bool HoldButtonPressed = false;

        private Valve.VR.EVRButtonId UseButton = Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad;
        public bool UseButtonDown = false;
        public bool UseButtonUp = false;
        public bool UseButtonPressed = false;

        public Rigidbody Rigidbody;

        private Dictionary<VRIInteractable, Dictionary<Collider, float>> CurrentlyHoveringOver;

        private SteamVR_Controller.Device Controller;


        private VRIInteractable CurrentlyInteracting;

        private int EstimationSampleIndex;
        private Vector3[] LastPositions;
        private Quaternion[] LastRotations;
        private float[] LastDeltas;
        private int EstimationSamples = 5;

        public bool IsHovering
        {
            get
            {
                return CurrentlyHoveringOver.Any(kvp => kvp.Value.Count > 0);
            }
        }
        public bool IsInteracting
        {
            get
            {
                return CurrentlyInteracting != null;
            }
        }


        private void Awake()
        {
            Rigidbody = this.GetComponent<Rigidbody>();

            CurrentlyHoveringOver = new Dictionary<VRIInteractable, Dictionary<Collider, float>>();

            LastPositions = new Vector3[EstimationSamples];
            LastRotations = new Quaternion[EstimationSamples];
            LastDeltas = new float[EstimationSamples];
            EstimationSampleIndex = 0;
        }

        private void Update()
        {
            if (Controller == null)
                return;

            HoldButtonPressed = Controller.GetPress(HoldButton);
            HoldButtonDown = Controller.GetPressDown(HoldButton);
            HoldButtonUp = Controller.GetPressUp(HoldButton);

            UseButtonPressed = Controller.GetPress(UseButton);
            UseButtonDown = Controller.GetPressDown(UseButton);
            UseButtonUp = Controller.GetPressUp(UseButton);

            if (HoldButtonDown == true)
            {
                if (CurrentlyInteracting == null)
                {
                    PickupClosest();
                }
            }
            else if (HoldButtonUp == true && CurrentlyInteracting != null)
            {
                EndInteraction();
            }

            if (IsInteracting == true)
            {
                CurrentlyInteracting.InteractingUpdate(this);
            }
        }

        public Vector3 GetVelocityEstimation()
        {
            float delta = LastDeltas.Sum();
            Vector3 distance = Vector3.zero;

            for (int index = 0; index < LastPositions.Length-1; index++)
            {
                Vector3 diff = LastPositions[index + 1] - LastPositions[index];
                distance += diff;
            }

            return distance / delta;
        }

        public Vector3 GetPositionDelta()
        {
            int last = EstimationSampleIndex - 1;
            int secondToLast = EstimationSampleIndex - 2;

            if (last < 0)
                last += EstimationSamples;
            if (secondToLast < 0)
                secondToLast += EstimationSamples;

            return LastPositions[last] - LastPositions[secondToLast];
        }

        private void FixedUpdate()
        {
            LastPositions[EstimationSampleIndex] = this.transform.position;
            LastRotations[EstimationSampleIndex] = this.transform.rotation;
            LastDeltas[EstimationSampleIndex] = Time.fixedDeltaTime;
            EstimationSampleIndex++;
            if (EstimationSampleIndex >= LastPositions.Length)
                EstimationSampleIndex = 0;

            if (Controller != null && IsInteracting == false && IsHovering == true)
            {
                Controller.TriggerHapticPulse(100);
            }
        }

        private void BeginInteraction(VRIInteractable interactable)
        {
            if (interactable.CanAttached == true)
            {
                if (interactable.AttachedBy != null)
                {
                    interactable.AttachedBy.EndInteraction();
                }

                CurrentlyInteracting = interactable;
                CurrentlyInteracting.BeginInteraction(this);
            }
        }

        public void EndInteraction()
        {
            if (CurrentlyInteracting != null)
            {
                CurrentlyInteracting.EndInteraction();
                CurrentlyInteracting = null;
            }
        }


        private void PickupClosest()
        {
            VRIInteractable closest = null;
            float closestDistance = float.MaxValue;

            foreach (var hovering in CurrentlyHoveringOver)
            {
                float distance = Vector3.Distance(this.transform.position, hovering.Key.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = hovering.Key;
                }
            }

            if (closest != null)
            {
                BeginInteraction(closest);
            }
        }

        private void OnTriggerEnter(Collider collider)
        {
            VRIInteractable interactable = VRIInteractables.GetInteractable(collider);
            if (interactable == null)
                return;

            if (CurrentlyHoveringOver.ContainsKey(interactable) == false)
                CurrentlyHoveringOver[interactable] = new Dictionary<Collider, float>();

            if (CurrentlyHoveringOver[interactable].ContainsKey(collider) == false)
                CurrentlyHoveringOver[interactable][collider] = Time.time;
        }

        private void OnTriggerStay(Collider collider)
        {
            VRIInteractable interactable = VRIInteractables.GetInteractable(collider);
            if (interactable == null)
                return;

            if (CurrentlyHoveringOver.ContainsKey(interactable) == false)
                CurrentlyHoveringOver[interactable] = new Dictionary<Collider, float>();

            if (CurrentlyHoveringOver[interactable].ContainsKey(collider) == false)
                CurrentlyHoveringOver[interactable][collider] = Time.time;
        }

        private void OnTriggerExit(Collider collider)
        {
            VRIInteractable interactable = VRIInteractables.GetInteractable(collider);
            if (interactable == null)
                return;

            if (CurrentlyHoveringOver.ContainsKey(interactable) == true)
            {
                if (CurrentlyHoveringOver[interactable].ContainsKey(collider) == true)
                {
                    CurrentlyHoveringOver[interactable].Remove(collider);
                }
            }
        }


        private void SetDeviceIndex(int index)
        {
            Controller = SteamVR_Controller.Input(index);
        }
    }
}