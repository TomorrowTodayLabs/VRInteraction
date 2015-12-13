﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace VRInteraction
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class VRIHand : MonoBehaviour
    {
        private Valve.VR.EVRButtonId HoldButton = Valve.VR.EVRButtonId.k_EButton_Grip;
        public bool HoldButtonDown = false;
        public bool HoldButtonUp = false;
        public bool HoldButtonPressed = false;

        private Valve.VR.EVRButtonId UseButton = Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger;
        public bool UseButtonDown = false;
        public bool UseButtonUp = false;
        public bool UseButtonPressed = false;

        public Rigidbody Rigidbody;

        private VisibilityLevel CurrentVisibility = VisibilityLevel.Visible;

        private HandState CurrentHandState = HandState.Uninitialized;

        private Dictionary<VRIInteractable, Dictionary<Collider, float>> CurrentlyHoveringOver;

        private SteamVR_Controller.Device Controller;


        private VRIInteractable CurrentlyInteracting;

        private int EstimationSampleIndex;
        private Vector3[] LastPositions;
        private Quaternion[] LastRotations;
        private float[] LastDeltas;
        private int EstimationSamples = 5;

        private Collider[] TriggerColliders;
        private Collider[] NontriggerColliders;

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

            Collider[] colliders = this.GetComponentsInChildren<Collider>();
            NontriggerColliders = colliders.Where(collider => collider.isTrigger == false).ToArray();
            TriggerColliders = colliders.Where(collider => collider.isTrigger == true).ToArray();
        }

        private void Update()
        {
            if (Controller == null || CurrentHandState == HandState.Uninitialized)
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
                EndInteraction(null);
            }

            if (IsInteracting == true)
            {
                CurrentlyInteracting.InteractingUpdate(this);
            }

            if (VRIPlayer.Instance.PhysicalHands == true)
                UpdateVisibilityAndColliders();
        }

        private void UpdateVisibilityAndColliders()
        {
            if (HoldButtonPressed == true && IsInteracting == false)
            {
                if (CurrentHandState != HandState.GripDownNotInteracting)
                {
                    SetVisibility(VisibilityLevel.Visible);
                    SetTriggerState(false);
                    SetNontriggerState(true);
                    CurrentHandState = HandState.GripDownNotInteracting;
                }
            }
            else if (HoldButtonDown == true && IsInteracting == true)
            {
                if (CurrentHandState != HandState.GripDownInteracting)
                {
                    SetVisibility(VisibilityLevel.Ghost);
                    SetTriggerState(true);
                    SetNontriggerState(false);
                    CurrentHandState = HandState.GripDownInteracting;
                }
            }
            else if (IsInteracting == false)
            {
                if (CurrentHandState != HandState.Idle)
                {
                    SetVisibility(VisibilityLevel.Ghost);
                    SetTriggerState(true);
                    SetNontriggerState(false);
                    CurrentHandState = HandState.Idle;
                }
            }
        }

        private void SetTriggerState(bool state)
        {
            for (int index = 0; index < TriggerColliders.Length; index++)
            {
                TriggerColliders[index].enabled = state;
            }
        }

        private void SetNontriggerState(bool state)
        {
            for (int index = 0; index < NontriggerColliders.Length; index++)
            {
                NontriggerColliders[index].enabled = state;
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
                if (interactable.AttachedHand != null)
                {
                    interactable.AttachedHand.EndInteraction(null);
                }

                CurrentlyInteracting = interactable;
                CurrentlyInteracting.BeginInteraction(this);
            }
        }

        public void EndInteraction(VRIInteractable item)
        {
            if (item != null && CurrentlyHoveringOver.ContainsKey(item) == true)
                CurrentlyHoveringOver.Remove(item);

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
                if (hovering.Key == null)
                    continue;

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
            StartCoroutine(DoSetDeviceIndex(index));
        }

        public void DeregisterInteractable(VRIInteractable interactable)
        {
            if (CurrentlyInteracting == interactable)
                CurrentlyInteracting = null;

            CurrentlyHoveringOver.Remove(interactable);
        }

        private void SetVisibility(VisibilityLevel visibility)
        {
            CurrentVisibility = visibility;

            Renderer[] renderers = this.GetComponentsInChildren<Renderer>();

            for (int index = 0; index < renderers.Length; index++)
            {
                Color color = renderers[index].material.color;
                color.a = (float)visibility / 100;
                renderers[index].material.color = color;
            }
        }

        private IEnumerator DoSetDeviceIndex(int index)
        {
            yield return null;

            SetVisibility(CurrentVisibility);

            if (CurrentHandState == HandState.Uninitialized)
            {
                InitializeController();
            }
        }

        private void InitializeController()
        {
            if (VRIPlayer.Instance.PhysicalHands == true)
            {
                Renderer[] renderers = this.GetComponentsInChildren<Renderer>();
                for (int index = 0; index < renderers.Length; index++)
                {
                    VRIHelpers.SetTransparent(renderers[index].material);
                }

                Transform trackhat = this.transform.FindChild("trackhat");

                Collider trackhatCollider = trackhat.GetComponent<Collider>();
                if (trackhatCollider == null)
                    trackhatCollider = trackhat.gameObject.AddComponent<BoxCollider>();

                Transform body = this.transform.FindChild("body");
                Collider bodyCollider = body.GetComponent<Collider>();
                if (bodyCollider == null)
                    bodyCollider = body.gameObject.AddComponent<BoxCollider>();

                NontriggerColliders = new Collider[] { trackhatCollider, bodyCollider };
                SetNontriggerState(false);
                UpdateVisibilityAndColliders();
            }
            else
            {
                CurrentHandState = HandState.Idle;
            }
        }
    }

    public enum VisibilityLevel
    {
        Invisible = 0,
        Ghost = 80,
        Visible = 100,
    }

    public enum HandState
    {
        Uninitialized, 
        Idle,
        GripDownNotInteracting,
        GripDownInteracting,
    }
}