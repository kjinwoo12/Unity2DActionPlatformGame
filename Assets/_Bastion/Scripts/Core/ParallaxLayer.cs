using UnityEngine;

namespace Bastion.Core
{
    /// <summary>
    /// 카메라 이동량의 일부만 따라가게 해서 원근감을 만든다. 0이면 월드에 고정, 1이면 카메라에 고정.
    /// </summary>
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float factorX = 0.5f;
        [SerializeField, Range(0f, 1f)] float factorY = 0.2f;

        Transform cameraTransform;
        Vector3 startPosition;
        Vector3 cameraStartPosition;

        void Start()
        {
            cameraTransform = Camera.main.transform;
            startPosition = transform.position;
            cameraStartPosition = cameraTransform.position;
        }

        void LateUpdate()
        {
            Vector3 delta = cameraTransform.position - cameraStartPosition;
            transform.position = startPosition + new Vector3(delta.x * factorX, delta.y * factorY, 0f);
        }
    }
}
