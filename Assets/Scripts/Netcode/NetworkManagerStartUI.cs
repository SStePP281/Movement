using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerStartUI : MonoBehaviour
{
    [SerializeField] private Button startServer;
    [SerializeField] private Button startHost;
    [SerializeField] private Button startClient;

    private void Awake()
    {
        startServer.onClick.AddListener(() => { NetworkManager.Singleton.StartServer(); Hide(); });
        startHost.onClick.AddListener(() => { NetworkManager.Singleton.StartHost(); Hide(); });
        startClient.onClick.AddListener(() => { NetworkManager.Singleton.StartClient(); Hide(); });
    }

    private void Start()
    {
        Show();
    }

    private void Show() => gameObject.SetActive(true);

    private void Hide() => gameObject.SetActive(false);
}
