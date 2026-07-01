# Checklist 03. PlayerController

## 에디터 설정

- [ ] `Player`에 `Rigidbody2D`가 있다.
- [ ] `Rigidbody2D`의 Gravity Scale이 0이다.
- [ ] 필요 시 Z Rotation이 고정되어 있다.
- [ ] `PlayerController`가 `Player`에 부착되어 있다.
- [ ] 이동 속도가 Inspector에 노출된다.

## 동작 검증

- [x] 씬 `EventSystem`이 Old Input용 `StandaloneInputModule`을 사용하도록 설정되어 있다.
- [ ] `W` 또는 `Up Arrow` 입력 시 위로 이동한다.
- [ ] `S` 또는 `Down Arrow` 입력 시 아래로 이동한다.
- [ ] `A` 또는 `Left Arrow` 입력 시 왼쪽으로 이동한다.
- [ ] `D` 또는 `Right Arrow` 입력 시 오른쪽으로 이동한다.
- [ ] 대각선 입력 시 단일 축 이동보다 빠르지 않다.
- [ ] 입력이 없으면 정지한다.
- [ ] 서로 반대 방향 입력 시 해당 축 이동값이 0이 된다.
- [ ] Player 사망 또는 게임 오버 상태에서 이동이 중단된다.

## 통과 기준

- [ ] 이동 중 충돌 판정이 유지된다.
- [ ] Console에 반복 에러가 발생하지 않는다.
