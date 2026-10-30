using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace BalanceBridge
{
    public enum RoundState { Scanning, Playing, Paused, Won, Lost }

    public class GameController : MonoBehaviour
    {
        public Camera ARCamera;
        public ARPlaneManager Planes;
        public ARRaycastManager Raycasts;
        public ARSession Session;
        public bool PracticeMode;
        public RoundState State { get; private set; } = RoundState.Scanning;
        public BallController Ball { get; private set; }
        public GameObject Course { get; private set; }
        public float Elapsed { get; private set; }
        public int Level = 1;
        GameUI ui;
        GameObject reticle;
        Pose placement;
        bool canPlace, suspended, permissionDenied;
        float lastCameraFrame, scanStarted;
        float? brightness;
        ARCameraManager cameraManager;
        GameObject preview;
        bool recovering;
        readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

        IEnumerator Start()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            ui = new GameObject("Balance UI").AddComponent<GameUI>(); ui.Build(this);
            ui.Joystick.gameObject.SetActive(false); ui.Retry.interactable = false; ui.Reposition.interactable = false;
#if UNITY_EDITOR || UNITY_STANDALONE
            PracticeMode = true;
#endif
            if (PracticeMode)
            {
                Session.enabled = false; Planes.enabled = false; Raycasts.enabled = false;
                ARCamera.GetComponent<ARCameraBackground>().enabled = false;
                ARCamera.GetComponent<ARCameraManager>().enabled = false;
                var driver = ARCamera.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
                if (driver != null) driver.enabled = false;
                ARCamera.clearFlags = CameraClearFlags.SolidColor; ARCamera.backgroundColor = new Color(0.035f, 0.07f, 0.095f);
                ARCamera.transform.SetPositionAndRotation(new Vector3(0.65f, 0.80f, -0.65f), Quaternion.LookRotation(new Vector3(-0.65f, -0.72f, 1.12f)));
                CreateRound(new Pose(Vector3.zero, Quaternion.identity));
                yield break;
            }
#if UNITY_ANDROID
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                var callbacks = new UnityEngine.Android.PermissionCallbacks();
                callbacks.PermissionDenied += _ => permissionDenied = true;
                callbacks.PermissionDeniedAndDontAskAgain += _ => permissionDenied = true;
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera, callbacks);
            }
