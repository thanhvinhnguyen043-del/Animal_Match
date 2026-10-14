using UnityEngine;
using UnityEngine.UI;

public enum TileState { Normal, Ice, Hidden, Barrier }

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
    public GameObject barrierOverlay;
    public GameObject highlightObj;
    public Button button;
    [Header("Collider Input")]
    public BoxCollider2D hitCollider;
    private TileColliderInput colliderInput;
    public bool CanReceiveColliderClick => isActiveAndEnabled && hitCollider && hitCollider.enabled &&
        currentState!=TileState.Hidden && currentState!=TileState.Barrier;
    
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
    }

    public void SetState(TileState newState)
    {
        currentState = newState;
        if (iceOverlay) iceOverlay.SetActive(currentState == TileState.Ice);
        if (hiddenOverlay) hiddenOverlay.SetActive(currentState == TileState.Hidden);
        

        if (barrierOverlay) barrierOverlay.SetActive(currentState == TileState.Barrier); 
        if(hitCollider) hitCollider.enabled=currentState!=TileState.Hidden && currentState!=TileState.Barrier;
    }

    public void SetHighlight(bool isOn)
    {
        if (highlightObj) highlightObj.SetActive(isOn);
    }

    public void ConfigureColliderInput(TileColliderInput input)
    {
        colliderInput=input;
        if(!hitCollider) hitCollider=GetComponent<BoxCollider2D>();
        if(!hitCollider) hitCollider=gameObject.AddComponent<BoxCollider2D>();
        gameObject.layer=input.TileLayer;
        hitCollider.isTrigger=true;
        RefreshHitbox();
        if(button) button.enabled=false;
        foreach(var graphic in GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget=false;
        SetState(currentState);
        input.Register(this);
    }
    private void RefreshHitbox()
    {
        if(!rect || !hitCollider) return;
        hitCollider.size=rect.rect.size;
        hitCollider.offset=rect.rect.center;
    }
    private void OnRectTransformDimensionsChange() { RefreshHitbox(); }
    private void OnEnable() { if(colliderInput) colliderInput.Register(this); }
    private void OnDisable() { if(colliderInput) colliderInput.Unregister(this); }
    public void DisableColliderInput()
    {
        if(hitCollider) hitCollider.enabled=false;
        if(colliderInput) colliderInput.Unregister(this);
    }
}
