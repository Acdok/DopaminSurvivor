# Task 04. CameraFollow

## 목표

Orthographic Camera가 플레이어를 따라가도록 구현한다.

## 구현 대상

- 예정 스크립트: `Assets/Scripts/CameraFollow.cs`
- 부착 대상: `Main Camera`

## 선행 Task

- Task 03. PlayerController

## 구현 범위

- 따라갈 대상 Transform을 Inspector에서 지정한다.
- 플레이어의 X/Y 위치를 카메라가 추적한다.
- 카메라 Z 위치는 고정한다.
- 즉시 추적 또는 부드러운 추적 옵션을 제공한다.
- 추적 속도는 Inspector에서 조정 가능하게 한다.
- 전투 판독성을 해치지 않도록 카메라 회전은 변경하지 않는다.

## 완료 조건

- 플레이어가 이동하면 카메라가 따라간다.
- 카메라의 Z축 값과 Orthographic 설정이 유지된다.
- 추적 대상이 비어 있을 때 에러가 반복 발생하지 않는다.

## 제외 범위

- 카메라 흔들림.
- 맵 경계 클램프.
- 줌 인/아웃 연출.
