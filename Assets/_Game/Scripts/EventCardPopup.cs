using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EventCardPopup : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button actionButton;

    // Hàm hiển thị popup với tiêu đề và nội dung tùy chỉnh
    public void ShowPopup(string title, string description)
    {
        gameObject.SetActive(true);
        if (titleText != null) titleText.text = title;
        if (descriptionText != null) descriptionText.text = description;
    }

    // Hàm ẩn popup khi bấm nút xác nhận
    public void HidePopup()
    {
        gameObject.SetActive(false);
    }

    private void Start()
    {
        // Gắn sự kiện đóng popup khi bấm nút
        if (actionButton != null)
        {
            actionButton.onClick.AddListener(HidePopup);
        }
    }
    
}