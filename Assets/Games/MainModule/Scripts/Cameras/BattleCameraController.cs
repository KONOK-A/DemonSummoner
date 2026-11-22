using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Games.MainModule.Scripts.Cameras
{
    [RequireComponent(typeof(Camera))]
    [MovedFrom(true, sourceNamespace: "Games.MainModule.Scripts.Carama", sourceClassName: "CamaraController")]
    public class BattleCameraController : MonoBehaviour
    {
        private const float ReferenceAspect = 16f / 9f;

        public float Left;
        public float Right;
        public float speed;

        private Camera battleCamera;
        private float previousInset;

        private void Awake()
        {
            battleCamera = GetComponent<Camera>();
        }

        private void Update()
        {
            // Preserve the visible end of the lane when the camera is already at a pan limit.
            float inset = Mathf.Max(0f, ReferenceAspect - battleCamera.aspect) * battleCamera.orthographicSize;
            var position = transform.position;
            float x = position.x;
            if (Mathf.Approximately(x, Left - previousInset))
                x = Left - inset;
            else if (Mathf.Approximately(x, Right + previousInset))
                x = Right + inset;
            previousInset = inset;

            if (Input.GetKey(KeyCode.A))
                x -= Time.deltaTime * speed;
            else if (Input.GetKey(KeyCode.D))
                x += Time.deltaTime * speed;

            position.x = Mathf.Clamp(x, Left - inset, Right + inset);
            transform.position = position;
        }
    }
}
