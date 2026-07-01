# Task 05. EnemyController

## 목표

기본 적이 플레이어 위치를 향해 계속 이동하도록 구현한다.

## 구현 대상

- 예정 스크립트: `Assets/Scripts/EnemyController.cs`
- 부착 대상: `Enemy`

## 선행 Task

- Task 01. Health
- Task 03. PlayerController

## 구현 범위

- 추적 대상 Player Transform을 참조한다.
- 매 물리 업데이트마다 플레이어 방향을 계산한다.
- `Rigidbody2D` 기반으로 플레이어를 향해 이동한다.
- 이동 속도는 Inspector에서 조정 가능하게 한다.
- 적의 `Health`가 사망 상태면 이동을 중단한다.
- 플레이어 참조가 없으면 이동하지 않고 안전하게 대기한다.

## 완료 조건

- 적이 생성되면 플레이어를 향해 접근한다.
- 플레이어가 이동하면 적의 추적 방향도 갱신된다.
- 적이 사망하면 더 이상 이동하지 않는다.

## 제외 범위

- 길찾기.
- Enemy끼리의 분리 이동.
- 복잡한 공격 패턴 또는 보스 패턴.
