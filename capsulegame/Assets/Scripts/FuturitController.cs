using Futurift.DataSenders;
using Futurift.Options;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using System.Collections;
using TMPro;

namespace Futurift
{
    public class FuturiftController : MonoBehaviour
    {
        [SerializeField] private string ipAddress = "127.0.0.1";
        [SerializeField] private int port = 6065;
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [Header("Camera Settings")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float cameraRotationSpeed = 90f;
        [Header("Capsule Tilt Settings")]
        [SerializeField] private float maxRoll = 15f;
        [SerializeField] private float maxMovementPitch = 10f;
        [SerializeField] private float tiltSmoothTime = 0.1f;
        [SerializeField] private float tiltReturnSpeed = 10f;
        [Header("Input Actions")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference rotateAction;
        [SerializeField] private Transform xrOrigin;
        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugLogs = true;

        private FutuRiftController _controller;
        private Vector3 currentVelocity;
        private Vector3 targetVelocity;
        private Vector3 lastMoveDirection;
        private bool isMoving = false;
        private Vector3 targetPosition;

        // СИСТЕМА НАКЛОНОВ КАПСУЛЫ
        private float capsuleTiltPitch = 0f;
        private float capsuleTiltRoll = 0f;
        private float tiltPitchVelocity = 0f;
        private float tiltRollVelocity = 0f;

        // СИСТЕМА ДВИЖЕНИЯ
        private Vector3 lastPosition;
        private bool wasMoving = false;

        private bool isWalking = false;
        private bool isRotating = false;

        // Для тестирования
        private bool testMovementForward = false;
        private bool testMovementBackward = false;
        private bool testStrafeRight = false;
        private bool testStrafeLeft = false;

        private void Awake()
        {
            Debug.Log("=== FUTURIFT CONTROLLER AWAKE ===");

            var udpOptions = new UdpOptions
            {
                ip = ipAddress,
                port = port
            };
            _controller = new FutuRiftController(new UdpPortSender(udpOptions));

            //// Убираем Rigidbody если он есть
            //Rigidbody rb = GetComponent<Rigidbody>();
            //if (rb != null)
            //{
            //    Destroy(rb);
            //}

            // Находим камеру
            if (cameraTransform == null)
            {
                cameraTransform = Camera.main?.transform;
                if (cameraTransform == null)
                {
                    Debug.LogError("[Futurift] Камера не найдена!");
                }
            }

            lastPosition = transform.position;
            targetPosition = transform.position;
        }

        private void Start()
        {
            Debug.Log("=== FUTURIFT CONTROLLER START ===");
        }

        private void OnEnable()
        {
            _controller?.Start();
            if (moveAction != null) moveAction.action.Enable();
            if (rotateAction != null) rotateAction.action.Enable();
        }

        private void OnDisable()
        {
            _controller?.Stop();
            if (moveAction != null) moveAction.action.Disable();
            if (rotateAction != null) rotateAction.action.Disable();
        }

        private void Update()
        {
            if (moveAction == null || rotateAction == null) return;

            Vector2 moveInput = GetTestMovementInput();
            Vector2 rotateInput = GetTestRotationInput();

            // Мертвые зоны
            if (moveInput.magnitude < 0.15f) moveInput = Vector2.zero;
            if (rotateInput.magnitude < 0.15f) rotateInput = Vector2.zero;

            // 1. ОБРАБОТКА ПОВОРОТА КАПСУЛЫ И КАМЕРЫ (правый стик)
            HandleRotation(rotateInput);

            // 2. ОБРАБОТКА ДВИЖЕНИЯ КАПСУЛЫ
            HandleCapsuleMovement(moveInput);

            // 3. ОБРАБОТКА НАКЛОНОВ КАПСУЛЫ (от ЛЕВОГО стика)
            HandleCapsuleTilts(moveInput, rotateInput);

            // 4. ПРИМЕНЕНИЕ ВРАЩЕНИЯ КАПСУЛЫ
            ApplyCapsuleRotation();

        }

        private void HandleCapsuleMovement(Vector2 moveInput)
        {
            bool hasMoveInput = moveInput.magnitude > 0.1f;

            if (hasMoveInput)
            {
                // Направление движения от КАПСУЛЫ
                Vector3 moveDirection = Vector3.zero;

                // Движение вперед/назад относительно капсулы
                if (Mathf.Abs(moveInput.y) > 0.1f)
                {
                    Vector3 capsuleForward = transform.forward;
                    capsuleForward.y = 0;
                    capsuleForward.Normalize();
                    moveDirection += capsuleForward * moveInput.y;
                }

                // Боковое движение относительно капсулы
                if (Mathf.Abs(moveInput.x) > 0.3f)
                {
                    Vector3 capsuleRight = transform.right;
                    capsuleRight.y = 0;
                    capsuleRight.Normalize();
                    moveDirection += capsuleRight * moveInput.x * 0.7f;
                }

                if (moveDirection.magnitude > 0.1f)
                {
                    moveDirection.Normalize();

                    // НЕПОСРЕДСТВЕННОЕ перемещение без сохранения состояния
                    transform.position += moveDirection * moveSpeed * Time.deltaTime;
                    isMoving = true;
                }
                else
                {
                    isMoving = false;
                }
            }
            else
            {
                isMoving = false;
                // НИКАКОГО движения - позиция остается как есть
            }

            // Синхронизация с XR Origin
            if (xrOrigin != null)
            {
                xrOrigin.position = transform.position;
            }
        }

        private void HandleRotation(Vector2 rotateInput)
        {
            if (Mathf.Abs(rotateInput.x) > 0.1f && xrOrigin != null)
            {
                // БЫЛО: float rotationAmount = rotateInput.x * rotationSpeed * Time.deltaTime;
                // СТАЛО: меняем знак для правильного направления
                float rotationAmount = rotateInput.x * cameraRotationSpeed * Time.deltaTime;
        
                // Поворачиваем всю XR Origin
                xrOrigin.Rotate(0f, rotationAmount, 0f);
        
                if (enableDebugLogs && Mathf.Abs(rotateInput.x) > 0.5f)
                {
                    Debug.Log($"[Rotation] Input: {rotateInput.x:F2}, Amount: {rotationAmount:F2}°");
                }
            }
        }

        private void HandleCapsuleTilts(Vector2 moveInput, Vector2 rotateInput)
        {
            bool isMoving = moveInput.magnitude > 0.1f;
            bool isRotating = rotateInput.magnitude > 0.1f;

            float desiredTiltPitch = 0f;
            float desiredTiltRoll = 0f;

            // 1. НАКЛОН ВПЕРЕД/НАЗАД ОТ ДВИЖЕНИЯ (левый стик)
            if (isMoving)
            {
                if (moveInput.y > 0.3f) // Движение вперед
                {
                    desiredTiltPitch = maxMovementPitch * Mathf.Clamp01(moveInput.y);
                }
                else if (moveInput.y < -0.3f) // Движение назад
                {
                    desiredTiltPitch = -maxMovementPitch * 0.5f * Mathf.Clamp01(-moveInput.y);
                }
            }

            // 2. КРЕН ОТ БОКОВОГО ДВИЖЕНИЯ (левый стик)
            if (Mathf.Abs(moveInput.x) > 0.3f)
            {
                // Крен при боковом движении влево/вправо
                desiredTiltRoll = -moveInput.x * maxRoll * 0.8f * Mathf.Clamp01(Mathf.Abs(moveInput.x));
            }

            // 3. КРЕН ОТ ПОВОРОТА (правильное направление)
            if (isRotating)
            {
                // БЫЛО: desiredTiltRoll += rotateInput.x * maxRoll * 0.6f;
                // СТАЛО: меняем знак для согласованности
                desiredTiltRoll += rotateInput.x * maxRoll * 0.6f;
            }

            // ПРИМЕНЕНИЕ НАКЛОНОВ
            if (isMoving || isRotating)
            {
                // Плавное достижение целевых наклонов
                capsuleTiltPitch = Mathf.SmoothDamp(capsuleTiltPitch, desiredTiltPitch, ref tiltPitchVelocity, tiltSmoothTime);
                capsuleTiltRoll = Mathf.SmoothDamp(capsuleTiltRoll, desiredTiltRoll, ref tiltRollVelocity, tiltSmoothTime);
            }
            else
            {
                // БЫСТРЫЙ ВОЗВРАТ К НУЛЮ при отсутствии ввода
                capsuleTiltPitch = Mathf.MoveTowards(capsuleTiltPitch, 0f, tiltReturnSpeed * Time.deltaTime);
                capsuleTiltRoll = Mathf.MoveTowards(capsuleTiltRoll, 0f, tiltReturnSpeed * Time.deltaTime);
            }

            // Ограничения наклонов капсулы
            capsuleTiltPitch = Mathf.Clamp(capsuleTiltPitch, -maxMovementPitch, maxMovementPitch);
            capsuleTiltRoll = Mathf.Clamp(capsuleTiltRoll, -maxRoll, maxRoll);
        }

        private void ApplyCapsuleRotation()
        {
            // Базовое вращение капсулы (уже установлено в HandleRotation)
            // Добавляем только наклоны к текущему вращению
            Quaternion baseRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            Quaternion tiltRotation = Quaternion.Euler(capsuleTiltPitch, 0f, capsuleTiltRoll);

            transform.rotation = baseRotation * tiltRotation;

            // Отправка данных на физическую капсулу (ТОЛЬКО наклоны)
            _controller.Pitch = capsuleTiltPitch;
            _controller.Roll = capsuleTiltRoll;
        }

        private Vector2 GetTestMovementInput()
        {
            Vector2 input = moveAction.action.ReadValue<Vector2>();

            if (testMovementForward)
            {
                input.y = 1f;
                input.x = 0f;
            }
            else if (testMovementBackward)
            {
                input.y = -1f;
                input.x = 0f;
            }
            else if (testStrafeRight)
            {
                input.x = 1f;
                input.y = 0f;
            }
            else if (testStrafeLeft)
            {
                input.x = -1f;
                input.y = 0f;
            }

            return input;
        }

        private Vector2 GetTestRotationInput()
        {
            return rotateAction.action.ReadValue<Vector2>();
        }


        [ContextMenu("Debug Log Current State")]
        public void DebugLogCurrentState()
        {
            Vector2 moveInput = moveAction?.action?.ReadValue<Vector2>() ?? Vector2.zero;
            Vector2 rotateInput = rotateAction?.action?.ReadValue<Vector2>() ?? Vector2.zero;

            Debug.Log("=== FUTURIFT DEBUG STATE ===");
            Debug.Log($"[Futurift] Позиция: {transform.position}");
            Debug.Log($"[Futurift] Вращение капсулы: {transform.eulerAngles.y:F1}°");
            Debug.Log($"[Futurift] Наклоны капсулы: Pitch={capsuleTiltPitch:F1}°, Roll={capsuleTiltRoll:F1}°");
            Debug.Log($"[Futurift] Ввод: Move({moveInput.x:F2}, {moveInput.y:F2}), Rotate({rotateInput.x:F2}, {rotateInput.y:F2})");
            Debug.Log("=============================");
        }

        [ContextMenu("Force Stop")]
        public void ForceStop()
        {
            // Просто логируем - позиция и так не должна меняться
            Debug.Log("[Futurift] Принудительная остановка");
        }

        [ContextMenu("Reset Tilts")]
        public void ResetTilts()
        {
            capsuleTiltPitch = 0f;
            capsuleTiltRoll = 0f;
            tiltPitchVelocity = 0f;
            tiltRollVelocity = 0f;
            Debug.Log("[Futurift] Наклоны сброшены");
        }

        [ContextMenu("Test Forward Tilt")]
        public void TestForwardTilt()
        {
            testMovementForward = true;
            StartCoroutine(ResetTestAfterDelay(1.5f));
        }

        [ContextMenu("Test Backward Tilt")]
        public void TestBackwardTilt()
        {
            testMovementBackward = true;
            StartCoroutine(ResetTestAfterDelay(1.5f));
        }

        [ContextMenu("Test Strafe Right Tilt")]
        public void TestStrafeRightTilt()
        {
            testStrafeRight = true;
            StartCoroutine(ResetTestAfterDelay(1.5f));
        }

        [ContextMenu("Test Strafe Left Tilt")]
        public void TestStrafeLeftTilt()
        {
            testStrafeLeft = true;
            StartCoroutine(ResetTestAfterDelay(1.5f));
        }

        private IEnumerator ResetTestAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            testMovementForward = false;
            testMovementBackward = false;
            testStrafeRight = false;
            testStrafeLeft = false;
        }

        public void OnPlayerDeath()
        {
            _controller.Pitch = 0f;
            _controller.Roll = 0f;
            this.enabled = false;
        }

        public void Respawn()
        {
            this.enabled = true; // включаем обратно
        }
    }
}