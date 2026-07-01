# AGENTS.md

## 프로젝트 기준
- Unity 2D Top View 생존 슈팅 프로토타입으로 작업한다.
- 범위는 이동, 자동 조준/공격, 피격, 사망, 스폰, 카메라, 최소 UI/게임오버 검증에 한정한다.
- 세트 시너지, 성장, 보상, 저장, 멀티플레이, 라이브 기능은 사용자가 명시하기 전까지 구현하지 않는다.
- 기획 기준은 `mds/기획기본구조.md`, 작업 단위는 `Assets/Tasks/*.md`, 검증 상태는 `Assets/Checklists/*_Checklist.md`를 우선 확인한다.

## 작업 실행 규칙
- 사용자가 Task 실행을 요청하면 서브에이전트로 관련 파일, 구현 범위, 위험 요소를 먼저 점검한 뒤 진행한다.
- Task 구현 후에는 반드시 대응되는 체크리스트 문서에 구현 결과를 체크하고, 필요한 경우 짧은 확인 메모를 남긴다.
- 기존 구조를 우선 유지하고, 요청 범위 밖의 리팩터링이나 에셋 변경은 피한다.

## Unity/C# 규칙
- 입력은 New Input System이 아니라 Old Input을 사용한다. `Input.GetAxisRaw`, `Input.GetKey`, `Input.GetKeyDown` 계열을 우선한다.
- 새로 만드는 C# 클래스 이름은 반드시 `JIN_` Prefix를 붙인다.
- 클래스, 메서드, 프로퍼티는 PascalCase, private 필드와 지역 변수는 camelCase를 사용한다.
- Inspector 조정값은 `[SerializeField] private` 필드로 노출하고, 프로토타입 수치는 임시값으로 둔다.
- 코드 작성 시 반드시 한국어 주석을 달되, 복잡한 의도나 Unity 설정 이유를 설명하는 핵심 주석만 남긴다.
