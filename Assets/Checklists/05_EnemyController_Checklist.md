# Checklist 05. EnemyController

## 에디터 설정

- [ ] `Enemy`에 `Rigidbody2D`가 있다.
- [ ] `Rigidbody2D`의 Gravity Scale이 0이다.
- [ ] `EnemyController`가 `Enemy`에 부착되어 있다.
- [ ] 이동 속도가 Inspector에 노출된다.
- [x] 좌우 흔들림 회전 각도와 속도가 Inspector에 노출된다.
- [ ] 추적 대상 Player Transform이 연결되어 있다.
- [ ] `Enemy`에 `Health`가 부착되어 있다.
- [x] `EnemyPrefab`의 SpriteRenderer에 `Assets/Image/Enemy.png`가 연결되어 있다.
- [x] `EnemyPrefab`의 Collider2D가 적끼리 물리 충돌하도록 Trigger 해제되어 있다.

## 동작 검증

- [ ] Enemy가 Player 방향으로 접근한다.
- [ ] Player가 이동하면 Enemy의 이동 방향이 갱신된다.
- [x] Enemy가 이동 중 좌우로 떨리듯 회전하는 시각 흔들림이 적용되어 있다.
- [ ] Enemy가 Player와 충돌할 수 있다.
- [x] Enemy끼리 서로 겹치지 않도록 물리 충돌 판정이 적용되어 있다.
- [ ] Enemy HP가 0 이하가 되면 이동이 중단된다.
- [ ] Player 참조가 없을 때 Enemy가 비정상 이동하지 않는다.

## 통과 기준

- [ ] 적 추적만으로 기본 압박 상황을 만들 수 있다.
- [ ] Console에 반복 에러가 발생하지 않는다.

## 확인 메모

- 2026-07-07: `Assets/Image/Enemy.png`를 Sprite로 import하도록 설정하고 `EnemyPrefab`에 적용했다.
- 2026-07-07: 다시 교체된 `Enemy.png`의 import 설정을 Sprite/Single/투명 알파 사용으로 재적용했다.
- 2026-07-07: `EnemyPrefab` 크기를 2배로 키우고 Collider2D Trigger를 해제해 Enemy끼리 충돌하게 했다.
- 2026-07-07: `EnemyController`에 이동 중 좌우 회전 흔들림을 추가하고 `EnemyPrefab` 기본값을 각도 8, 속도 6으로 설정했다.
