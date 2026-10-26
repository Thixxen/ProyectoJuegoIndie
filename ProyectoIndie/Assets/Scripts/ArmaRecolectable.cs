
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class ArmaRecolectable : NetworkBehaviour
{
    private NetworkVariable<bool> recogida = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        recogida.OnValueChanged += AlCambiarRecogida;
        ActualizarEstado(recogida.Value);
    }

    public override void OnNetworkDespawn()
    {
        recogida.OnValueChanged -= AlCambiarRecogida;
    }

    public void Recoger()
    {
        if (!IsServer || !IsSpawned || recogida.Value)
            return;

        recogida.Value = true;
    }

    private void AlCambiarRecogida(bool anterior, bool nueva)
    {
        ActualizarEstado(nueva);
    }

    private void ActualizarEstado(bool estaRecogida)
    {
        // Solo desactiva la parte visual y física del pickup.
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            r.enabled = !estaRecogida;

        foreach (Collider c in GetComponentsInChildren<Collider>(true))
            c.enabled = !estaRecogida;
    }
}
