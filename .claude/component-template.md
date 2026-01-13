### 컴포넌트 템플릿

```csharp
// 프로토타입용 표준 컴포넌트 템플릿
public class [컴포넌트명] : MonoBehaviour
{
    // ========== 인스펙터 노출 변수 ==========
    [Header("필수 참조")]
    [SerializeField] private [타입] [필수변수];
    
    [Header("설정값")]
    [SerializeField] private [타입] [설정변수] = [기본값];
    
    // ========== 내부 상태 변수 ==========
    private [타입] [내부변수];
    
    // ========== 유니티 라이프사이클 ==========
    private void Awake()
    {
        // 싱글톤 설정 (필요시)
        // 컴포넌트 초기화
    }
    
    private void Start()
    {
        // 다른 시스템과의 연결
        // 초기 상태 설정
    }
    
    private void Update()
    {
        // 프레임별 업데이트 (최소화)
    }
    
    // ========== 공개 인터페이스 ==========
    public void [주요기능메서드]()
    {
        // 핵심 기능 구현
        // 간단하고 명확한 로직
    }
    
    // ========== 내부 유틸리티 ==========
    private void [내부메서드]()
    {
        // 내부 처리 로직
    }
    
    // ========== 에러 처리 ==========
    private void OnValidate()
    {
        // 인스펙터 검증
    }
    
    private void OnDestroy()
    {
        // 리소스 정리
    }
}
```

### 디버깅 및 로깅 패턴

```csharp
// 프로토타입용 디버깅 도구
public static class PrototypeDebug
{
    private static bool debugMode = true;
    
    public static void Log(string message, Object context = null)
    {
        if (debugMode)
            Debug.Log($"[PROTOTYPE] {message}", context);
    }
    
    public static void LogWarning(string message, Object context = null)
    {
        if (debugMode)
            Debug.LogWarning($"[PROTOTYPE] {message}", context);
    }
    
    public static void LogError(string message, Object context = null)
    {
        Debug.LogError($"[PROTOTYPE] {message}", context);
    }
}
```
