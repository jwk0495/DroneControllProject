using JWK.Scripts;
using UnityEditor;
using UnityEngine;

namespace JWK.Editor
{
    [CustomEditor(typeof(MultiCameraSystem))]
    public class MultiCameraSystemEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MultiCameraSystem cameraSystem = (MultiCameraSystem)target;

            EditorGUILayout.Space();

            if (GUILayout.Button("Preview Mechanism Camera"))
            {
                // [수정] 미리보기 함수가 이제 회전 값도 함께 처리합니다.
                cameraSystem.PositionMechanismCameraForPreview();
            }

            EditorGUILayout.HelpBox("위 버튼을 클릭하면 'Impact Camera'가 미리보기 위치와 회전으로 설정됩니다. 오프셋과 회전 값을 조정한 뒤 버튼을 다시 클릭하여 뷰를 업데이트하세요.", MessageType.Info);
        }
    }
}