using UnityEngine;
using UnityEngine.EventSystems;

public class RotatePreview : MonoBehaviour, IDragHandler
{
    public Transform character; // Nhân vật muốn xoay
    public float rotationSpeed = 5f;

    public void OnDrag(PointerEventData eventData)
    {
        if (character != null)
        {
            character.Rotate(Vector3.up, -eventData.delta.x * rotationSpeed, Space.World);
        }
    }
}
