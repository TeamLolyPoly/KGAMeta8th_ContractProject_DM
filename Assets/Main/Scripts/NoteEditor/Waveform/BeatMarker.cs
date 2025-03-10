using UnityEngine;
using UnityEngine.UI;

namespace NoteEditor
{
    public class BeatMarker : MonoBehaviour
    {
        public Color markerColor = Color.white;
        public float width = 2f;

        private Image markerImage;

        private void Awake()
        {
            markerImage = GetComponent<Image>();
            if (markerImage == null)
            {
                markerImage = gameObject.AddComponent<Image>();
            }

            markerImage.color = markerColor;

            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
            }
        }
    }
}
