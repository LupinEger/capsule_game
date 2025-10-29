using Unity.XR.CoreUtils;
using UnityEngine;

public class AlwaysUpdateUIPosition : MonoBehaviour
{
    [SerializeField] private float distance = 2f;
    [SerializeField] private float heightOffset = 0.3f;

    private Transform xrCamera;
    private Vector3 lastCameraPosition;
    private Quaternion lastCameraRotation;

    void Start()
    {
        XROrigin xrOrigin = FindObjectOfType<XROrigin>();
        if (xrOrigin != null)
        {
            xrCamera = xrOrigin.Camera.transform;
        }
        else
        {
            xrCamera = Camera.main.transform;
        }

        UpdateUIPosition();
    }

    void Update()
    {
        // Обновляем позицию только если камера переместилась
        if (xrCamera != null &&
            (Vector3.Distance(xrCamera.position, lastCameraPosition) > 0.01f ||
             Quaternion.Angle(xrCamera.rotation, lastCameraRotation) > 1f))
        {
            UpdateUIPosition();
        }
    }

    void UpdateUIPosition()
    {
        if (xrCamera == null) return;

        Vector3 cameraForward = xrCamera.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        Vector3 targetPosition = xrCamera.position +
                               cameraForward * distance +
                               Vector3.up * heightOffset;

        transform.position = targetPosition;

        // Поворачиваем к игроку
        Vector3 directionToCamera = xrCamera.position - targetPosition;
        directionToCamera.y = 0;

        if (directionToCamera != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(-directionToCamera);
        }

        lastCameraPosition = xrCamera.position;
        lastCameraRotation = xrCamera.rotation;
    }

    // Вызывать этот метод при смерти игрока
    public void ShowUI()
    {
        // Обновляем позицию перед показом
        UpdateUIPosition();
        gameObject.SetActive(true);
    }
}