using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace NetworkStudy.Student
{
    public class MyPlayerMovement : NetworkBehaviour
    {
        [Tooltip("초당 이동 속도(월드 유닛).")]
        [SerializeField]
        private float m_MoveSpeed = 5f;

        [Tooltip("초당 회전 속도(도).")]
        [SerializeField]
        private float m_RotateSpeed = 120f;
        private float m_JumpForce = 5f;
        private float m_Gravity = 9.81f;    // 중력 가속도 (유닛/초^2)
        private float m_VerticalVelocity = 0f;  // 수직 속도 (점프 및 낙하에 사용)

        private bool m_IsGrounded = true;

        private NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>(
                Vector3.zero,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Owner
            );


        private NetworkVariable<bool> isSprint = new NetworkVariable<bool>(
                false,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Owner
            );

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                Debug.Log($"[MyPlayerMovement] 내 플레이어 스폰 {OwnerClientId}");
            }
            else
            {
                Debug.Log($"[MyPlayerMovement] 다른 플레이어 스폰 {OwnerClientId}");
            }
        }

        private void Update()
        {
            if (!IsOwner)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            isSprint.Value = keyboard.leftShiftKey.isPressed;

            float move = 0f;
            float turn = 0f;

            if (keyboard.wKey.isPressed)
            {
                move += 1f;
            }
            if (keyboard.sKey.isPressed)
            {
                move -= 1f;
            }
            if (keyboard.aKey.isPressed)
            {
                turn -= 1f;
            }
            if (keyboard.dKey.isPressed)
            {
                turn += 1f;
            }

            // 지면 판정 (y <= 0)
            m_IsGrounded = transform.position.y <= 0f;

            // C 키 점프: 수직 속도를 JumpForce로 설정
            if (keyboard.cKey.wasPressedThisFrame)
            {
                Debug.Log($"[MyPlayerMovement] 점프 시도 isGrounded={m_IsGrounded}");
                if (m_IsGrounded)
                {
                    Debug.Log($"[MyPlayerMovement] 점프 성공");
                    m_VerticalVelocity = m_JumpForce;
                }
            }

            // 중력 적용 (착지 시에는 낙하 속도만 0으로, 점프 직후 속도는 유지)
            if (!m_IsGrounded)
            {
                m_VerticalVelocity -= m_Gravity * Time.deltaTime;   // 떨어지는거
            }
            else if (m_VerticalVelocity < 0f)
            {
                m_VerticalVelocity = 0f;    // 착지하면 속도 초기화
            }

            // 수직 이동 - Translate는 로컬 축 기준이므로 Space.World 지정
            float verticalDelta = m_VerticalVelocity * Time.deltaTime;
            // 땅 아래로 내려가지 않도록 clamp
            if (transform.position.y + verticalDelta < 0f)
            {
                verticalDelta = -transform.position.y;
                m_VerticalVelocity = 0f;
            }
            transform.Translate(0f, verticalDelta, 0f, Space.World);    // 수직으로 이동, 월드 좌표계 기준으로

            if (isSprint.Value)
            {
                move *= 3f;
            }
            else
            {
                move *= 1f;
            }

            transform.Rotate(0f, turn * m_RotateSpeed * Time.deltaTime, 0f);
            transform.Translate(0f, 0f, move * m_MoveSpeed * Time.deltaTime);
        }
    }
}
