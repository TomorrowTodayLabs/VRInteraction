using UnityEngine;
using System.Collections;

namespace VRInteraction
{
    [RequireComponent(typeof(Rigidbody))]
    public abstract class VRIInteractable : MonoBehaviour
    {
        public Rigidbody Rigidbody;
        public bool CanAttached = true;
        public VRIHand AttachedHand = null;

        protected Collider[] Colliders;
        protected virtual float DropDistance { get { return 3f; } }

        public virtual bool IsAttached
        {
            get
            {
                return AttachedHand != null;
            }
        }

        protected virtual void Awake()
        {   
            Rigidbody = this.GetComponent<Rigidbody>();
            Colliders = this.GetComponentsInChildren<Collider>();
        }

        protected virtual void Start()
        {
            VRIInteractables.Register(this, Colliders);
        }

        protected virtual void FixedUpdate()
        {
            if (IsAttached == true)
            {
                float shortestDistance = float.MaxValue;
                for (int index = 0; index < Colliders.Length; index++)
                {
                    Vector3 closest = Colliders[index].bounds.ClosestPoint(AttachedHand.transform.position);
                    float distance = Vector3.Distance(AttachedHand.transform.position, closest);

                    if (distance < shortestDistance)
                    {
                        shortestDistance = distance;
                    }
                }

                if (shortestDistance > DropDistance)
                {
                    DroppedBecauseOfDistance();
                }
            }
        }

        //Remove items that go too high or too low.
        protected virtual void Update()
        {
            if (this.transform.position.y > 10000 || this.transform.position.y < -10000)
            {
                if (AttachedHand != null)
                    AttachedHand.EndInteraction(this);

                Destroy(this.gameObject);
            }
        }

        public virtual void BeginInteraction(VRIHand hand)
        {
            AttachedHand = hand;
        }

        public virtual void InteractingUpdate(VRIHand hand)
        {
            if (hand.UseButtonUp == true)
            {
                UseButtonUp();
            }
        }

        public virtual void EndInteraction()
        {
            AttachedHand = null;
        }

        protected virtual void DroppedBecauseOfDistance()
        {
            AttachedHand.EndInteraction(this);
        }

        public virtual void UseButtonUp()
        {

        }
    }
}