using System.Linq;
using UnityEngine;

public class PlayerAssignToSpawnPoints : MonoBehaviour
{
    [SerializeField] Cinemachine.CinemachineFreeLook cinemachineFreeLook;
    Transform[] spawnPoints;
    private void Awake()
    {
        spawnPoints = GetComponentsInChildren<Transform>();
        GameManager.Instance.MatchFound += OnMatchFound;
        GameManager.Instance.FindMatch();
    }
    private void OnDestroy()
    {
        GameManager.Instance.MatchFound -= OnMatchFound;
    }

    private void OnMatchFound()
    {
        var randomPoint = Random.Range(0, spawnPoints.Length);
        StartCoroutine(GameManager.Instance.SetPlayerPos(spawnPoints[randomPoint],cinemachineFreeLook));
        //GameManager.Instance.serverPlayer.AddComponent<Rigidbody>();
        
    }
}
