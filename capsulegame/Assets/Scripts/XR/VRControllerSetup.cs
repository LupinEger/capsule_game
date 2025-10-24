using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class VRControllerSetup : MonoBehaviour
{
    [Header("Controller Settings")]
    public bool useDirectInteractor = true;

    void Start()
    {
        AddControllers();
        SetupMovement();
        AddHandVisuals();
        TagAsPlayer();
    }

    void AddControllers()
    {
        Transform cameraOffset = transform.Find("Camera Offset");
        if (cameraOffset == null) return;

        // === ����� ���� ===
        GameObject leftController = new GameObject("LeftHand Controller");
        leftController.transform.SetParent(cameraOffset);
        leftController.transform.localPosition = new Vector3(-0.2f, 0, 0.1f);

        // ��������� ����������
        if (useDirectInteractor)
        {
            leftController.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor>();
        }
        else
        {
            leftController.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
            leftController.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual>();
        }

        // ��������� ����������
        XRController leftXRController = leftController.AddComponent<XRController>();
        leftXRController.controllerNode = UnityEngine.XR.XRNode.LeftHand;

        // === ������ ���� ===
        GameObject rightController = new GameObject("RightHand Controller");
        rightController.transform.SetParent(cameraOffset);
        rightController.transform.localPosition = new Vector3(0.2f, 0, 0.1f);

        // ��������� ����������
        if (useDirectInteractor)
        {
            rightController.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor>();
        }
        else
        {
            rightController.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
            rightController.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual>();
        }

        // ��������� ����������
        XRController rightXRController = rightController.AddComponent<XRController>();
        rightXRController.controllerNode = UnityEngine.XR.XRNode.RightHand;
    }

    void SetupMovement()
    {
        // ��������� Locomotion System ���� ���
        if (FindObjectOfType<LocomotionSystem>() == null)
        {
            GameObject locomotionObj = new GameObject("Locomotion System");
            locomotionObj.AddComponent<LocomotionSystem>();
        }

        // ��������� Continuous Move Provider ���� ���
        if (GetComponent<ContinuousMoveProviderBase>() == null)
        {
            ContinuousMoveProviderBase moveProvider = gameObject.AddComponent<ActionBasedContinuousMoveProvider>();
        }

        // ��������� Continuous Turn Provider ���� ���
        if (GetComponent<ContinuousTurnProviderBase>() == null)
        {
            ContinuousTurnProviderBase turnProvider = gameObject.AddComponent<ActionBasedContinuousTurnProvider>();
        }
    }

    void AddHandVisuals()
    {
        // ������� �����������
        XRController[] controllers = GetComponentsInChildren<XRController>();

        foreach (XRController controller in controllers)
        {
            // ������� ���������� ������������� ����
            GameObject handVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handVisual.name = controller.controllerNode + " Hand Visual";
            handVisual.transform.SetParent(controller.transform);
            handVisual.transform.localPosition = Vector3.zero;
            handVisual.transform.localScale = new Vector3(0.1f, 0.2f, 0.3f);

            // ����������� ����
            Renderer renderer = handVisual.GetComponent<Renderer>();
            renderer.material.color = controller.controllerNode == UnityEngine.XR.XRNode.LeftHand ?
                Color.blue : Color.red;

            // ��������� ������������
            //handVisual.AddComponent<HandStabilizer>();

            // ������� ��������� ����� �� ����� ��������������
            DestroyImmediate(handVisual.GetComponent<BoxCollider>());
        }
    }

    void TagAsPlayer()
    {
        gameObject.tag = "Player";
    }
}
