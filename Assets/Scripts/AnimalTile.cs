using UnityEngine;
using UnityEngine.UI;

public enum TileState { Normal, Ice, Hidden, Barrier } // Thêm trạng thái Barrier (Chặn)

public class AnimalTile : MonoBehaviour
{
    [Header("Data")]
    public int animalID;
    public int gridX;
    public int gridY;
    public TileState currentState = TileState.Normal;
    
    [Header("UI References")]
    public Image animalImage;
    public GameObject iceOverlay;
    public GameObject hiddenOverlay;
    public GameObject barrierOverlay; // Biến mới để hứng cái ảnh Khối Chặn
    public GameObject highlightObj;
    public Button button;
    
    [HideInInspector] public RectTransform rect;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }
    public void SetupTile(int id, Sprite sprite, int x, int y, TileState state = TileState.Normal)
    {
        animalID = id;
        gridX = x;
        gridY = y;
        animalImage.sprite = sprite;
        if (highlightObj && highlightObj.TryGetComponent<Image>(out var highlightImage))
            highlightImage.sprite = sprite;
        SetHighlight(false);
        SetState(state);
        button.onClick.AddListener(OnTileClicked);
    }

    public void SetState(TileState newState)
    {
        currentState = newState;
        if (iceOverlay) iceOverlay.SetActive(currentState == TileState.Ice);
        if (hiddenOverlay) hiddenOverlay.SetActive(currentState == TileState.Hidden);
        
        // Bật/tắt ảnh khối chặn
        if (barrierOverlay) barrierOverlay.SetActive(currentState == TileState.Barrier); 
    }

    public void SetHighlight(bool isOn)
    {
        if (highlightObj) highlightObj.SetActive(isOn);
    }

    private void OnTileClicked()
    {
        GridManager.Instance.SelectTile(this);
    }
}
