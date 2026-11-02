using UnityEngine;

namespace Genesis.Sandbox
{
    public sealed class SandboxLabel : MonoBehaviour
    {
        private Transform _camera;
        private void Start() { if (Camera.main != null) _camera = Camera.main.transform; }
        private void LateUpdate() { if (_camera != null) transform.rotation = _camera.rotation; }
    }
}
