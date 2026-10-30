using UnityEngine;

namespace BalanceBridge
{
    public static class BridgeFactory
    {
        static Material Material(Color color, bool metal = false)
        {
            var template = Resources.Load<Material>("BridgeSurface");
            var material = template != null ? new Material(template) : new Material(Shader.Find("Standard"));
            material.color = color;
            material.SetFloat("_Metallic", metal ? 0.65f : 0.08f);
            material.SetFloat("_Glossiness", metal ? 0.55f : 0.25f);
            return material;
        }
        public static GameObject Create(Pose pose, int level = 1)
        {
            var root = new GameObject("Placed bridge — 1 metre");
            root.transform.SetPositionAndRotation(pose.position, pose.rotation);
            var wood = Material(new Color(0.46f, 0.25f, 0.12f));
            var lightWood = Material(new Color(0.72f, 0.45f, 0.23f));
            var dark = Material(new Color(0.06f, 0.12f, 0.15f), true);
            var mint = Material(new Color(0.12f, 0.88f, 0.70f), true);
            Box(root, "Start platform", new Vector3(0, -0.025f, 0), new Vector3(0.23f, 0.05f, 0.23f), dark);
            Box(root, "Finish platform", new Vector3(0, -0.025f, 0.92f), new Vector3(0.23f, 0.05f, 0.23f), dark);
            // A continuous collision deck avoids catching the ball on cosmetic plank edges.
            var deck = Box(root, "Continuous bridge collider", new Vector3(0, -0.020f, 0.46f), new Vector3(0.10f, 0.04f, 0.72f), wood);
            if (level == 2)
            {
                deck.transform.localScale = new Vector3(0.075f, 0.04f, 0.72f);
                Box(root, "Wide approach", new Vector3(0,-0.020f,0.22f), new Vector3(0.10f,0.04f,0.24f), wood);
                Box(root, "Wide exit", new Vector3(0,-0.020f,0.70f), new Vector3(0.10f,0.04f,0.24f), wood);
            }
            for (int i = 0; i < 14; i++)
            {
                bool narrow = level == 2 && i >= 4 && i <= 8;
                var plank = Box(root, "Wood plank " + (i + 1), new Vector3(0, 0.0005f, 0.125f + i * 0.052f), new Vector3(narrow ? 0.075f : 0.102f, 0.005f, 0.049f), narrow ? mint : i % 2 == 0 ? lightWood : wood);
                Object.DestroyImmediate(plank.GetComponent<Collider>());
            }
            Object.DestroyImmediate(Box(root, "Left under-beam", new Vector3(-0.035f, -0.05f, 0.46f), new Vector3(0.015f, 0.035f, 0.78f), dark).GetComponent<Collider>());
            Object.DestroyImmediate(Box(root, "Right under-beam", new Vector3(0.035f, -0.05f, 0.46f), new Vector3(0.015f, 0.035f, 0.78f), dark).GetComponent<Collider>());
            Object.DestroyImmediate(Box(root, "Start marker", new Vector3(0, 0.002f, 0.055f), new Vector3(0.17f, 0.004f, 0.012f), mint).GetComponent<Collider>());
            Object.DestroyImmediate(Box(root, "Finish marker", new Vector3(0, 0.002f, 0.88f), new Vector3(0.19f, 0.004f, 0.02f), mint).GetComponent<Collider>());
            Box(root, "Finish left post", new Vector3(-0.10f, 0.10f, 1.01f), new Vector3(0.012f, 0.20f, 0.012f), mint);
            Box(root, "Finish right post", new Vector3(0.10f, 0.10f, 1.01f), new Vector3(0.012f, 0.20f, 0.012f), mint);
            Box(root, "Finish arch", new Vector3(0, 0.20f, 1.01f), new Vector3(0.21f, 0.014f, 0.014f), mint);
            return root;
        }
        static GameObject Box(GameObject parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent.transform, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
        }
        public static BallController CreateBall(Vector3 position)
        {
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Player — Rigidbody ball";
            ball.transform.position = position;
            ball.transform.localScale = Vector3.one * 0.054f;
            if (ball.GetComponent<SphereCollider>() == null) ball.AddComponent<SphereCollider>();
            ball.GetComponent<Renderer>().sharedMaterial = Material(new Color(1f, 0.59f, 0.18f), true);
            var physicsMaterial = new PhysicsMaterial("Ball grip") { dynamicFriction = 0.45f, staticFriction = 0.45f, bounciness = 0.03f };
            ball.GetComponent<Collider>().sharedMaterial = physicsMaterial;
            ball.AddComponent<Rigidbody>();
            var controller = ball.AddComponent<BallController>();
            controller.Initialize();
            return controller;
        }
    }
}

