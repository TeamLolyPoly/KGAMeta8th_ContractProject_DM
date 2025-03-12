using UnityEngine;

namespace NoteEditor
{
    public class Cell : MonoBehaviour
    {
        public NoteData noteData;
        public Vector2 cellPosition;
        public int bar;
        public int beat;

        public void Initialize(int bar, int beat, Vector2 cellPosition)
        {
            this.bar = bar;
            this.beat = beat;
            this.cellPosition = cellPosition;
        }
    }
}
