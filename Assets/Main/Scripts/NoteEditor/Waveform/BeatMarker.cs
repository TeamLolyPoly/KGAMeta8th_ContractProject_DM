using UnityEngine;
using UnityEngine.UI;

namespace NoteEditor
{
    public class BeatMarker : MonoBehaviour
    {
        public Color markerColor = Color.white;

        private Image markerImage;

        private void Awake()
        {
            markerImage = GetComponent<Image>();
            if (markerImage == null)
            {
                markerImage = gameObject.AddComponent<Image>();
            }

            markerImage.color = markerColor;
        }
    }
}
