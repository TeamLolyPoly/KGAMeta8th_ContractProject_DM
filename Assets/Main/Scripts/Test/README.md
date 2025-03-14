# 노트 시스템 테스트 가이드

## 테스트 설정 방법

1. 새 씬을 생성하거나 기존 게임 씬을 사용합니다.
2. 씬에 다음 컴포넌트들이 있는지 확인합니다:

   - GridGenerator
   - NoteSpawner
   - GameManager (ScoreSystem 참조 필요)

3. 새로 생성한 `NoteMapTester` 스크립트를 빈 게임 오브젝트에 추가합니다.
4. Inspector에서 다음 항목을 설정합니다:
   - Note Map Json File: "TestNoteMap" (기본값)
   - Grid Generator: 씬의 GridGenerator 컴포넌트 참조
   - Start Delay: 노트 생성 시작 전 대기 시간 (초)

## 테스트 JSON 데이터 설명

생성된 `TestNoteMap.json` 파일에는 다양한 노트 타입과 방향을 포함한 테스트 데이터가 있습니다:

### 숏 노트 (Short Notes)

- 다양한 방향과 위치에 배치된 8개의 숏 노트
- 왼쪽/오른쪽 그리드에 번갈아 배치
- 다양한 NoteDirection 값 테스트
- 다양한 NoteAxis 값 테스트

### 롱 노트 (Long Notes)

- 다양한 설정의 롱 노트 3개
- 시계 방향/반시계 방향 회전
- 대칭/비대칭 패턴
- 다양한 지속 시간 설정:
  - 2박자 지속 (durationBars: 0, durationBeats: 2)
  - 4박자 지속 (durationBars: 0, durationBeats: 4)
  - 1마디 지속 (durationBars: 1, durationBeats: 0)

## 노트 타입 설명

### baseType

- 1: Short (숏 노트)
- 2: Long (롱 노트)

### noteType

- 1: Red (빨간색 노트)
- 2: Blue (파란색 노트)
- 3: Hand (손 노트)

### direction

- 1: East (동쪽)
- 2: West (서쪽)
- 3: South (남쪽)
- 4: North (북쪽)
- 5: Northeast (북동쪽)
- 6: Northwest (북서쪽)
- 7: Southeast (남동쪽)
- 8: Southwest (남서쪽)

### noteAxis

- 1: PZ (양의 Z축)
- 2: MZ (음의 Z축)
- 3: PX (양의 X축)
- 4: MX (음의 X축)

## 롱노트 지속 시간 설정

롱노트의 지속 시간은 마디와 박자 정보를 사용하여 설정합니다:

- `durationBars`: 롱노트가 지속되는 마디 수
- `durationBeats`: 롱노트가 지속되는 박자 수
- 예: durationBars=1, durationBeats=2는 1마디 2박자 동안 지속됨을 의미

시스템은 마디와 박자 정보를 기반으로 롱노트의 길이를 자동 계산합니다:

- 총 지속 시간(초) = (durationBars _ beatsPerBar + durationBeats) _ (60 / BPM)
- 세그먼트 수 = 총 지속 시간(초) / segmentSpawnInterval

## 문제 해결

1. "JSON 파일을 찾을 수 없습니다" 오류가 발생하면:

   - `TestNoteMap.json` 파일이 `Assets/Main/Resources/` 폴더에 있는지 확인하세요.
   - 파일 이름이 정확한지 확인하세요.

2. 노트가 생성되지 않으면:

   - 콘솔 로그에서 오류 메시지를 확인하세요.
   - GridGenerator와 NoteSpawner가 올바르게 초기화되었는지 확인하세요.
   - GameManager와 ScoreSystem이 씬에 존재하는지 확인하세요.

3. 노트가 잘못된 위치에 생성되면:

   - GridGenerator의 설정을 확인하세요.
   - JSON 데이터의 StartCell과 TargetCell 값을 확인하세요.

4. 롱노트 길이가 예상과 다르면:
   - durationBars와 durationBeats 값을 확인하세요.
   - BPM과 beatsPerBar 설정이 올바른지 확인하세요.
   - NoteSpawner의 segmentSpawnInterval 값을 확인하세요.
