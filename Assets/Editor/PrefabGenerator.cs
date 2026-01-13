using UnityEngine;
using UnityEditor;
using System.IO;

namespace Game.Editor
{
    /// <summary>
    /// 핀/슬롯 프리팹 자동 생성 도구
    /// </summary>
    public class PrefabGenerator : EditorWindow
    {
        [MenuItem("Tools/Rope Untangle/Generate Prefabs")]
        public static void ShowWindow()
        {
            GetWindow<PrefabGenerator>("Prefab Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Rope Untangle Prefab Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Generate Pin Prefab", GUILayout.Height(30)))
            {
                GeneratePinPrefab();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Generate Slot Prefab", GUILayout.Height(30)))
            {
                GenerateSlotPrefab();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Generate All Prefabs", GUILayout.Height(30)))
            {
                GeneratePinPrefab();
                GenerateSlotPrefab();
            }

            GUILayout.Space(20);
            GUILayout.Label("Generated prefabs will be saved to:", EditorStyles.miniLabel);
            GUILayout.Label("Assets/Prefabs/", EditorStyles.miniLabel);
        }

        private static void EnsurePrefabsFolderExists()
        {
            string path = "Assets/Prefabs";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
        }

        private static void GeneratePinPrefab()
        {
            EnsurePrefabsFolderExists();

            // 핀 게임오브젝트 생성
            GameObject pinObj = new GameObject("Pin");

            // 메인 바디 (구체)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Body";
            body.transform.SetParent(pinObj.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

            // Collider 제거 (별도로 추가할 것)
            DestroyImmediate(body.GetComponent<SphereCollider>());

            // 핀 헤드 (작은 구체 - 위쪽)
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(pinObj.transform);
            head.transform.localPosition = new Vector3(0, 0, -0.15f); // 카메라 방향으로
            head.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            DestroyImmediate(head.GetComponent<SphereCollider>());

            // 전체 Collider 추가
            SphereCollider collider = pinObj.AddComponent<SphereCollider>();
            collider.radius = 0.3f;
            collider.center = Vector3.zero;

            // PinController 추가
            pinObj.AddComponent<Game.PinController>();

            // 프리팹 저장
            string prefabPath = "Assets/Prefabs/Pin.prefab";
            PrefabUtility.SaveAsPrefabAsset(pinObj, prefabPath);

            // 임시 오브젝트 삭제
            DestroyImmediate(pinObj);

            Debug.Log($"Pin prefab generated: {prefabPath}");
            AssetDatabase.Refresh();
        }

        private static void GenerateSlotPrefab()
        {
            EnsurePrefabsFolderExists();

            // 슬롯 게임오브젝트 생성
            GameObject slotObj = new GameObject("Slot");

            // 베이스 (납작한 실린더)
            GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "Base";
            baseObj.transform.SetParent(slotObj.transform);
            baseObj.transform.localPosition = new Vector3(0, 0, 0.1f); // 뒤쪽으로
            baseObj.transform.localScale = new Vector3(0.7f, 0.03f, 0.7f);
            baseObj.transform.localRotation = Quaternion.Euler(90, 0, 0); // 바닥에 눕힘

            // Collider 제거
            DestroyImmediate(baseObj.GetComponent<CapsuleCollider>());

            // 링 (토러스 대신 얇은 실린더)
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Ring";
            ring.transform.SetParent(slotObj.transform);
            ring.transform.localPosition = new Vector3(0, 0, 0.05f);
            ring.transform.localScale = new Vector3(0.6f, 0.02f, 0.6f);
            ring.transform.localRotation = Quaternion.Euler(90, 0, 0);
            DestroyImmediate(ring.GetComponent<CapsuleCollider>());

            // 내부 구멍 표현 (작은 어두운 실린더)
            GameObject hole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hole.name = "Hole";
            hole.transform.SetParent(slotObj.transform);
            hole.transform.localPosition = new Vector3(0, 0, 0.08f);
            hole.transform.localScale = new Vector3(0.35f, 0.02f, 0.35f);
            hole.transform.localRotation = Quaternion.Euler(90, 0, 0);
            DestroyImmediate(hole.GetComponent<CapsuleCollider>());

            // 슬롯용 머티리얼 색상 설정
            Renderer baseRenderer = baseObj.GetComponent<Renderer>();
            Renderer ringRenderer = ring.GetComponent<Renderer>();
            Renderer holeRenderer = hole.GetComponent<Renderer>();

            // 기본 색상 설정 (런타임에서 변경 가능)
            if (baseRenderer != null)
            {
                baseRenderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                baseRenderer.sharedMaterial.color = new Color(0.4f, 0.4f, 0.4f);
            }
            if (ringRenderer != null)
            {
                ringRenderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                ringRenderer.sharedMaterial.color = new Color(0.6f, 0.6f, 0.6f);
            }
            if (holeRenderer != null)
            {
                holeRenderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                holeRenderer.sharedMaterial.color = new Color(0.2f, 0.2f, 0.2f);
            }

            // 프리팹 저장
            string prefabPath = "Assets/Prefabs/Slot.prefab";
            PrefabUtility.SaveAsPrefabAsset(slotObj, prefabPath);

            // 임시 오브젝트 삭제
            DestroyImmediate(slotObj);

            Debug.Log($"Slot prefab generated: {prefabPath}");
            AssetDatabase.Refresh();
        }
    }
}
