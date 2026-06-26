using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class RPCDemo : NetworkBehaviour
{
    public int m_ActionID = 1;

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
        if (keyboard.fKey.wasPressedThisFrame)
        {
            RequestActionRPC(m_ActionID);
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestActionRPC(int actionID, RpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        if (actionID <= 0)
        {
            Debug.LogWarning($"[Server] 잘못된 actionID = {actionID} (clientId = {senderClientId})");
            return;
        }

        Debug.Log($"[Server] clientId = {senderClientId}, 액션 = {actionID} 수락");

        AnnounceActionRPC(senderClientId, actionID);

        AckRPC(actionID, RpcTarget.Single(senderClientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void AnnounceActionRPC(ulong actorClientId, int actionID)
    {
        bool isMine = actorClientId == NetworkManager.LocalClientId;
        Debug.Log($"[Client/Host] clientId = {actorClientId}, 액션 = {actionID}, isMine? {isMine}");
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void AckRPC(int actionID, RpcParams rpcParams)
    {
        Debug.Log($"[Client] 서버 응답 수신: actionID{actionID} 처리 완료");
    }
}