#endif
            CreateReticle();
            cameraManager = ARCamera.GetComponent<ARCameraManager>();
            cameraManager.frameReceived += CameraFrame;
            scanStarted = lastCameraFrame = Time.unscaledTime;
        }
        void CameraFrame(ARCameraFrameEventArgs args)
        {
            lastCameraFrame = Time.unscaledTime;
            brightness = args.lightEstimation.averageBrightness;
        }
        void OnDestroy() { if (cameraManager != null) cameraManager.frameReceived -= CameraFrame; }
        public void RecoverCamera()
        {
            if (!recovering && !PracticeMode) StartCoroutine(RestartCamera());
        }
        IEnumerator RestartCamera()
        {
            recovering = true; Reposition();
            ui.Primary.interactable = false;
            Session.enabled = false; cameraManager.enabled = false;
            yield return null;
            cameraManager.enabled = true; Session.enabled = true; Session.Reset();
            scanStarted = lastCameraFrame = Time.unscaledTime;
            recovering = false;
        }
        void CreateReticle()
        {
            reticle = new GameObject("Surface placement cue");
            var line = reticle.AddComponent<LineRenderer>();
            line.useWorldSpace = false; line.loop = true; line.positionCount = 48; line.widthMultiplier = 0.003f;
            line.material = new Material(Shader.Find("Sprites/Default")); line.startColor = line.endColor = new Color(0.25f, 1f, 0.8f);
            for (int i = 0; i < 48; i++) { float angle = i * Mathf.PI * 2 / 48; line.SetPosition(i, new Vector3(Mathf.Cos(angle) * 0.06f, 0.005f, Mathf.Sin(angle) * 0.06f)); }
            reticle.SetActive(false);
            preview = new GameObject("One metre bridge footprint");
            preview.transform.SetParent(reticle.transform, false);
            var outline = preview.AddComponent<LineRenderer>();
            outline.useWorldSpace = false; outline.loop = false; outline.positionCount = 8; outline.widthMultiplier = 0.004f;
            outline.material = line.material; outline.startColor = outline.endColor = new Color(0.25f, 1f, 0.8f);
            var points = new[] { new Vector3(-0.115f, .007f, -.115f), new Vector3(-0.115f,.007f,1.035f), new Vector3(.115f,.007f,1.035f), new Vector3(.115f,.007f,-.115f), new Vector3(-.115f,.007f,-.115f), new Vector3(0,.007f,.55f), new Vector3(-.05f,.007f,.48f), new Vector3(0,.007f,.55f) };
            outline.SetPositions(points);
        }
        void Update()
        {
            if (ui == null) return;
            if (!PracticeMode && State == RoundState.Scanning) UpdatePlacement();
            if (!PracticeMode && State == RoundState.Playing && ARSession.state != ARSessionState.SessionTracking) PauseRound();
            if (State == RoundState.Playing)
            {
                Elapsed += Time.deltaTime;
                EvaluateRound();
                ui.Timer.text = Elapsed.ToString("00.0") + "s";
                float progress = Mathf.Clamp01(Course.transform.InverseTransformPoint(Ball.Body.position).z / 0.87f);
                ui.SetProgress(progress); ui.Progress.text = Mathf.RoundToInt(progress * 100) + "% CROSSED";
            }
            if (State == RoundState.Paused)
                ui.Primary.interactable = !suspended && (PracticeMode || ARSession.state == ARSessionState.SessionTracking);
        }
        void UpdatePlacement()
        {
            canPlace = !permissionDenied && ARSession.state == ARSessionState.SessionTracking && Raycasts.Raycast(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), hits, TrackableType.PlaneWithinPolygon);
            if (canPlace)
            {
                var plane = Planes.GetPlane(hits[0].trackableId);
                canPlace = plane != null && plane.alignment == PlaneAlignment.HorizontalUp;
                if (canPlace)
                {
                    var forward = Vector3.ProjectOnPlane(ARCamera.transform.forward, Vector3.up).normalized;
                    if (forward.sqrMagnitude < 0.1f) forward = Vector3.ProjectOnPlane(ARCamera.transform.up, Vector3.up).normalized;
                    placement = new Pose(hits[0].pose.position, Quaternion.LookRotation(forward, Vector3.up));
                    reticle.transform.SetPositionAndRotation(placement.position, placement.rotation);
                }
            }
            if (reticle != null) reticle.SetActive(canPlace);
            ui.Primary.interactable = canPlace || permissionDenied;
            ui.PrimaryLabel.text = permissionDenied ? "RETRY CAMERA PERMISSION" : canPlace ? "PLACE BRIDGE HERE" : "LOOKING FOR A SURFACE…";
            ui.Status.text = permissionDenied ? "CAMERA ACCESS NEEDED" : canPlace ? "SURFACE FOUND / READY TO PLACE" : "SCAN A FLOOR OR TABLE";
            ui.CardTitle.text = permissionDenied ? "Allow camera access" : canPlace ? "Your bridge starts here" : "Find your starting point";
            ui.CardBody.text = permissionDenied ? "Camera access is required for AR. Retry, or enable Camera in Android Settings > Apps > AR Balance Bridge > Permissions." : canPlace ? "The bridge extends about one metre away from this ring. Leave room ahead, then place your course." : ARSession.state == ARSessionState.Unsupported ? "AR is unavailable. Check Google Play Services for AR and restart the app." : "Move your phone slowly over a well-lit floor or table. Aim the centre of the screen at a textured surface.";
            if (!permissionDenied && !canPlace && ARSession.state != ARSessionState.Unsupported)
            {
                string reason = ARSession.notTrackingReason.ToString();
                if (Time.unscaledTime - lastCameraFrame > 6)
                { ui.CardTitle.text = "Camera feed interrupted"; ui.CardBody.text = "Check that Android camera access is on, then tap RESET AR above. This clears the old placement and restarts scanning."; }
                else if (reason == "InsufficientLight" || (brightness.HasValue && brightness.Value < 0.15f))
                { ui.CardTitle.text = "A little more light"; ui.CardBody.text = "Turn on a room light and aim at a textured floor or table. Avoid reflections and deep shadows."; }
                else if (reason == "ExcessiveMotion")
                { ui.CardTitle.text = "Move more slowly"; ui.CardBody.text = "Sweep gently across the surface. Fast movement makes it harder to track your surroundings."; }
                else if (Time.unscaledTime - scanStarted > 12)
                { ui.CardTitle.text = "Point down at a surface"; ui.CardBody.text = "Scan a floor or table with visible detail. Blank walls do not qualify. Move sideways slowly; use RESET AR if the camera gets stuck."; }
                ui.Status.text = "SCANNING / " + (ARSession.notTrackingReason == NotTrackingReason.None ? "AIM AT FLOOR OR TABLE" : reason.ToUpperInvariant());
            }
            if (canPlace) ui.CardBody.text = "The outline previews the one-metre course and its direction. Leave the entire footprint clear, then place. Push MOVE up and make small side corrections.";
        }
        void FixedUpdate()
        {
            if (State != RoundState.Playing || Ball == null || ui == null) return;
            var input = ui.Joystick.Value;
            if (PracticeMode && Keyboard.current != null)
            {
                var keys = Keyboard.current;
                input += new Vector2((keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0) - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0), (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1 : 0) - (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1 : 0));
            }
            Ball.Step(Vector2.ClampMagnitude(input, 1), Course.transform.rotation);
        }
        public void CreateRound(Pose pose)
        {
            if (Course != null) Destroy(Course);
            if (Ball != null) Destroy(Ball.gameObject);
            Course = BridgeFactory.Create(pose, Level);
            Ball = BridgeFactory.CreateBall(Course.transform.TransformPoint(new Vector3(0, 0.055f, 0)));
            State = RoundState.Playing; Elapsed = 0;
            if (reticle != null) reticle.SetActive(false);
            if (ui != null)
            {
                ui.SetControls(true, true);
                ui.Card.SetActive(false); ui.Joystick.gameObject.SetActive(true); ui.Retry.interactable = true; ui.Reposition.interactable = true;
                ui.Joystick.Clear(); ui.Status.text = PracticeMode ? "PRACTICE MODE / NOT AR" : "BRIDGE PLACED / CROSS CAREFULLY";
                ui.Hint.text = PracticeMode ? "Practice mode • WASD / arrows or joystick • up moves along the bridge" : "Drag the joystick gently • push up to move along the bridge";
                ui.SetProgress(0); ui.Timer.text = "00.0s";
                ui.Status.text = "LEVEL " + Level + (Level == 2 ? " / NARROW MIDDLE" : " / FIRST CROSSING");
            }
            Debug.Log("BALANCE: round started" + (PracticeMode ? " (practice)" : " (AR)"));
        }
        public void EvaluateRound()
        {
            if (State != RoundState.Playing || Ball == null || Course == null) return;
            var local = Course.transform.InverseTransformPoint(Ball.Body.position);
            if (local.y < -0.24f) EndRound(false);
            else if (local.z >= 0.87f && local.z <= 1.035f && Mathf.Abs(local.x) < 0.115f && local.y > 0 && local.y < 0.10f) EndRound(true);
        }
        void EndRound(bool won)
        {
            State = won ? RoundState.Won : RoundState.Lost;
            Ball.Body.linearVelocity = Vector3.zero; Ball.Body.angularVelocity = Vector3.zero; Ball.Body.isKinematic = true;
            if (ui != null)
            {
                ui.Joystick.Clear(); ui.Joystick.gameObject.SetActive(false); ui.Card.SetActive(true);
                ui.CardTitle.text = won ? "Bridge crossed." : "A little too far.";
                ui.CardBody.text = won ? "You kept your balance in " + Elapsed.ToString("0.0") + " seconds. Try another crossing, or find a new spot." : "The ball fell off the bridge. Use small joystick movements and keep it near the middle.";
                ui.PrimaryLabel.text = "TRY AGAIN"; ui.Primary.interactable = true;
                if (won) { ui.PrimaryLabel.text = Level == 1 ? "NEXT: LEVEL 2" : "PLAY LEVEL 1 AGAIN"; ui.CardBody.text = Level == 1 ? "Crossed in " + Elapsed.ToString("0.0") + " seconds. Next: a narrower middle section. RETRY repeats this level." : "Both levels complete! Try to improve your time, or play Level 1 again."; }
                ui.Status.text = won ? "CROSSING COMPLETE" : "BALL FELL / READY TO RETRY";
                if (won) ui.SetProgress(1);
            }
            Debug.Log(won ? "BALANCE: WIN" : "BALANCE: FALL");
        }
        public void Retry()
        {
            if (Course == null || Ball == null) return;
            if (!PracticeMode && (suspended || ARSession.state != ARSessionState.SessionTracking)) { PauseRound(); return; }
            Ball.ResetAt(Course.transform.TransformPoint(new Vector3(0, 0.055f, 0)));
            State = RoundState.Playing; Elapsed = 0;
            if (ui != null) ui.SetControls(true, true);
            if (ui != null) { ui.Card.SetActive(false); ui.Joystick.gameObject.SetActive(true); ui.Joystick.Clear(); ui.SetProgress(0); ui.Status.text = PracticeMode ? "PRACTICE MODE / NOT AR" : "BRIDGE PLACED / CROSS CAREFULLY"; }
        }
        public void Reposition()
        {
            if (PracticeMode) { Retry(); return; }
            if (Ball != null) Destroy(Ball.gameObject);
            if (Course != null) Destroy(Course);
            State = RoundState.Scanning; canPlace = false; Elapsed = 0;
            scanStarted = Time.unscaledTime;
            if (ui != null) { ui.Card.SetActive(true); ui.Joystick.Clear(); ui.Joystick.gameObject.SetActive(false); ui.Retry.interactable = false; ui.Reposition.interactable = false; ui.SetProgress(0); ui.Timer.text = "00.0s"; ui.Progress.text = "ONE BRIDGE. STAY BALANCED."; }
        }
        void PauseRound()
        {
            if (Ball == null) return;
            State = RoundState.Paused;
            Ball.Body.linearVelocity = Vector3.zero; Ball.Body.angularVelocity = Vector3.zero; Ball.Body.isKinematic = true;
            if (ui != null) { ui.SetControls(true, false); ui.Card.SetActive(true); ui.CardTitle.text = "Hold steady."; ui.CardBody.text = "MOVE is disabled while paused. Point back at the bridge, then resume. If the camera stays black, RESET AR clears this round and restarts scanning."; ui.PrimaryLabel.text = "RESUME CROSSING"; ui.Status.text = "PAUSED / WAITING FOR TRACKING"; }
        }
        public void PrimaryAction()
        {
            if (State == RoundState.Won)
            {
                if (!PracticeMode && (suspended || ARSession.state != ARSessionState.SessionTracking)) return;
                var pose = new Pose(Course.transform.position, Course.transform.rotation);
                Level = Level == 1 ? 2 : 1; CreateRound(pose); return;
            }
            if (State == RoundState.Paused)
            {
                if (suspended || (!PracticeMode && ARSession.state != ARSessionState.SessionTracking)) return;
                State = RoundState.Playing; Ball.Body.isKinematic = false; Ball.Body.WakeUp();
                ui.SetControls(true, true);
                ui.Card.SetActive(false); ui.Joystick.gameObject.SetActive(true); ui.Joystick.Clear(); ui.Status.text = "CROSSING RESUMED";
            }
            else if (State == RoundState.Scanning)
            {
#if UNITY_ANDROID
                if (permissionDenied)
                {
                    permissionDenied = false;
                    var callbacks = new UnityEngine.Android.PermissionCallbacks();
                    callbacks.PermissionDenied += _ => permissionDenied = true;
                    callbacks.PermissionDeniedAndDontAskAgain += _ => permissionDenied = true;
                    UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera, callbacks);
                    return;
                }
#endif
                if (canPlace) CreateRound(placement);
            }
            else Retry();
        }
        void OnApplicationPause(bool paused) { suspended = paused; if (ui != null) ui.Joystick.Clear(); if (paused && State == RoundState.Playing) PauseRound(); }
        void OnApplicationFocus(bool focused) { if (!focused && State == RoundState.Playing) PauseRound(); }
    }
}
