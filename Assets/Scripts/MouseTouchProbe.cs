using UnityEngine;
using WeArt.Components;

namespace DefaultNamespace
{
    /// <summary>
    /// Lets you "touch" WeArtTouchableObjects with the mouse, without the WeArt hand.
    /// Put on an empty GameObject that also has a WeArtHapticObject.
    /// </summary>
    [RequireComponent(typeof(WeArtHapticObject))]
    public class MouseTouchProbe : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private LayerMask _touchableLayers = ~0;
        [SerializeField] private float _maxRayDistance = 100f;
        [SerializeField] private bool _requireMouseButton = true; // hold LMB to feel
        [SerializeField] private int _mouseButton = 0;

        private WeArtHapticObject _haptic;
        private Collider _current;

        private void Awake()
        {
            _haptic = GetComponent<WeArtHapticObject>();
            if (_camera == null) _camera = Camera.main;
        }

        private void Update()
        {
            _haptic.EnablingActuating(true); // allow it to send to the device

            bool active = !_requireMouseButton || Input.GetMouseButton(_mouseButton);
            if (!active) { ClearContact(); return; }

            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, _maxRayDistance, _touchableLayers))
            {
                transform.position = hit.point; // contact point = impact position

                if (hit.collider != _current)
                {
                    if (_current != null) _haptic.HandleOnTriggerExit(_current);
                    _current = hit.collider;
                    _haptic.HandleOnTriggerEnter(_current);
                }
                else
                {
                    _haptic.HandleOnTriggerStay(_current);
                }
            }
            else ClearContact();
        }

        private void ClearContact()
        {
            if (_current != null)
            {
                _haptic.HandleOnTriggerExit(_current);
                _current = null;
            }
        }

        private void OnDisable() => ClearContact();
    }
}