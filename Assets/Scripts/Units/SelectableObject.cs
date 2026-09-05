using UnityEngine;

namespace AntColony.Units
{
    // SIMUL-TeaamProject(hyeonyeop 브랜치) SelectableObject.cs 참고 포팅.
    public class SelectableObject : MonoBehaviour
    {
        public bool IsSelected { get; private set; }

        [Header("Selection Visual")]
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Color selectedColor = Color.green;

        [Header("Selection Position")]
        [SerializeField] private Transform selectionPointOverride;

        private Color[] originalColors;

        private void Awake()
        {
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>();

            originalColors = new Color[renderers.Length];

            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    originalColors[i] = renderers[i].material.color;
            }
        }

        private void OnDisable()
        {
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;

            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].material.color = selected ? selectedColor : originalColors[i];
            }
        }

        public Vector3 GetSelectionWorldPosition()
        {
            return selectionPointOverride != null ? selectionPointOverride.position : transform.position;
        }
    }
}
