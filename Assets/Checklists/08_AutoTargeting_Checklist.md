# Checklist 08. AutoTargeting

## 에디터 설정

- [ ] `Player`에 `AutoTargeting`이 부착되어 있다.
- [ ] Enemy 오브젝트를 식별할 방식이 설정되어 있다.
- [ ] Enemy에 `Health`가 부착되어 있다.
- [ ] Enemy의 활성/비활성 상태를 테스트할 수 있다.

## 동작 검증

- [ ] Enemy가 하나 있을 때 해당 Enemy를 반환한다.
- [ ] Enemy가 여러 명 있을 때 가장 가까운 Enemy를 반환한다.
- [ ] 가장 가까운 Enemy가 사망하면 다음 유효 Enemy를 반환한다.
- [ ] Enemy가 없으면 타겟 없음 상태를 반환한다.
- [ ] 비활성화된 Enemy는 타겟으로 선택하지 않는다.

## 통과 기준

- [ ] WeaponController가 사용할 수 있는 안정적인 타겟 조회 결과를 제공한다.
- [ ] Console에 반복 에러가 발생하지 않는다.
