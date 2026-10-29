using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(400)]
public sealed class CacaoArtSizing : MonoBehaviour
{
    private readonly Dictionary<Sprite,Sprite> slices=new Dictionary<Sprite,Sprite>();
    private GameManager game;
    private Coroutine pending;
    private void Start()
    {
        game=GetComponent<ProgressUI>().grid.Session;game.StateChanged+=Changed;Changed(game.State);
    }
    private void Changed(GameState state)
    {
        if(pending!=null) StopCoroutine(pending);pending=StartCoroutine(RefreshNextFrame());
    }
    private IEnumerator RefreshNextFrame() {yield return null;Apply();pending=null;}
    public void Apply()
    {
        foreach(var image in GetComponentsInChildren<Image>(true))
        {
            if(!image.sprite || image is RoundedImage) continue;
            var source=image.sprite;string name=source.name;
            if(name!="Panel__Header" && name!="Button__Primary" && name!="Button__Secondary") continue;
            float height=image.rectTransform.rect.height;if(height<=0) continue;
            if(!slices.TryGetValue(source,out var sliced))
            {
                if(source.border.x>0) sliced=source;
                else
                {
                    float end=source.rect.width*(name=="Panel__Header" ? .29f : .28f);
                    sliced=Sprite.Create(source.texture,source.rect,new Vector2(.5f,.5f),source.pixelsPerUnit,0,SpriteMeshType.FullRect,new Vector4(end,0,end,0));
                    sliced.name=name;
                }
                slices[source]=sliced;slices[sliced]=sliced;
            }
            image.sprite=sliced;image.type=Image.Type.Sliced;image.preserveAspect=false;
            float reference=image.canvas ? image.canvas.referencePixelsPerUnit : 100;
            image.pixelsPerUnitMultiplier=source.rect.height/height*reference/source.pixelsPerUnit;
        }
    }
    private void OnDestroy()
    {
        if(game) game.StateChanged-=Changed;
        var destroyed=new HashSet<Sprite>();foreach(var pair in slices) if(pair.Key!=pair.Value && destroyed.Add(pair.Value))
        {if(Application.isPlaying) Destroy(pair.Value);else DestroyImmediate(pair.Value);}
    }
}
