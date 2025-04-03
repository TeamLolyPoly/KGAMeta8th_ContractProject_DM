using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace NoteEditor
{
    /// <summary>
    /// 오디오 파일 및 관련 리소스의 로드/저장을 담당하는 서비스 클래스
    /// </summary>
    public static class ResourceIO
    {
        private static Dictionary<string, AudioClip> audioCache =
            new Dictionary<string, AudioClip>();
        private static Dictionary<string, Sprite> imageCache = new Dictionary<string, Sprite>();

        #region 오디오 파일 관련 메서드

        /// <summary>
        /// 트랙 이름으로 오디오 파일을 로드합니다.
        /// </summary>
        /// <param name="trackName">트랙 이름</param>
        /// <param name="progress">진행 상황 보고 인터페이스</param>
        /// <returns>로드된 AudioClip</returns>
        public static async Task<AudioClip> LoadAudioAsync(
            TrackData trackData,
            IProgress<float> progress = null
        )
        {
            if (audioCache.TryGetValue(trackData.id.ToString(), out AudioClip cachedClip))
            {
                Debug.Log($"오디오 캐시에서 로드: {trackData.trackName}");
                progress?.Report(1.0f);
                return cachedClip;
            }

            string filePath = ResourcePath.GetAudioFilePath(trackData.id);
            if (!File.Exists(filePath))
            {
                Debug.LogError($"오디오 파일을 찾을 수 없음: {filePath}");
                return null;
            }

            progress?.Report(0.1f);

            try
            {
                using (
                    UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(
                        "file://" + filePath,
                        GetAudioTypeFromExtension(filePath)
                    )
                )
                {
                    www.useHttpContinue = false;
                    www.certificateHandler = null;
                    www.disposeCertificateHandlerOnDispose = true;
                    www.disposeDownloadHandlerOnDispose = true;

                    var operation = www.SendWebRequest();

                    while (!operation.isDone)
                    {
                        progress?.Report(0.1f + 0.8f * www.downloadProgress);
                        await Task.Delay(10);
                    }

                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                        clip.name = trackData.trackName;

                        audioCache[trackData.id.ToString()] = clip;

                        progress?.Report(1.0f);
                        return clip;
                    }
                    else
                    {
                        Debug.LogError($"오디오 파일 로드 실패: {www.error}");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"오디오 로드 중 예외 발생: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 외부 오디오 파일을 로드하고 저장합니다.
        /// </summary>
        /// <param name="filePath">외부 파일 경로</param>
        /// <param name="progress">진행 상황 보고 인터페이스</param>
        /// <returns>로드된 AudioClip과 트랙 이름</returns>
        public static async Task<(AudioClip clip, TrackData trackData)> ImportAudioFileAsync(
            string filePath,
            IProgress<float> progress = null
        )
        {
            string trackName = Path.GetFileNameWithoutExtension(filePath);

            EditorLoadingManager.Instance.SetLoadingText($"오디오 경로 로드 중... {trackName}");
            progress?.Report(0.1f);

            try
            {
                using (
                    UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(
                        "file://" + filePath,
                        GetAudioTypeFromExtension(filePath)
                    )
                )
                {
                    www.useHttpContinue = false;
                    www.certificateHandler = null;
                    www.disposeCertificateHandlerOnDispose = true;
                    www.disposeDownloadHandlerOnDispose = true;

                    var operation = www.SendWebRequest();

                    EditorLoadingManager.Instance.SetLoadingText($"오디오 파일 로드 중...");
                    while (!operation.isDone)
                    {
                        progress?.Report(0.1f + 0.6f * www.downloadProgress);
                        await Task.Delay(10);
                    }

                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        EditorLoadingManager.Instance.SetLoadingText($"오디오 파일 저장 중... ");
                        AudioClip clip = DownloadHandlerAudioClip.GetContent(www);

                        TrackData trackData = new TrackData
                        {
                            id = Guid.NewGuid(),
                            trackName = trackName,
                        };

                        clip.name = trackData.id.ToString();

                        progress?.Report(0.7f);

                        await SaveAudioAsync(clip, trackData);

                        audioCache[trackData.id.ToString()] = clip;

                        progress?.Report(1.0f);
                        return (clip, trackData);
                    }
                    else
                    {
                        Debug.LogError($"오디오 파일 로드 실패: {www.error}");
                        return (null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"오디오 로드 중 예외 발생: {ex.Message}");
                return (null, null);
            }
        }

        /// <summary>
        /// 오디오 파일을 저장합니다.
        /// </summary>
        /// <param name="clip">저장할 AudioClip</param>
        /// <param name="trackName">트랙 이름</param>
        /// <param name="onMainThreadProcess">메인 스레드에서 실행할 텍스처 처리 콜백</param>
        /// <returns>비동기 작업</returns>
        public static async Task SaveAudioAsync(AudioClip clip, TrackData trackData)
        {
            if (clip == null)
                return;

            string filePath = ResourcePath.GetAudioFilePath(trackData.id);

            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            int frequency = clip.frequency;
            int channels = clip.channels;
            int sampleCount = clip.samples;

            short[] intData = new short[samples.Length];
            byte[] bytesData = new byte[samples.Length * 2];

            int rescaleFactor = 32767;
            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short)(samples[i] * rescaleFactor);
                byte[] byteArr = BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }

            await Task.Run(() =>
            {
                try
                {
                    ResourcePath.EnsureDirectoriesExist();

                    using (FileStream fileStream = CreateEmptyWav(filePath))
                    {
                        WriteWavHeader(fileStream, frequency, channels, sampleCount);
                        fileStream.Write(bytesData, 0, bytesData.Length);
                    }

                    Debug.Log($"오디오 파일 저장됨: {filePath}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"오디오 파일 저장 중 오류 발생: {ex.Message}");
                }
            });
        }

        #endregion

        #region 앨범 아트 관련 메서드

        /// <summary>
        /// 트랙 이름으로 앨범 아트를 로드합니다.
        /// </summary>
        /// <param name="trackName">트랙 이름</param>
        /// <returns>로드된 Sprite</returns>
        public static IEnumerator<Sprite> LoadAlbumArtAsync(TrackData trackData)
        {
            if (imageCache.TryGetValue(trackData.id.ToString(), out Sprite cachedSprite))
            {
                Debug.Log($"이미지 캐시에서 로드: {trackData.trackName}");
                yield return cachedSprite;
            }

            string filePath = ResourcePath.GetAlbumArtPath(trackData.id);
            if (!File.Exists(filePath))
            {
                Debug.Log($"앨범 아트 파일을 찾을 수 없음: {filePath}");
                yield break;
            }

            UnityWebRequest www = UnityWebRequestTexture.GetTexture("file://" + filePath);

            www.useHttpContinue = false;
            www.certificateHandler = null;
            www.disposeCertificateHandlerOnDispose = true;
            www.disposeDownloadHandlerOnDispose = true;

            var operation = www.SendWebRequest();

            while (!operation.isDone)
            {
                yield return null;
            }

            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );

                imageCache[trackData.id.ToString()] = sprite;

                yield return sprite;
            }
            else
            {
                Debug.LogError($"앨범 아트 로드 실패: {www.error}");
            }
        }

        /// <summary>
        /// 외부 이미지 파일을 앨범 아트로 로드하고 저장합니다.
        /// </summary>
        /// <param name="filePath">외부 이미지 파일 경로</param>
        /// <param name="trackData">트랙 데이터</param>
        /// <param name="progress">진행 상황 보고 인터페이스</param>
        /// <returns>로드된 Sprite</returns>
        public static async Task<Sprite> ImportAlbumArtAsync(
            string filePath,
            TrackData trackData,
            IProgress<float> progress = null
        )
        {
            progress?.Report(0.1f);

            try
            {
                using (
                    UnityWebRequest www = UnityWebRequestTexture.GetTexture("file://" + filePath)
                )
                {
                    www.useHttpContinue = false;
                    www.certificateHandler = null;
                    www.disposeCertificateHandlerOnDispose = true;
                    www.disposeDownloadHandlerOnDispose = true;

                    var operation = www.SendWebRequest();

                    while (!operation.isDone)
                    {
                        progress?.Report(0.1f + 0.6f * www.downloadProgress);
                        await Task.Delay(10);
                    }

                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        Texture2D texture = DownloadHandlerTexture.GetContent(www);
                        Sprite sprite = Sprite.Create(
                            texture,
                            new Rect(0, 0, texture.width, texture.height),
                            new Vector2(0.5f, 0.5f)
                        );

                        progress?.Report(0.7f);

                        await SaveAlbumArtAsync(sprite, trackData);

                        imageCache[trackData.id.ToString()] = sprite;

                        progress?.Report(1.0f);
                        return sprite;
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
        }

        /// <summary>
        /// 앨범 아트를 저장합니다.
        /// </summary>
        /// <param name="albumArt">저장할 Sprite</param>
        /// <param name="trackName">트랙 이름</param>
        /// <param name="onMainThreadProcess">메인 스레드에서 실행할 텍스처 처리 콜백</param>
        /// <returns>비동기 작업</returns>
        public static async Task SaveAlbumArtAsync(Sprite albumArt, TrackData trackData)
        {
            if (albumArt == null)
                return;

            string filePath = ResourcePath.GetAlbumArtPath(trackData.id);

            byte[] bytes = albumArt.texture.EncodeToPNG();

            await Task.Run(() =>
            {
                try
                {
                    ResourcePath.EnsureDirectoriesExist();
                    File.WriteAllBytes(filePath, bytes);
                    Debug.Log($"앨범 아트 저장됨: {filePath}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"앨범 아트 저장 중 오류 발생: {ex.Message}");
                }
            });
        }

        #endregion

        #region 메타데이터 관련 메서드

        /// <summary>
        /// 트랙 메타데이터를 로드합니다.
        /// </summary>
        /// <returns>트랙 메타데이터 리스트</returns>
        public static async Task<List<TrackData>> LoadMetadataAsync()
        {
            string filePath = ResourcePath.TRACK_METADATA_PATH;

            if (!File.Exists(filePath))
            {
                Debug.Log("메타데이터 파일이 없습니다.");
                return new List<TrackData>();
            }

            try
            {
                string json = await Task.Run(() => File.ReadAllText(filePath));

                return JsonConvert.DeserializeObject<List<TrackData>>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"메타데이터 로드 중 오류 발생: {ex.Message}");
                return new List<TrackData>();
            }
        }

        /// <summary>
        /// 트랙 메타데이터를 저장합니다.
        /// </summary>
        /// <param name="metadata">저장할 메타데이터 리스트</param>
        /// <returns>비동기 작업</returns>
        public static async Task SaveMetadataAsync(List<TrackData> metadata)
        {
            string filePath = ResourcePath.TRACK_METADATA_PATH;

            try
            {
                ResourcePath.EnsureDirectoriesExist();

                string json = JsonConvert.SerializeObject(metadata);
                await Task.Run(() => File.WriteAllText(filePath, json));

                Debug.Log("트랙 메타데이터가 저장되었습니다.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"메타데이터 저장 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 트랙 파일을 삭제합니다.
        /// </summary>
        /// <param name="guid">삭제할 트랙 고유 식별자</param>
        /// <returns>비동기 작업</returns>
        public static async Task DeleteTrackFilesAsync(Guid guid)
        {
            await Task.Run(() =>
            {
                try
                {
                    string audioFilePath = ResourcePath.GetAudioFilePath(guid);
                    if (File.Exists(audioFilePath))
                    {
                        File.Delete(audioFilePath);
                        Debug.Log($"트랙 파일 삭제됨: {audioFilePath}");
                    }

                    string albumArtPath = ResourcePath.GetAlbumArtPath(guid);
                    if (File.Exists(albumArtPath))
                    {
                        File.Delete(albumArtPath);
                        Debug.Log($"앨범 아트 파일 삭제됨: {albumArtPath}");
                    }

                    string noteMapPath = ResourcePath.GetNoteMapPath(guid);
                    if (File.Exists(noteMapPath))
                    {
                        File.Delete(noteMapPath);
                        Debug.Log($"노트맵 파일 삭제됨: {noteMapPath}");
                    }

                    audioCache.Remove(guid.ToString());
                    imageCache.Remove(guid.ToString());
                }
                catch (Exception ex)
                {
                    Debug.LogError($"트랙 파일 삭제 중 오류 발생: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 모든 트랙 파일을 삭제합니다.
        /// </summary>
        /// <returns>비동기 작업</returns>
        public static async Task DeleteAllTrackFilesAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    if (Directory.Exists(ResourcePath.TRACK_AUDIO_PATH))
                    {
                        string[] audioFiles = Directory.GetFiles(
                            ResourcePath.TRACK_AUDIO_PATH,
                            "*.wav"
                        );
                        foreach (string file in audioFiles)
                        {
                            File.Delete(file);
                        }
                        Debug.Log("모든 오디오 파일이 삭제되었습니다.");
                    }

                    if (Directory.Exists(ResourcePath.ALBUM_ART_PATH))
                    {
                        string[] artFiles = Directory.GetFiles(
                            ResourcePath.ALBUM_ART_PATH,
                            "*.png"
                        );
                        foreach (string file in artFiles)
                        {
                            File.Delete(file);
                        }
                        Debug.Log("모든 앨범 아트 파일이 삭제되었습니다.");
                    }

                    if (File.Exists(ResourcePath.TRACK_METADATA_PATH))
                    {
                        File.Delete(ResourcePath.TRACK_METADATA_PATH);
                        Debug.Log("메타데이터 파일이 삭제되었습니다.");
                    }

                    ClearCache();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"모든 트랙 파일 삭제 중 오류 발생: {ex.Message}");
                }
            });
        }

        #endregion

        #region 유틸리티 메서드

        /// <summary>
        /// 캐시를 초기화합니다.
        /// </summary>
        public static void ClearCache()
        {
            audioCache.Clear();
            imageCache.Clear();
            Debug.Log("오디오 및 이미지 캐시가 초기화되었습니다.");
        }

        /// <summary>
        /// 파일 확장자로부터 오디오 타입을 가져옵니다.
        /// </summary>
        /// <param name="filePath">파일 경로</param>
        /// <returns>오디오 타입</returns>
        private static AudioType GetAudioTypeFromExtension(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();

            switch (extension)
            {
                case ".mp3":
                    return AudioType.MPEG;
                case ".ogg":
                    return AudioType.OGGVORBIS;
                case ".wav":
                    return AudioType.WAV;
                case ".aiff":
                case ".aif":
                    return AudioType.AIFF;
                default:
                    Debug.LogWarning($"지원되지 않는 오디오 형식: {extension}, WAV로 처리합니다.");
                    return AudioType.WAV;
            }
        }

        /// <summary>
        /// WAV 파일 생성을 위한 헬퍼 메서드
        /// </summary>
        /// <param name="filepath">파일 경로</param>
        /// <returns>FileStream</returns>
        private static FileStream CreateEmptyWav(string filepath)
        {
            string directoryPath = Path.GetDirectoryName(filepath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            FileStream fileStream = new FileStream(filepath, FileMode.Create);
            byte emptyByte = new byte();

            for (int i = 0; i < 44; i++) // WAV 헤더 크기
            {
                fileStream.WriteByte(emptyByte);
            }

            return fileStream;
        }

        /// <summary>
        /// WAV 헤더 작성 메서드 - AudioClip 대신 필요한 값들을 직접 전달받음
        /// </summary>
        /// <param name="fileStream">파일 스트림</param>
        /// <param name="frequency">주파수</param>
        /// <param name="channels">채널</param>
        /// <param name="samples">샘플</param>
        private static void WriteWavHeader(
            FileStream fileStream,
            int frequency,
            int channels,
            int samples
        )
        {
            fileStream.Seek(0, SeekOrigin.Begin);

            byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
            fileStream.Write(riff, 0, 4);

            byte[] chunkSize = BitConverter.GetBytes(samples * channels * 2 + 36);
            fileStream.Write(chunkSize, 0, 4);

            byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
            fileStream.Write(wave, 0, 4);

            byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
            fileStream.Write(fmt, 0, 4);

            byte[] subChunk1 = BitConverter.GetBytes(16);
            fileStream.Write(subChunk1, 0, 4);

            ushort audioFormat = 1;
            byte[] audioFormatBytes = BitConverter.GetBytes(audioFormat);
            fileStream.Write(audioFormatBytes, 0, 2);

            ushort numChannels = (ushort)channels;
            byte[] numChannelsBytes = BitConverter.GetBytes(numChannels);
            fileStream.Write(numChannelsBytes, 0, 2);

            byte[] sampleRate = BitConverter.GetBytes(frequency);
            fileStream.Write(sampleRate, 0, 4);

            byte[] byteRate = BitConverter.GetBytes(frequency * channels * 2);
            fileStream.Write(byteRate, 0, 4);

            ushort blockAlign = (ushort)(channels * 2);
            byte[] blockAlignBytes = BitConverter.GetBytes(blockAlign);
            fileStream.Write(blockAlignBytes, 0, 2);

            ushort bitsPerSample = 16;
            byte[] bitsPerSampleBytes = BitConverter.GetBytes(bitsPerSample);
            fileStream.Write(bitsPerSampleBytes, 0, 2);

            byte[] dataString = System.Text.Encoding.UTF8.GetBytes("data");
            fileStream.Write(dataString, 0, 4);

            byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
            fileStream.Write(subChunk2, 0, 4);
        }

        #endregion
    }

    /// <summary>
    /// 오디오 관련 파일 경로를 관리하는 유틸리티 클래스
    /// </summary>
    public static class ResourcePath
    {
        private static readonly string BASE_PATH = Application.persistentDataPath;
        private static readonly string TRACK_DATA_PATH = Path.Combine(BASE_PATH, "Data");

        public static readonly string ALBUM_ART_PATH = Path.Combine(BASE_PATH, "AlbumArts");
        public static readonly string TRACK_METADATA_PATH = Path.Combine(
            BASE_PATH,
            "Data/TrackData.json"
        );
        public static readonly string NOTEMAP_PATH = Path.Combine(BASE_PATH, "NoteMaps");
        public static readonly string TRACK_AUDIO_PATH = Path.Combine(BASE_PATH, "Audio");

        /// <summary>
        /// 트랙 이름으로 오디오 파일 경로를 가져옵니다.
        /// </summary>
        /// <param name="guid">고유 식별자</param>
        /// <returns>오디오 파일 경로</returns>
        public static string GetAudioFilePath(Guid guid)
        {
            return Path.Combine(TRACK_AUDIO_PATH, $"{guid}.wav");
        }

        /// <summary>
        /// 트랙 이름으로 앨범 아트 파일 경로를 가져옵니다.
        /// </summary>
        /// <param name="guid">고유 식별자</param>
        /// <returns>앨범 아트 파일 경로</returns>
        public static string GetAlbumArtPath(Guid guid)
        {
            return Path.Combine(ALBUM_ART_PATH, $"{guid}.png");
        }

        /// <summary>
        /// 트랙 이름으로 노트맵 파일 경로를 가져옵니다.
        /// </summary>
        /// <param name="guid">고유 식별자</param>
        /// <returns>노트맵 파일 경로</returns>
        public static string GetNoteMapPath(Guid guid)
        {
            return Path.Combine(NOTEMAP_PATH, $"{guid}.json");
        }

        /// <summary>
        /// 필요한 디렉토리가 존재하는지 확인하고, 없으면 생성합니다.
        /// </summary>
        public static void EnsureDirectoriesExist()
        {
            if (!Directory.Exists(BASE_PATH))
                Directory.CreateDirectory(BASE_PATH);

            if (!Directory.Exists(TRACK_DATA_PATH))
                Directory.CreateDirectory(TRACK_DATA_PATH);

            if (!Directory.Exists(ALBUM_ART_PATH))
                Directory.CreateDirectory(ALBUM_ART_PATH);

            if (!Directory.Exists(NOTEMAP_PATH))
                Directory.CreateDirectory(NOTEMAP_PATH);
        }
    }

    public static class WavUtility
    {
        public static AudioClip ToAudioClip(byte[] wavData, string name)
        {
            using (MemoryStream stream = new MemoryStream(wavData))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                if (new string(reader.ReadChars(4)) != "RIFF")
                {
                    Debug.LogError("WAV 파일이 아닙니다.");
                    return null;
                }

                reader.ReadInt32();
                if (new string(reader.ReadChars(4)) != "WAVE")
                {
                    Debug.LogError("WAV 포맷이 아닙니다.");
                    return null;
                }

                while (new string(reader.ReadChars(4)) != "fmt ")
                {
                    int chunkSize = reader.ReadInt32();
                    reader.BaseStream.Position += chunkSize;
                }

                reader.ReadInt32();
                int audioFormat = reader.ReadInt16();
                int numChannels = reader.ReadInt16();
                int sampleRate = reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadInt16();
                int bitsPerSample = reader.ReadInt16();

                while (new string(reader.ReadChars(4)) != "data")
                {
                    int chunkSize = reader.ReadInt32();
                    reader.BaseStream.Position += chunkSize;
                }

                int dataSize = reader.ReadInt32();
                byte[] data = reader.ReadBytes(dataSize);

                float[] samples = ConvertWavToFloat(data, bitsPerSample);
                AudioClip audioClip = AudioClip.Create(
                    name,
                    samples.Length,
                    numChannels,
                    sampleRate,
                    false
                );
                audioClip.SetData(samples, 0);
                return audioClip;
            }
        }

        private static float[] ConvertWavToFloat(byte[] wavData, int bitsPerSample)
        {
            int bytesPerSample = bitsPerSample / 8;
            int sampleCount = wavData.Length / bytesPerSample;
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                if (bitsPerSample == 16)
                {
                    short sample = BitConverter.ToInt16(wavData, i * 2);
                    samples[i] = sample / 32768f;
                }
                else if (bitsPerSample == 8)
                {
                    samples[i] = (wavData[i] - 128) / 128f;
                }
            }

            return samples;
        }
    }
}
