using Unity.Netcode;
using UnityEngine;

public class PlayerSetupUIhandler : MonoBehaviour
{
    [SerializeField] GameObject player;
    private void Start()
    {
        GameManager.Instance.UnitySignIn();
    }
    public void OnPlay()
    {
        GameManager.Instance.AddNewServerPlayer(player);
    }
}
