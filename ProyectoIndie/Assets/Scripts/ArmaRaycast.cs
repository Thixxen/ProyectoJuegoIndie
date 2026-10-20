
using UnityEngine;
using Unity.Netcode;
using System;

public class ArmaRaycast : NetworkBehaviour
{
    [Header("Configuracion del arma")]
    [SerializeField] private float dano = 20f;
    [SerializeField] private float alcance = 150f;
    [SerializeField] private float disparosPorSegundo = 8f;

    [Header("Referencias")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private LayerMask capasImpacto = ~0;

    [Header("Mira")]
    [SerializeField] private bool mostrarMira = true;
    [SerializeField] private float tamanoMira = 8f;
    [SerializeField] private float grosorMira = 2f;
    [SerializeField] private float separacionMira = 3f;
    [SerializeField] private Color colorMira = Color.white;

    private NetworkObject jugador;
    private float proximoDisparo;
    private float siguienteDisparoServidor;

    private void Awake()
    {
        jugador = GetComponentInParent<NetworkObject>();

        if (jugador == null)
            jugador = GetComponent<NetworkObject>();
    }

    public override void OnNetworkSpawn()
    {
        if (playerCamera == null && jugador != null)
        {
            Camera camara = jugador.GetComponentInChildren<Camera>(true);

            if (camara != null)
                playerCamera = camara.transform;
        }

        if (playerCamera == null)
            Debug.LogError("ArmaRaycast: asigna la camara del jugador en el Inspector.");
    }

    private void Update()
    {
        if (!IsSpawned || !IsOwner || playerCamera == null)
            return;

        if (!Input.GetMouseButton(0) || Time.time < proximoDisparo)
            return;

        proximoDisparo = Time.time + 1f / Mathf.Max(0.1f, disparosPorSegundo);

        SolicitarDisparoRpc(playerCamera.position, playerCamera.forward);
    }

    private void OnGUI()
    {
        if (!mostrarMira || !IsSpawned || !IsOwner)
            return;

        float x = Screen.width * 0.5f;
        float y = Screen.height * 0.5f;

        GUI.color = colorMira;

        GUI.DrawTexture(new Rect(x - grosorMira / 2f, y - separacionMira - tamanoMira, grosorMira, tamanoMira), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x - grosorMira / 2f, y + separacionMira, grosorMira, tamanoMira), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x - separacionMira - tamanoMira, y - grosorMira / 2f, tamanoMira, grosorMira), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x + separacionMira, y - grosorMira / 2f, tamanoMira, grosorMira), Texture2D.whiteTexture);

        GUI.color = Color.white;
    }

    [Rpc(SendTo.Server)]
    private void SolicitarDisparoRpc(
        Vector3 origen,
        Vector3 direccion,
        RpcParams rpcParams = default)
    {
        if (jugador == null || !jugador.IsSpawned)
            return;

        if (rpcParams.Receive.SenderClientId != jugador.OwnerClientId)
            return;

        if (direccion.sqrMagnitude < 0.001f)
            return;

        if (Vector3.Distance(origen, jugador.transform.position) > 3f)
            return;

        if (Time.time < siguienteDisparoServidor)
            return;

        siguienteDisparoServidor = Time.time + 1f / Mathf.Max(0.1f, disparosPorSegundo);

        direccion.Normalize();

        RaycastHit[] impactos = Physics.RaycastAll(
            origen,
            direccion,
            alcance,
            capasImpacto,
            QueryTriggerInteraction.Ignore
        );

        Array.Sort(impactos, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit impacto in impactos)
        {
            NetworkObject objetoImpactado = impacto.collider.GetComponentInParent<NetworkObject>();

            if (objetoImpactado != null &&
                objetoImpactado.NetworkObjectId == jugador.NetworkObjectId)
                continue;

            VidaJugador vida = impacto.collider.GetComponentInParent<VidaJugador>();

            if (vida != null)
                vida.RecibirDano(dano);

            break;
        }
    }
}
