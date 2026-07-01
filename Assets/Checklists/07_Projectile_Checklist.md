# Checklist 07. Projectile

## 에디터 설정

- [x] `ProjectilePrefab`에 `Projectile`이 부착되어 있다.
- [x] `ProjectilePrefab`에 `Rigidbody2D`가 있다.
- [x] `ProjectilePrefab`에 충돌 감지용 `Collider2D`가 있다.
- [x] 속도, 데미지, 수명이 설정되어 있다.
- [x] Projectile이 Player에게 피해를 주지 않도록 Layer 또는 충돌 조건이 설정되어 있다.
- [x] 관통 횟수와 유도 반경/회전 속도를 설정할 수 있다.

## 동작 검증

- [x] 발사체가 지정 방향으로 출발한 뒤 유도 설정에 따라 목표 방향으로 보정된다.
- [ ] 발사체가 Enemy와 충돌하면 Enemy HP가 감소한다.
- [x] 관통 설정이 켜진 발사체는 명중 후에도 최대 관통 수까지 유지된다.
- [ ] 하나의 발사체가 같은 적에게 중복 데미지를 주지 않는다.
- [ ] 적에게 닿지 않은 발사체는 수명 종료 시 제거된다.
- [ ] 발사체가 Player와 충돌해도 Player HP가 감소하지 않는다.

## 통과 기준

- [ ] 발사체 이동과 명중 처리가 기본 공격 검증에 충분히 안정적이다.
- [ ] Console에 반복 에러가 발생하지 않는다.

## 확인 메모

- 2026-06-29: 기본 공격은 충전형 `JIN_BrimstoneLaser`로 이전되어 `ProjectilePrefab`에 의존하지 않는다. `Projectile`은 다른 무기/확장 시스템용으로 유지한다.
- 2026-06-30: 유도 투사체 기본 회전 속도와 재탐색 간격을 완만한 곡선 이동에 맞춰 조정했다.
- 2026-06-30: `WeaponController` 기본 공격이 다시 `ProjectilePrefab`을 사용하도록 복구됐다.
- 2026-06-30: 프리팹 파일 기준으로 `Projectile`, `Rigidbody2D`, Trigger `Collider2D`, 속도/데미지/수명, Player 제외 필터가 설정되어 있음을 확인했다.
- 2026-06-30: 범용 유도 공격 옵션이 켜진 경우 `WeaponController`가 `Projectile.ConfigureHoming`을 호출해 일반 투사체에도 유도를 적용한다.
- 2026-06-30: `Projectile`에 적중 시 약한 분열 투사체를 생성하는 설정 API를 추가했다.
