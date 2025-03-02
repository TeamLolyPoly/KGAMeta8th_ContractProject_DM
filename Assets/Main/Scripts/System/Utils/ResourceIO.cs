using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

public static class ResourceIO
{
    private static Dictionary<string, AudioClip> audioCache = new Dictionary<string, AudioClip>();
    private static Dictionary<string, Sprite> imageCache = new Dictionary<string, Sprite>();

    /// <summary>
    /// 오디오 파일을 비동기적으로 로드합니다.
    /// </summary>
    /// <param name="filePath">로드할 오디오 파일 경로</param>
    /// <param name="useCache">캐시 사용 여부</param>
    /// <returns>로드된 AudioClip과 파일명을 포함한 튜플</returns>
    public static async Task<(AudioClip clip, string fileName)> LoadAudioFileAsync(string filePath, bool useCache = true)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);

        if (useCache && audioCache.TryGetValue(fileName, out AudioClip cachedClip))
        {
            Debug.Log($"오디오 캐시에서 로드: {fileName}");
            return (cachedClip, fileName);
        }

        Debug.Log($"오디오 파일 로드 시작: {fileName}");

        try
        {
            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > 10 * 1024 * 1024)
            {
                Debug.LogWarning($"대용량 오디오 파일: {fileInfo.Length / (1024 * 1024)}MB. 로딩 시간이 길어질 수 있습니다.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"파일 정보 확인 중 오류: {ex.Message}");
        }

        AudioClip clip = null;

        try
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, GetAudioTypeFromExtension(filePath)))
            {
                www.useHttpContinue = false;
                www.certificateHandler = null;
                www.disposeCertificateHandlerOnDispose = true;
                www.disposeDownloadHandlerOnDispose = true;

                var operation = www.SendWebRequest();

                while (!operation.isDone)
                {
                    await Task.Delay(10);
                }

                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"오디오 파일 다운로드 완료: {fileName}");

                    var startTime = Time.realtimeSinceStartup;
                    clip = DownloadHandlerAudioClip.GetContent(www);
                    clip.name = fileName;

                    Debug.Log($"오디오 클립 생성 시간: {(Time.realtimeSinceStartup - startTime) * 1000}ms");

                    if (useCache)
                    {
                        audioCache[fileName] = clip;
                    }

                    _ = Task.Run(() => SaveAudioToCustomFolder(filePath, fileName));
                }
                else
                {
                    Debug.LogError($"오디오 파일 로드 실패: {www.error}");
                    return (null, fileName);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"오디오 로드 중 예외 발생: {ex.Message}");
            return (null, fileName);
        }

        return (clip, fileName);
    }

    /// <summary>
    /// 앨범 아트 이미지를 비동기적으로 로드합니다.
    /// </summary>
    /// <param name="filePath">로드할 이미지 파일 경로</param>
    /// <param name="useCache">캐시 사용 여부</param>
    /// <returns>로드된 Sprite</returns>
    public static async Task<Sprite> LoadAlbumArtAsync(string filePath, bool useCache = true)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);

        if (useCache && imageCache.TryGetValue(fileName, out Sprite cachedSprite))
        {
            Debug.Log($"이미지 캐시에서 로드: {fileName}");
            return cachedSprite;
        }

        Debug.Log($"앨범 아트 로드 시작: {fileName}");

        Sprite albumArt = null;

        try
        {
            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture("file://" + filePath))
            {
                www.useHttpContinue = false;
                www.certificateHandler = null;
                www.disposeCertificateHandlerOnDispose = true;
                www.disposeDownloadHandlerOnDispose = true;

                var operation = www.SendWebRequest();

                while (!operation.isDone)
                {
                    await Task.Delay(10);
                }

                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"앨범 아트 다운로드 완료: {fileName}");

                    Texture2D texture = DownloadHandlerTexture.GetContent(www);
                    albumArt = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

                    if (useCache)
                    {
                        imageCache[fileName] = albumArt;
                    }

                    _ = Task.Run(() => SaveAlbumArtToCustomFolder(texture, fileName));
                }
                else
                {
                    Debug.LogError($"앨범 아트 로드 실패: {www.error}");
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"앨범 아트 로드 중 예외 발생: {ex.Message}");
            return null;
        }

        return albumArt;
    }

    /// <summary>
    /// 오디오 파일을 커스텀 폴더에 저장합니다.
    /// </summary>
    private static void SaveAudioToCustomFolder(string sourceFilePath, string clipName)
    {
        try
        {
            string customPath = Path.Combine(Application.persistentDataPath, "Tracks");

            if (!Directory.Exists(customPath))
            {
                Directory.CreateDirectory(customPath);
            }

            string extension = Path.GetExtension(sourceFilePath);
            string destinationFilePath = Path.Combine(customPath, clipName + extension);

            if (File.Exists(destinationFilePath))
            {
                Debug.Log($"파일이 이미 존재합니다: {destinationFilePath}");
                return;
            }

            File.Copy(sourceFilePath, destinationFilePath, true);

            Debug.Log($"오디오 파일이 저장되었습니다: {destinationFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"오디오 파일 복사 중 오류 발생: {e.Message}");
        }
    }

    /// <summary>
    /// 앨범 아트를 커스텀 폴더에 저장합니다.
    /// </summary>
    private static void SaveAlbumArtToCustomFolder(Texture2D texture, string trackName)
    {
        try
        {
            string albumArtPath = Path.Combine(Application.persistentDataPath, "Tracks", "AlbumArts");

            if (!Directory.Exists(albumArtPath))
            {
                Directory.CreateDirectory(albumArtPath);
            }

            string filePath = Path.Combine(albumArtPath, trackName + ".png");

            if (File.Exists(filePath))
            {
                Debug.Log($"앨범 아트가 이미 존재합니다: {filePath}");
                return;
            }

            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);

            Debug.Log($"앨범 아트가 저장되었습니다: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"앨범 아트 저장 중 오류 발생: {e.Message}");
        }
    }

    /// <summary>
    /// 파일 확장자에 따른 AudioType을 반환합니다.
    /// </summary>
    private static AudioType GetAudioTypeFromExtension(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLower();

        switch (extension)
        {
            case ".mp3":
                return AudioType.MPEG;
            case ".wav":
                return AudioType.WAV;
            case ".ogg":
                return AudioType.OGGVORBIS;
            default:
                return AudioType.UNKNOWN;
        }
    }

    /// <summary>
    /// 캐시를 정리합니다.
    /// </summary>
    public static void ClearCache()
    {
        audioCache.Clear();
        imageCache.Clear();
        Debug.Log("리소스 캐시가 정리되었습니다.");
    }
}