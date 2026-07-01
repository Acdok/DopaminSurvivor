# Checklist 05. EnemyController

## 에디터 설정

- [ ] `Enemy`에 `Rigidbody2D`가 있다.
- [ ] `Rigidbody2D`의 Gravity Scale이 0이다.
- [ ] `EnemyController`가 `Enemy`에 부착되어 있다.
- [ ] 이동 속도가 Inspector에 노출된다.
- [ ] 추적 대상 Player Transform이 연결되어 있다.
- [ ] `Enemy`에 `Health`가 부착되어 있다.

## 동작 검증

- [ ] Enemy가 Player 방향으로 접근한다.
- [ ] Player가 이동하면 Enemy의 이동 방향이 갱신된다.
- [ ] Enemy가 Player와 충돌할 수 있다.
- [ ] Enemy HP가 0 이하가 되면 이동이 중단된다.
- [ ] Player 참조가 없을 때 Enemy가 비정상 이동하지 않는다.

## 통과 기준

- [ ] 적 추적만으로 기본 압박 상황을 만들 수 있다.
- [ ] Console에 반복 에러가 발생하지 않는다.
