# Checklist 06. ContactDamage

## 에디터 설정

- [ ] `Enemy`에 `ContactDamage`가 부착되어 있다.
- [ ] 접촉 데미지가 Inspector에 노출된다.
- [ ] 피해 적용 간격이 Inspector에 노출된다.
- [ ] Player와 Enemy의 Collider2D가 접촉을 감지할 수 있게 설정되어 있다.
- [ ] Player에 `Health`가 부착되어 있다.

## 동작 검증

- [ ] Enemy가 Player와 닿으면 Player HP가 감소한다.
- [ ] 접촉 상태에서 Player HP가 매 프레임 감소하지 않는다.
- [ ] 피해 적용 간격이 지난 뒤 추가 피해가 적용된다.
- [ ] 접촉이 끝난 뒤 다시 닿아도 피해 간격 규칙이 유지된다.
- [ ] Enemy 사망 후 접촉 데미지가 적용되지 않는다.
- [ ] Player 사망 후 추가 데미지가 적용되지 않는다.

## 통과 기준

- [ ] 접촉 피해가 명확히 체감되지만 프레임 단위로 과도하게 누적되지 않는다.
- [ ] Console에 반복 에러가 발생하지 않는다.
