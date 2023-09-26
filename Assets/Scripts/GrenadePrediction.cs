using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Sample
{
    public class GrenadePrediction : MonoBehaviour
    {
        [SerializeField]
        private LineRenderer lineRenderer;
        [SerializeField]
        private float radius;
        [SerializeField]
        private float maxPredictTime;
        [SerializeField]
        private Vector3 v0;


        private void Update()
        {
            lineRenderer.startWidth = radius;

            List<Vector3> vertices = ListPool<Vector3>.Get();
            Predict(transform.position, v0, Time.fixedDeltaTime, vertices);
            Vector3[] positions = ArrayPool<Vector3>.Shared.Rent(vertices.Count); // lineRenderer.SetPositions는 배열만을 입력받음
            vertices.CopyTo(positions); // vertices -> positions 복사

            lineRenderer.positionCount = vertices.Count;
            lineRenderer.SetPositions(positions);

            // 풀에서 빌린 list와 배열 반환
            ListPool<Vector3>.Release(vertices);
            ArrayPool<Vector3>.Shared.Return(positions);
        }

        private bool Predict(Vector3 p0, Vector3 v0, float fdt, IList<Vector3> vertices)
        {
            vertices.Clear();

            Vector3 current = CalculatePosition(p0, v0, 0);

            for (float t = 0; t < maxPredictTime; t += fdt)
            {
                // 다음 step 위치
                Vector3 next = CalculatePosition(p0, v0, t + fdt);
                // 다음 step까지 변화량
                Vector3 delta = next - current;

                Ray ray = new Ray(current, delta);

                // 예측 성공
                if (Physics.SphereCast(ray, radius, out RaycastHit hitInfo, delta.magnitude))
                {
                    vertices.Add(hitInfo.point + hitInfo.normal * radius); // 충돌 위치 추가
                    return true;
                }

                // 현재 위치 추가
                vertices.Add(current);
                current = next;
            }

            // Predict가 실패하더라도 궤도 정보는 알고 싶을 수 있으므로 Clear는 안 하는 게 나을 듯
            // vertices.Clear();
            return false;
        }

        // 초기 위치 p0, 초기 속도 v0, 운동 시작으로부터의 시간 t일 때의 위치 계산
        private static Vector3 CalculatePosition(Vector3 p0, Vector3 v0, float t)
        {
            return p0 + v0 * t + 0.5f * t * t * Physics.gravity;
        }
    }
}
