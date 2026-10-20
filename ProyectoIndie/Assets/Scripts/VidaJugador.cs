
using UnityEngine;
using Unity.Netcode;

public class VidaJugador : NetworkBehaviour
{
    [Header("Vida")]
    public float vidaMaxima = 100f;

    public NetworkVariable<float> vidaActual = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool muerto;

    public override void OnNetworkSpawn()
    {
        muerto = false;

        if (IsServer)
            vidaActual.Value = vidaMaxima;

        vidaActual.OnValueChanged += ActualizarVida;
        ActualizarVida(vidaActual.Value, vidaActual.Value);
    }

    public override void OnNetworkDespawn()
    {
        vidaActual.OnValueChanged -= ActualizarVida;
    }

    public void RecibirDano(float cantidad)
    {
        if (!IsServer || muerto || cantidad <= 0f)
            return;

        vidaActual.Value = Mathf.Max(0f, vidaActual.Value - cantidad);

        Debug.Log("Jugador " + OwnerClientId + " recibió " + cantidad +
                  " de daño. Vida restante: " + vidaActual.Value);

        if (vidaActual.Value <= 0f)
        {
            Morir();
        }
    }

    private void Morir()
    {
        if (!IsServer || muerto)
            return;

        muerto = true;

        Debug.Log("Jugador " + OwnerClientId + " murió.");

        NetworkObject.Despawn(true);
    }

    private void ActualizarVida(float anterior, float nueva)
    {
        Debug.Log("Jugador " + OwnerClientId + " | Vida: " + nueva);
    }
}
