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
            // AntVisual 유닛은 기존 원형 링 두께(.055 → .13)로 선택을 구분한다.
            var visual = GetComponent<AntVisual>();
            var outline = GetComponent<SelectionOutline>();
            if (outline == null && selected && (visual == null || !visual.enabled)) outline = gameObject.AddComponent<SelectionOutline>();
            if (outline != null) outline.Show(selected);

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
