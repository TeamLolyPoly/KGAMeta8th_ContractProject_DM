using System.IO;
using UnityEngine;

namespace NoteEditor
{
    /// <summary>
    /// 오디오 관련 파일 경로를 관리하는 유틸리티 클래스
    /// </summary>
    public static class AudioPathProvider
    {
        private static string _basePath = Path.Combine(Application.persistentDataPath, "Tracks");

        public static string BasePath => _basePath;

        public static string AlbumArtPath => Path.Combine(BasePath, "AlbumArts");

        public static string TrackDataPath => Path.Combine(BasePath, "Data");

        public static string NoteMapPath => Path.Combine(BasePath, "NoteMaps");

        /// <summary>
        /// 기본 경로를 설정합니다.
        /// </summary>
        /// <param name="path">설정할 기본 경로</param>
        public static void SetBasePath(string path)
        {
            _basePath = path;
            EnsureDirectoriesExist();
        }

        /// <summary>
        /// 트랙 이름으로 오디오 파일 경로를 가져옵니다.
        /// </summary>
        /// <param name="trackName">트랙 이름</param>
        /// <returns>오디오 파일 경로</returns>
        public static string GetAudioFilePath(string trackName)
        {
            return Path.Combine(BasePath, $"{trackName}.wav");
        }

        /// <summary>
        /// 트랙 이름으로 앨범 아트 파일 경로를 가져옵니다.
        /// </summary>
        /// <param name="trackName">트랙 이름</param>
        /// <returns>앨범 아트 파일 경로</returns>
        public static string GetAlbumArtPath(string trackName)
        {
            return Path.Combine(AlbumArtPath, $"{trackName}.png");
        }

        /// <summary>
        /// 트랙 이름으로 노트맵 파일 경로를 가져옵니다.
        /// </summary>
        /// <param name="trackName">트랙 이름</param>
        /// <returns>노트맵 파일 경로</returns>
        public static string GetNoteMapPath(string trackName)
        {
            return Path.Combine(NoteMapPath, $"{trackName}.json");
        }

        /// <summary>
        /// 필요한 디렉토리가 존재하는지 확인하고, 없으면 생성합니다.
        /// </summary>
        public static void EnsureDirectoriesExist()
        {
            if (!Directory.Exists(BasePath))
                Directory.CreateDirectory(BasePath);

            if (!Directory.Exists(TrackDataPath))
                Directory.CreateDirectory(TrackDataPath);

            if (!Directory.Exists(AlbumArtPath))
                Directory.CreateDirectory(AlbumArtPath);

            if (!Directory.Exists(NoteMapPath))
                Directory.CreateDirectory(NoteMapPath);
        }
    }
}
