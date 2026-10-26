
using Unity.Netcode;
using UnityEngine;

public class InventarioArmas : NetworkBehaviour
{
    [Header("Recoleccion")]
    [SerializeField] private float distanciaRecoleccion = 2.5f;

    [Header("Referencias")]
    [SerializeField] private GameObject armaVisualPrefab;
    [SerializeField] private Transform firstPersonWeaponHold;
    [SerializeField] private Transform thirdPersonWeaponHold;

    private readonly NetworkVariable<bool> tieneM4 =
        new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private GameObject armaVisualInstanciada;

    public bool TieneArmaEquipada => tieneM4.Value;

    public override void OnNetworkSpawn()
    {
        tieneM4.OnValueChanged += AlCambiarArma;
        ActualizarArma(tieneM4.Value);
    }

    public override void OnNetworkDespawn()
    {
        tieneM4.OnValueChanged -= AlCambiarArma;

        if (armaVisualInstanciada != null)
            Destroy(armaVisualInstanciada);
    }

    private void Update()
    {
        if (!IsSpawned || !IsOwner)
            return;

        if (Input.GetKeyDown(KeyCode.E) && !tieneM4.Value)
            BuscarArma();
    }

    private void BuscarArma()
    {
        Collider[] objetos = Physics.OverlapSphere(
            transform.position,
            distanciaRecoleccion,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide
        );

        ArmaRecolectable cercana = null;
        float menorDistancia = float.MaxValue;

        foreach (Collider col in objetos)
        {
            ArmaRecolectable arma =
                col.GetComponentInParent<ArmaRecolectable>();

            if (arma == null || !arma.IsSpawned)
                continue;

            float distancia = Vector3.Distance(
                transform.position,
                arma.transform.position
            );

            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                cercana = arma;
            }
        }

        if (cercana != null)
        {
            SolicitarRecogerRpc(
                new NetworkObjectReference(cercana.NetworkObject)
            );
        }
    }

    [Rpc(SendTo.Server)]
    private void SolicitarRecogerRpc(
        NetworkObjectReference referencia,
        RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId)
            return;

        if (tieneM4.Value)
            return;

        if (!referencia.TryGet(out NetworkObject objeto))
            return;

        if (!objeto.IsSpawned)
            return;

        float distancia = Vector3.Distance(
            transform.position,
            objeto.transform.position
        );

        if (distancia > distanciaRecoleccion + 0.5f)
            return;

        ArmaRecolectable arma =
            objeto.GetComponent<ArmaRecolectable>();

        if (arma == null)
            return;

        tieneM4.Value = true;
        arma.Recoger();
    }

    private void AlCambiarArma(bool anterior, bool nueva)
    {
        ActualizarArma(nueva);
    }

    private void ActualizarArma(bool equipada)
    {
        if (armaVisualInstanciada != null)
        {
            Destroy(armaVisualInstanciada);
            armaVisualInstanciada = null;
        }

        if (!equipada || armaVisualPrefab == null)
            return;

        Transform punto = IsOwner
            ? firstPersonWeaponHold
            : thirdPersonWeaponHold;

        if (punto == null)
        {
            Debug.LogWarning(
                "InventarioArmas: falta asignar el punto de anclaje."
            );
            return;
        }

        armaVisualInstanciada = Instantiate(armaVisualPrefab, punto);
        armaVisualInstanciada.transform.localPosition = Vector3.zero;
        armaVisualInstanciada.transform.localRotation = Quaternion.identity;
        armaVisualInstanciada.transform.localScale = Vector3.one;
    }
}
