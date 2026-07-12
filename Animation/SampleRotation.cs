using UnityEngine;

public class SampleRotation : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private float range;

    private float _current;
    private void Start() { }
    private void Update()
    {
        _current += speed;
        var angle = Mathf.Sin(_current) * range;
        this.transform.rotation = Quaternion.Euler(new Vector3(angle, angle, angle));
    }
}

