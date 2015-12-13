using UnityEngine;
using System.Collections;

namespace VRInteraction
{
    public class VRIInteractableItem : VRIInteractable
    {
        public Transform InteractionPoint;
        
        protected float AttachedRotationMagic = 250f;
        protected float AttachedPositionMagic = 2000f;
        
        protected Transform PickupTransform;
        
        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (IsAttached == true)
            {
                Vector3 PositionDelta;
                Quaternion RotationDelta;

                if (InteractionPoint != null)
                {
                    //todo: apply the non interaction point rotation concept to this
                    RotationDelta = Quaternion.RotateTowards(this.transform.rotation, AttachedHand.transform.rotation, 500f * Time.fixedDeltaTime);
                    PositionDelta = (AttachedHand.transform.position - InteractionPoint.position);
                }
                else
                {
                    RotationDelta = Quaternion.RotateTowards(this.transform.rotation, PickupTransform.rotation, 500f * Time.fixedDeltaTime);
                    PositionDelta = (PickupTransform.position - this.transform.position);
                }

                this.Rigidbody.velocity = PositionDelta * AttachedPositionMagic * Time.fixedDeltaTime;


                float angle = 0.0f;
                Vector3 axis = Vector3.zero;
                RotationDelta.ToAngleAxis(out angle, out axis);

                //todo: make angularvelocity work. probably with an estimator.
                this.Rigidbody.angularVelocity = (4f * Mathf.Sin(angle) / AttachedRotationMagic) * axis;
                this.Rigidbody.MoveRotation(RotationDelta); //todo: use angular velocity

                //Quaternion toHandRot = Quaternion.Slerp(this.transform.rotation, AttachedBy.transform.rotation, 1f);
                //Quaternion toHandRot = Quaternion.RotateTowards(this.transform.rotation, AttachedBy.transform.rotation, AttachedRotationMagic * Time.fixedDeltaTime);

                /*
                if (this.transform.rotation != AttachedBy.transform.rotation)
                {
                    Rigidbody.angularVelocity = Vector3.Cross(this.Rigidbody.velocity, AttachedBy.Rigidbody.velocity);
                }
                else
                {
                    this.Rigidbody.angularVelocity = new Vector3(0, 0, 0);
                }
                */
            }
        }

        public override void BeginInteraction(VRIHand hand)
        {
            base.BeginInteraction(hand);

            Vector3 closestPoint = Vector3.zero;
            float shortestDistance = float.MaxValue;
            for (int index = 0; index < Colliders.Length; index++)
            {
                Vector3 closest = Colliders[index].bounds.ClosestPoint(AttachedHand.transform.position);
                float distance = Vector3.Distance(AttachedHand.transform.position, closest);

                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    closestPoint = closest;
                }
            }

            PickupTransform = new GameObject("PickupTransform: " + this.gameObject.name).transform;
            PickupTransform.parent = hand.transform;
            PickupTransform.position = this.transform.position;
            PickupTransform.rotation = this.transform.rotation;
        }

        public override void EndInteraction()
        {
            base.EndInteraction();

            Destroy(PickupTransform.gameObject);
        }
    }
}