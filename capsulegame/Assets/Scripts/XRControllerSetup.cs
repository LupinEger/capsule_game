using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XRControllerSetup : MonoBehaviour
{
    [Header("Controller Setup")]
    public GameObject leftControllerModel;
    public GameObject rightControllerModel;

    void Start()
    {
        SetupControllers();
    }

    void SetupControllers()
    {
        // Настройка левого контроллера для движения
        var leftController = GetComponentInChildren<ActionBasedController>();
        if (leftController != null)
        {
            leftController.enableInputTracking = true;
            leftController.enableInputActions = true;
        }

        // Настройка правого контроллера для поворота
        var rightController = GetComponentInChildren<ActionBasedController>();
        if (rightController != null)
        {
            rightController.enableInputTracking = true;
            rightController.enableInputActions = true;
        }

        // Включение моделей контроллеров (опционально)
        if (leftControllerModel != null) leftControllerModel.SetActive(true);
        if (rightControllerModel != null) rightControllerModel.SetActive(true);
    }
}
