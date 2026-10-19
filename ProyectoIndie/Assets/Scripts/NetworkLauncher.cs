
using UnityEngine;
using Unity.Netcode;

public class NetworkLauncher : MonoBehaviour
{
    private void OnGUI()
    {
        NetworkManager manager = NetworkManager.Singleton;

        if (manager == null)
        {
            GUI.Label(new Rect(20, 20, 300, 30), "No se encontro NetworkManager");
            return;
        }

        if (!manager.IsListening)
        {
            if (GUI.Button(new Rect(20, 20, 180, 40), "Iniciar Host"))
            {
                manager.StartHost();
            }

            if (GUI.Button(new Rect(20, 70, 180, 40), "Conectar Cliente"))
            {
                manager.StartClient();
            }

            if (GUI.Button(new Rect(20, 120, 180, 40), "Iniciar Servidor"))
            {
                manager.StartServer();
            }
        }
        else
        {
            string estado = manager.IsHost ? "Host" :
                manager.IsServer ? "Servidor" : "Cliente";

            GUI.Label(new Rect(20, 20, 250, 30),
                "Conectado como: " + estado);

            GUI.Label(new Rect(20, 50, 250, 30),
                "Jugadores: " + manager.ConnectedClientsIds.Count);

            if (GUI.Button(new Rect(20, 90, 180, 40), "Desconectar"))
            {
                manager.Shutdown();
            }
        }
    }
}
