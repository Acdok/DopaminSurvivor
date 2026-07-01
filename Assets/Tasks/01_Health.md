# Task 01. Health

## 목표

Player와 Enemy가 공통으로 사용할 HP 관리 컴포넌트를 구현한다.

## 구현 대상

- 예정 스크립트: `Assets/Scripts/Health.cs`
- 부착 대상: `Player`, `Enemy`

## 선행 Task

- 없음

## 구현 범위

- `maxHealth`, `currentHealth`를 관리한다.
- 최대 HP는 Inspector에서 조정 가능하게 한다.
- 데미지를 적용하는 공개 함수 `TakeDamage(float amount)`를 제공한다.
- HP가 0 이하가 되면 사망 상태로 전환한다.
- 사망 이벤트를 외부 컴포넌트가 구독할 수 있게 제공한다.
- 이미 사망한 대상은 추가 데미지나 중복 사망 처리가 발생하지 않게 막는다.
- 현재 HP, 최대 HP, 생존 여부를 읽을 수 있는 프로퍼티를 제공한다.

## 완료 조건

- Player와 Enemy가 같은 `Health` 컴포넌트를 사용할 수 있다.
- Inspector에서 최대 HP를 변경할 수 있다.
- 데미지를 받으면 현재 HP가 감소한다.
- HP가 0 이하가 되면 사망 이벤트가 한 번만 발생한다.

## 제외 범위

- 체력바 UI 표시.
- 회복 아이템, 보호막, 방어력 계산.
- 사망 애니메이션.
