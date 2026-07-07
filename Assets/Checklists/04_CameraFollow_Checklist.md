# Checklist 04. CameraFollow

## 에디터 설정

- [ ] `Main Camera`가 Orthographic 모드다.
- [ ] `CameraFollow`가 `Main Camera`에 부착되어 있다.
- [ ] 추적 대상에 `Player` Transform이 연결되어 있다.
- [ ] 카메라 Z 위치가 전투 화면을 볼 수 있는 값으로 설정되어 있다.
- [x] `PrototypeArena`에 무한 맵 배경 컴포넌트와 `Map.png` Sprite가 연결되어 있다.

## 동작 검증

- [ ] Player가 이동하면 카메라가 따라간다.
- [ ] 카메라 Z 위치가 바뀌지 않는다.
- [ ] 카메라 회전값이 바뀌지 않는다.
- [ ] 부드러운 추적 옵션 사용 시 화면 판독성이 유지된다.
- [x] Player 이동 기준으로 3x3 배경 타일이 재배치되어 맵이 이어져 보인다.
- [ ] 추적 대상이 없을 때 Console에 반복 에러가 발생하지 않는다.

## 통과 기준

- [ ] 플레이어가 화면 중심 근처에 유지된다.
- [ ] 1분 이상 이동 테스트 중 화면이 끊기거나 튀지 않는다.

## 확인 메모

- 2026-07-07: `Assets/Image/Map.webp`를 Unity용 `Map.png`로 변환하고, `JIN_InfiniteMapBackground`로 3x3 타일 무한 배경을 구성했다.
