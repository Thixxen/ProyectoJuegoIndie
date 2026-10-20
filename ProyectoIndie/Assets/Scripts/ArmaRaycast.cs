
using UnityEngine;
using Unity.Netcode;

public class ArmaRaycast : NetworkBehaviour
{
    [Header("Configuracion del arma")]
    public float dano = 20f;
    public float alcance = 150f;
    public float disparosPorSegundo = 8f;

    [Header("Referencias")]
    public LayerMask capasImpacto = ~0;

    private Camera camaraJugador;
    private float proximoDisparo;
    private float siguienteDisparoServidor;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        // Buscar automaticamente la camara del jugador
        camaraJugador = GetComponentInChildren<Camera>(true);

        if (camaraJugador == null)
        {
            Debug.LogWarning(
                "ArmaRaycast: No se encontro la camara del jugador."
            );
        }
    }

    void Update()
    {
        if (!IsSpawned || !IsOwner) return;

        if (camaraJugador == null)
        {
            camaraJugador = GetComponentInChildren<Camera>(true);
            if (camaraJugador == null) return;
        }

        if (Input.GetMouseButton(0) &&
            Time.time >= proximoDisparo)
        {
            proximoDisparo =
                Time.time + 1f / Mathf.Max(0.1f, disparosPorSegundo);

            Vector3 origen = camaraJugador.transform.position;
            Vector3 direccion = camaraJugador.transform.forward;

            SolicitarDisparoRpc(origen, direccion);
        }
    }

    [Rpc(SendTo.Server)]
    private void SolicitarDisparoRpc(
        Vector3 origen,
        Vector3 direccion,
        RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId)
            return;

        if (direccion.sqrMagnitude < 0.001f)
            return;

        if (Vector3.Distance(origen, transform.position) > 3f)
            return;

        if (Time.time < siguienteDisparoServidor)
            return;

        siguienteDisparoServidor =
            Time.time + 1f / Mathf.Max(0.1f, disparosPorSegundo);

        direccion.Normalize();

        RaycastHit[] impactos = Physics.RaycastAll(
            origen,
            direccion,
            alcance,
            capasImpacto,
            QueryTriggerInteraction.Ignore
        );

        System.Array.Sort(
            impactos,
            (a, b) => a.distance.CompareTo(b.distance)
        );

        foreach (RaycastHit impacto in impactos)
        {
            NetworkObject objeto = impacto.collider
                .GetComponentInParent<NetworkObject>();

            // Ignorar al propio jugador
            if (objeto != null &&
                objeto.NetworkObjectId == NetworkObjectId)
                continue;

            VidaJugador vida = impacto.collider
                .GetComponentInParent<VidaJugador>();

            if (vida != null)
            {
                vida.RecibirDano(dano);
            }

            break;
        }
    }
}
