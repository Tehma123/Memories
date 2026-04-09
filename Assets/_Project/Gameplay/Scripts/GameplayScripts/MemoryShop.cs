using UnityEngine;
using UnityEngine.SceneManagement;

public class MemoryShop : MonoBehaviour, IInteractable
{
    public void Interact() // Bắt buộc phải có hàm này vì đã ký "hợp đồng" IInteractable
    {
        Debug.Log("Mở cửa tiệm...");
    }
}