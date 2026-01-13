using System.Collections.Generic;
using UnityEngine;

namespace Game.Rendering
{
    /// <summary>
    /// 튜브 메시 생성기
    /// 3D 경로를 따라 원통형 메시를 생성합니다.
    /// </summary>
    public class TubeMeshGenerator
    {
        // ========== 설정 ==========
        public float TubeRadius { get; set; } = 0.1f;
        public int RadialSegments { get; set; } = 8;
        public float UVTileScale { get; set; } = 1f;

        // ========== 캐시 ==========
        private List<Vector3> _vertices = new List<Vector3>();
        private List<int> _triangles = new List<int>();
        private List<Vector2> _uvs = new List<Vector2>();
        private List<Vector3> _normals = new List<Vector3>();

        /// <summary>
        /// 경로로부터 튜브 메시 생성
        /// </summary>
        /// <param name="path">로프 중심선 경로 (최소 2개 포인트)</param>
        /// <returns>생성된 Mesh (경로가 부족하면 null)</returns>
        public Mesh GenerateMesh(List<Vector3> path)
        {
            if (path == null || path.Count < 2)
            {
                return null;
            }

            // 캐시 클리어
            _vertices.Clear();
            _triangles.Clear();
            _uvs.Clear();
            _normals.Clear();

            // 경로 총 길이 계산
            float totalLength = CalculateTotalLength(path);
            float currentLength = 0f;

            // 각 경로 포인트에 대해 단면 생성
            for (int i = 0; i < path.Count; i++)
            {
                // 방향 벡터 계산
                Vector3 forward = GetForwardVector(path, i);
                Vector3 up = CalculateUp(forward);
                Vector3 right = Vector3.Cross(forward, up).normalized;

                // 원형 단면 생성
                for (int j = 0; j < RadialSegments; j++)
                {
                    float angle = (j / (float)RadialSegments) * Mathf.PI * 2f;

                    // 단면 위치 계산
                    Vector3 offset = (Mathf.Cos(angle) * right + Mathf.Sin(angle) * up) * TubeRadius;
                    _vertices.Add(path[i] + offset);

                    // UV 계산 (U: 둘레, V: 경로 길이 기반)
                    float u = j / (float)RadialSegments;
                    float v = (currentLength / totalLength) * UVTileScale * totalLength;
                    _uvs.Add(new Vector2(u, v));

                    // Normal 계산 (외부 방향)
                    _normals.Add(offset.normalized);
                }

                // 다음 포인트까지 거리 누적
                if (i < path.Count - 1)
                {
                    currentLength += Vector3.Distance(path[i], path[i + 1]);
                }
            }

            // Triangle 연결
            GenerateTriangles(path.Count);

            // Mesh 생성
            Mesh mesh = new Mesh();
            mesh.name = "TubeMesh";
            mesh.vertices = _vertices.ToArray();
            mesh.triangles = _triangles.ToArray();
            mesh.uv = _uvs.ToArray();
            mesh.normals = _normals.ToArray();

            // Bounds 재계산
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// 캡이 있는 튜브 메시 생성
        /// </summary>
        public Mesh GenerateMeshWithCaps(List<Vector3> path)
        {
            Mesh tubeMesh = GenerateMesh(path);
            if (tubeMesh == null) return null;

            // TODO: 양 끝에 캡 추가 (필요 시)
            // 현재는 캡 없이 반환

            return tubeMesh;
        }

        // ========== 내부 유틸리티 ==========

        /// <summary>
        /// 경로의 총 길이 계산
        /// </summary>
        private float CalculateTotalLength(List<Vector3> path)
        {
            float length = 0f;
            for (int i = 0; i < path.Count - 1; i++)
            {
                length += Vector3.Distance(path[i], path[i + 1]);
            }
            return Mathf.Max(length, 0.001f); // 0 방지
        }

        /// <summary>
        /// 특정 인덱스에서의 전방 벡터 계산
        /// </summary>
        private Vector3 GetForwardVector(List<Vector3> path, int index)
        {
            Vector3 forward;

            if (index == 0)
            {
                // 첫 번째 점: 다음 점 방향
                forward = (path[1] - path[0]).normalized;
            }
            else if (index == path.Count - 1)
            {
                // 마지막 점: 이전 점에서 오는 방향
                forward = (path[index] - path[index - 1]).normalized;
            }
            else
            {
                // 중간 점: 양쪽 평균
                Vector3 toNext = (path[index + 1] - path[index]).normalized;
                Vector3 fromPrev = (path[index] - path[index - 1]).normalized;
                forward = ((toNext + fromPrev) * 0.5f).normalized;
            }

            // 길이가 0이면 기본값
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }

            return forward;
        }

