using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStats : NetworkBehaviour
{
    public int mScorePerPress = 1;
    public int m_StartHealth = 100;

    // private readonly NetworkVariable<int> m_Score = new NetworkVariable<int>(
    //     0,
    //     NetworkVariableReadPermission.Everyone,
    //     NetworkVariableWritePermission.Owner
    // );

    public int Score
    {
        get => mScorePerPress;
        set
        {
            if (IsOwner)
            {
                mScorePerPress = value;
            }
        }
    }

    private readonly NetworkVariable<int> m_Hp = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    private readonly NetworkVariable<FixedString32Bytes> m_DisplayName = new NetworkVariable<FixedString32Bytes>(
        "default",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public override void OnNetworkSpawn()
    {
        // m_Score.OnValueChanged += HandleScoreChanged;
        m_Hp.OnValueChanged += HandleHealthChanged;
        m_DisplayName.OnValueChanged += HandleNameChanged;

        // ApplyScore(m_Score.Value);
        ApplyHealth(m_Hp.Value);
        ApplyName(m_DisplayName.Value);

        if (IsServer)
        {
            m_Hp.Value = m_StartHealth;
        }
        if (IsOwner && m_DisplayName.Value.Length != 0)
        {
            m_DisplayName.Value = new FixedString32Bytes($"Player {OwnerClientId}");
        }

        if (!IsServer)
        {
            RequestCurrentScoreRPC();
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

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            RequestScoreRPC(Score);
        }
        if (keyboard.hKey.wasPressedThisFrame)
        {
            m_Hp.Value -= 5;
        }
    }

    public override void OnNetworkDespawn()
    {
        // m_Score.OnValueChanged -= HandleScoreChanged;
        m_Hp.OnValueChanged -= HandleHealthChanged;
        m_DisplayName.OnValueChanged -= HandleNameChanged;
    }

    private void HandleScoreChanged(int prev, int current)
    {
        ApplyScore(current);
        Debug.Log($"[PlayerStats] 점수 변경 {prev} -> {current}");
    }
    private void HandleHealthChanged(int prev, int current)
    {
        ApplyHealth(current);
        Debug.Log($"[PlayerStats] 체력 변경 {prev} -> {current}");
    }
    private void HandleNameChanged(FixedString32Bytes prev, FixedString32Bytes current)
    {
        ApplyName(current);
        Debug.Log($"[PlayerStats] 이름 변경 {prev} -> {current}");
    }

    private void ApplyScore(int value)
    {
        // m_Score.Value += value;
    }
    private void ApplyHealth(int value)
    {
        m_Hp.Value += value;
    }
    private void ApplyName(FixedString32Bytes value)
    {
        m_DisplayName.Value = value;
    }


    [Rpc(SendTo.Server)]
    private void RequestScoreRPC(int score, RpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        if (senderClientId != OwnerClientId)
        {
            return;
        }

        score += mScorePerPress;

        BroadcastScoreRPC(score);
        // AckRPC(score, RpcTarget.Single(senderClientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void BroadcastScoreRPC(int newScore)
    {
        Score = newScore;
        ApplyScore(newScore);
    }

    [Rpc(SendTo.Server)]
    private void RequestCurrentScoreRPC(RpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        SendCurrentScoreRPC(Score, RpcTarget.Single(senderClientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void SendCurrentScoreRPC(int currentScore, RpcParams rpcParams)
    {
        Score = currentScore;
        ApplyScore(Score);
    }

}
