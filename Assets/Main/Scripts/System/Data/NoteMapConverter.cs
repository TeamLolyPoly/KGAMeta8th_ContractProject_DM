using System;
using UnityEngine;

public static class NoteMapConverter
{
    public static NoteMap ConvertNoteMap(JsonNoteMap jsonNoteMap)
    {
        try
        {
            NoteMap noteMap = new NoteMap();
            noteMap.bpm = jsonNoteMap.bpm;
            noteMap.beatsPerBar = jsonNoteMap.beatsPerBar;

            foreach (JsonNoteData jsonNote in jsonNoteMap.notes)
            {
                NoteData note = new NoteData();

                note.noteType = (NoteType)jsonNote.noteType;
                note.noteColor = (NoteColor)jsonNote.noteColor;
                note.direction = (NoteDirection)jsonNote.direction;
                note.noteAxis = (NoteAxis)jsonNote.noteAxis;
                note.bar = jsonNote.bar;
                note.beat = jsonNote.beat;
                note.startIndex = jsonNote.startIndex;
                note.endIndex = jsonNote.endIndex;
                note.isSymmetric = jsonNote.isSymmetric;
                note.isClockwise = jsonNote.isClockwise;
                note.durationBars = jsonNote.durationBars;
                note.durationBeats = jsonNote.durationBeats;

                note.StartCell = new Vector2Int(jsonNote.StartCellX, jsonNote.StartCellY);
                note.TargetCell = new Vector2Int(jsonNote.TargetCellX, jsonNote.TargetCellY);

                noteMap.notes.Add(note);
            }

            return noteMap;
        }
        catch (Exception e)
        {
            Debug.LogError($"JSON 파싱 오류: {e.Message}");
            Debug.LogError(e.StackTrace);

            return null;
        }
    }
}
