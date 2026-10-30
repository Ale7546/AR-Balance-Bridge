using UnityEngine;

namespace BalanceBridge
{
    [RequireComponent(typeof(Rigidbody))]
    public class BallController : MonoBehaviour
    {
        public Rigidbody Body { get; private set; }
        public void Initialize()
        {
            Body = GetComponent<Rigidbody>();
            Body.mass = 0.2f;
            Body.linearDamping = 2.0f;
            Body.angularDamping = 0.15f;
            Body.sleepThreshold = 0;
            Body.maxAngularVelocity = 30;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
        public void Step(Vector2 input, Quaternion courseRotation)
        {
            if (Body.isKinematic) return;
            Vector3 force = courseRotation * new Vector3(input.x, 0, input.y);
            if (input.sqrMagnitude < 0.0025f) Body.linearVelocity = new Vector3(Body.linearVelocity.x * 0.94f, Body.linearVelocity.y, Body.linearVelocity.z * 0.94f);
            Body.AddForce(force * 1.05f, ForceMode.Acceleration);
            Vector3 horizontal = new Vector3(Body.linearVelocity.x, 0, Body.linearVelocity.z);
            if (horizontal.magnitude > 0.205f)
            {
                horizontal = horizontal.normalized * 0.205f;
                Body.linearVelocity = new Vector3(horizontal.x, Body.linearVelocity.y, horizontal.z);
            }
        }
        public void ResetAt(Vector3 position)
        {
            Body.isKinematic = false;
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Body.position = position;
            Body.rotation = Quaternion.identity;
            Body.WakeUp();
        }
    }
}