        /// <summary>
        /// 전방 벡터에 수직인 Up 벡터 계산
        /// </summary>
        private Vector3 CalculateUp(Vector3 forward)
        {
            // forward와 수직인 벡터 찾기
            Vector3 up = Vector3.Cross(forward, Vector3.right);

            // forward가 right와 거의 평행하면 다른 축 사용
            if (up.sqrMagnitude < 0.001f)
            {
                up = Vector3.Cross(forward, Vector3.up);
            }

            return up.normalized;
        }

        /// <summary>
        /// Triangle 인덱스 생성
        /// </summary>
        private void GenerateTriangles(int pathCount)
        {
            for (int i = 0; i < pathCount - 1; i++)
            {
                for (int j = 0; j < RadialSegments; j++)
                {
                    int current = i * RadialSegments + j;
                    int next = current + RadialSegments;
                    int nextJ = (j + 1) % RadialSegments;

                    // 첫 번째 삼각형
                    _triangles.Add(current);
                    _triangles.Add(next);
                    _triangles.Add(i * RadialSegments + nextJ);

                    // 두 번째 삼각형
                    _triangles.Add(i * RadialSegments + nextJ);
                    _triangles.Add(next);
                    _triangles.Add(next + nextJ - j);
                }
            }
        }

        /// <summary>
        /// 기존 메시 업데이트 (재생성 대신 수정)
        /// 성능 최적화용
        /// </summary>
        public void UpdateMesh(Mesh mesh, List<Vector3> path)
        {
            if (mesh == null || path == null || path.Count < 2)
            {
                return;
            }

            // 경로 포인트 수가 변경되었으면 재생성
            int expectedVertexCount = path.Count * RadialSegments;
            if (mesh.vertexCount != expectedVertexCount)
            {
                Game.Utilities.PrototypeDebug.Log($"TubeMeshGenerator: Regenerating mesh, vertexCount {mesh.vertexCount} -> {expectedVertexCount} (path count: {path.Count})");

                Mesh newMesh = GenerateMesh(path);
                if (newMesh != null)
                {
                    mesh.Clear();
                    mesh.vertices = newMesh.vertices;
                    mesh.triangles = newMesh.triangles;
                    mesh.uv = newMesh.uv;
                    mesh.normals = newMesh.normals;
                    mesh.RecalculateBounds();

                    Game.Utilities.PrototypeDebug.Log($"TubeMeshGenerator: Mesh regenerated, new vertexCount = {mesh.vertexCount}");
                }
                return;
            }

            // 버텍스만 업데이트 (Triangle은 유지)
            _vertices.Clear();

            for (int i = 0; i < path.Count; i++)
            {
                Vector3 forward = GetForwardVector(path, i);
                Vector3 up = CalculateUp(forward);
                Vector3 right = Vector3.Cross(forward, up).normalized;

                for (int j = 0; j < RadialSegments; j++)
                {
                    float angle = (j / (float)RadialSegments) * Mathf.PI * 2f;
                    Vector3 offset = (Mathf.Cos(angle) * right + Mathf.Sin(angle) * up) * TubeRadius;
                    _vertices.Add(path[i] + offset);
                }
            }

            mesh.vertices = _vertices.ToArray();
            mesh.RecalculateBounds();
        }
    }
}
