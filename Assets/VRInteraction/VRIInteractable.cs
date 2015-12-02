using UnityEngine;
using System.Collections;

namespace VRInteraction
{
    [RequireComponent(typeof(Rigidbody))]
    public class VRIInteractable : MonoBehaviour
    {
        public Rigidbody Rigidbody;

        public Transform InteractionPoint;

        public bool CanAttached = true;

        public VRIHand AttachedBy = null;


        private float AttachedRotationMagic = 1000f;
        private float AttachedPositionMagic = 2000f;
        private float DropDistance = 3f;

        public bool IsAttached
        {
            get
            {
                return AttachedBy != null;
            }
        }

        private void Awake()
        {   
            Rigidbody = this.GetComponent<Rigidbody>();

            if (InteractionPoint == null)
                InteractionPoint = this.transform;
        }

        private void Start()
        {
            VRIInteractables.Register(this, this.GetComponentsInChildren<Collider>());
        }

        private void FixedUpdate()
        {
            if (IsAttached == true)
            {
                if (Vector3.Distance(this.transform.position, AttachedBy.transform.position) > DropDistance)
                {
                    AttachedBy.EndInteraction();
                }
                else
                {
                    Vector3 toHandPos = (AttachedBy.transform.position - this.transform.position);
                    Rigidbody.velocity = toHandPos * AttachedPositionMagic * Time.fixedDeltaTime;

                    Quaternion toHandRot = Quaternion.RotateTowards(this.transform.rotation, AttachedBy.transform.rotation, AttachedRotationMagic * Time.fixedDeltaTime);
                    this.Rigidbody.MoveRotation(toHandRot); //todo: use angular velocity

                    if(this.transform.rotation != AttachedBy.transform.rotation)
                    {
                        Rigidbody.angularVelocity = Vector3.Cross(this.Rigidbody.velocity, AttachedBy.Rigidbody.velocity);
                    }
                    else
                    {
                        this.Rigidbody.angularVelocity = new Vector3(0, 0, 0);
                    }

                }
            }
        }

        public void BeginInteraction(VRIHand hand)
        {
            AttachedBy = hand;
        }

        public void InteractingUpdate(VRIHand hand)
        {
            if (hand.UseButtonUp == true)
            {
                UseButtonUp();
            }
        }

        public void EndInteraction()
        {
            AttachedBy = null;
        }

        public virtual void UseButtonUp()
        {

        }
    }
}