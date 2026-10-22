using UnityEngine;

public class Rotation : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private char axis = 'Z';
    void Start()
    {

    }

    void Update()
    {
        Vector3 rotationAxis = Vector3.forward; // ось Z по умолчанию

        if (axis == 'X') rotationAxis = Vector3.right;
        if (axis == 'Y') rotationAxis = Vector3.up;

        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
    }
}
