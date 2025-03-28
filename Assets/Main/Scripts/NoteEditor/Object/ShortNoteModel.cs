using UnityEngine;

public class ShortNoteModel : MonoBehaviour
{
    public Renderer rimRenderer;
    public Renderer topRenderer;

    private const float NOTE_ANGLE_OFFSET = 40f;

    private void Awake()
    {
        topRenderer.material.color = Color.green;
    }

    public void SetNoteColor(NoteColor noteColor)
    {
        switch (noteColor)
        {
            case NoteColor.None:
                rimRenderer.material.color = Color.white;
                break;
            case NoteColor.Red:
                rimRenderer.material.color = Color.red;
                break;
            case NoteColor.Blue:
                rimRenderer.material.color = Color.blue;
                break;
            case NoteColor.Yellow:
                rimRenderer.material.color = Color.yellow;
                break;
            default:
                rimRenderer.material.color = Color.white;
                break;
        }
    }

    public void SetNoteDirection(NoteDirection noteDirection)
    {
        switch (noteDirection)
        {
            case NoteDirection.None:
                transform.rotation = Quaternion.identity;
                break;
            case NoteDirection.East:
                transform.rotation = Quaternion.Euler(0f, -NOTE_ANGLE_OFFSET, 90f);
                break;
            case NoteDirection.West:
                transform.rotation = Quaternion.Euler(0f, NOTE_ANGLE_OFFSET, -90f);
                break;
            case NoteDirection.South:
                transform.rotation = Quaternion.Euler(-NOTE_ANGLE_OFFSET, 0f, 0f);
                break;
            case NoteDirection.North:
                transform.rotation = Quaternion.Euler(NOTE_ANGLE_OFFSET, 0f, 180f);
                break;
            case NoteDirection.NorthEast:
                transform.rotation = Quaternion.Euler(0f, -NOTE_ANGLE_OFFSET, 135f);
                break;
            case NoteDirection.NorthWest:
                transform.rotation = Quaternion.Euler(0f, NOTE_ANGLE_OFFSET, -135f);
                break;
            case NoteDirection.SouthEast:
                transform.rotation = Quaternion.Euler(0f, -NOTE_ANGLE_OFFSET, 45f);
                break;
            case NoteDirection.SouthWest:
                transform.rotation = Quaternion.Euler(0f, NOTE_ANGLE_OFFSET, -45f);
                break;
            default:
                transform.rotation = Quaternion.identity;
                break;
        }
    }
}
