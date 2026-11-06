using System;
using UnityEngine;

namespace AntColony.UI
{
    // A responsive screen is rebuilt only when its available canvas size changes.
    // This keeps text at its authored size instead of shrinking the entire screen.
    public sealed class PrototypeWindowLayout : MonoBehaviour
    {
        public Action Rebuild;
        private Vector2 size;
        private void Start() => size = ((RectTransform)transform.parent).rect.size;
        private void Update()
        {
            var next = ((RectTransform)transform.parent).rect.size;
            if ((next - size).sqrMagnitude < 4) return;
            size = next;
            Rebuild?.Invoke();
        }
    }
}
