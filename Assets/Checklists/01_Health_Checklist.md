# Checklist 01. Health

## 에디터 설정

- [ ] `Player` 오브젝트에 `Health`를 추가했다.
- [ ] `Enemy` 오브젝트에 `Health`를 추가했다.
- [ ] `maxHealth`가 Inspector에 노출된다.
- [ ] Player와 Enemy의 최대 HP를 서로 다른 값으로 설정할 수 있다.

## 동작 검증

- [ ] `TakeDamage` 호출 시 현재 HP가 감소한다.
- [ ] 현재 HP가 0 이하가 되면 생존 상태가 false가 된다.
- [ ] 사망 이벤트가 1회만 발생한다.
- [ ] 사망 이후 추가 데미지를 넣어도 중복 사망 이벤트가 발생하지 않는다.
- [ ] 음수 또는 0 데미지를 넣었을 때 비정상 동작이 없다.

## 통과 기준

- [ ] Player와 Enemy가 같은 컴포넌트로 HP와 사망 상태를 관리한다.
- [ ] Console에 반복 에러가 발생하지 않는다.
