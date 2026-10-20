
using UnityEngine;
using Unity.Netcode;

public class VidaJugador : NetworkBehaviour
{
    [Header("Vida")]
    public float vidaMaxima = 100f;

    public NetworkVariable<float> vidaActual =
        new NetworkVariable<float>(
            100f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private bool muerto = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            vidaActual.Value = vidaMaxima;
        }

        vidaActual.OnValueChanged += ActualizarVida;
        ActualizarVida(vidaActual.Value, vidaActual.Value);
    }

    public override void OnNetworkDespawn()
    {
        vidaActual.OnValueChanged -= ActualizarVida;
    }

    public void RecibirDano(float cantidad)
    {
        if (!IsServer || muerto) return;

        vidaActual.Value = Mathf.Max(
            0f,
            vidaActual.Value - cantidad
        );

        if (vidaActual.Value <= 0f)
        {
            muerto = true;
            MorirRpc();
        }
    }

    private void ActualizarVida(float anterior, float nueva)
    {
        Debug.Log(
            "Jugador " + OwnerClientId +
            " | Vida: " + nueva
        );
    }

    [Rpc(SendTo.Everyone)]
    private void MorirRpc()
    {
        Debug.Log("El jugador ha muerto");

        // Aqui agregaremos:
        // - Animacion de muerte
        // - Desactivar controles
        // - Soltar inventario
        // - Respawn
    }
}
