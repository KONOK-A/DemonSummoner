using UnityEngine;

namespace Games.MainModule.View.VFX
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ArcherShotEffect : XFGameFramework.View
    {
        [SerializeField] private Sprite arrow;
        [SerializeField] private Sprite impactChips;

        private const float ImpactDuration = 0.11f;
        private SpriteRenderer ink;
        private Vector3 start;
        private Vector3 end;
        private float flightDuration;
        private float elapsed;
        private bool playing;

        private void Awake() => ink = GetComponent<SpriteRenderer>();

        public void Launch(Vector3 origin, Vector3 impact)
        {
            start = origin;
            end = impact;
            Vector2 offset = impact - origin;
            float distance = offset.magnitude;
            flightDuration = Mathf.Clamp(distance / 24f, 0.075f, 0.15f);
            elapsed = 0f;
            playing = true;

            float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            transform.SetPositionAndRotation(origin, Quaternion.Euler(0f, 0f, angle));
            transform.localScale = Vector3.one * Mathf.Min(1f, distance / arrow.bounds.size.x);
            ink.sprite = arrow;
            ink.color = Color.white;
            ink.enabled = true;
        }

        private void Update()
        {
            if (!playing) return;
            elapsed += Time.deltaTime;
            if (elapsed < flightDuration)
            {
                transform.position = Vector3.Lerp(start, end, elapsed / flightDuration);
                return;
            }

            float phase = (elapsed - flightDuration) / ImpactDuration;
            if (phase >= 1f)
            {
                Close();
                return;
            }

            transform.position = end;
            ink.sprite = impactChips;
            float easeOut = 1f - (1f - phase) * (1f - phase);
            transform.localScale = Vector3.one * Mathf.Lerp(0.42f, 1f, easeOut);
            ink.color = new Color(1f, 1f, 1f, 1f - phase * phase);
        }

        public override void OnRemove()
        {
            playing = false;
            ink.enabled = false;
            ink.sprite = arrow;
            ink.color = Color.white;
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            base.OnRemove();
        }
    }
}
