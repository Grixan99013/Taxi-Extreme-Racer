using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    public Checkpoint checkpoint;
    private CheckpointCounter checkpointCounter;

    private void Start()
    {
        checkpointCounter = FindObjectOfType<CheckpointCounter>();
        if (checkpoint == null)
        {
            checkpoint = GetComponent<Checkpoint>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("PlayerCar"))
        {
            if (checkpoint != null && !checkpoint.isReached && checkpointCounter != null)
            {
                checkpointCounter.OnCheckpointReached(checkpoint);
            }
        }
    }
}