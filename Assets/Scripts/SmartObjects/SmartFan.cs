using UnityEngine;

// A fan whose blades spin while it's on.
public class SmartFan : SmartObject
{
    [SerializeField] private Transform blades;   // child object holding the blade meshes
    [SerializeField] private float rpm = 240f;

    private void Update()
    {
        if (IsOn && blades != null)
            blades.Rotate(Vector3.up, rpm * 6f * Time.deltaTime, Space.Self); // rpm -> degrees per second
    }
}
