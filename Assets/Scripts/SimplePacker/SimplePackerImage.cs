using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Assets.UI.SimplePacker;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class SimplePackerImage : BaseVertexEffect
{
    private Image bindImage
    {
        get
        {
            return gameObject.GetComponent<Image>();
        }
    }

    private Sprite bindSprite
    {
        get
        {
            Sprite _bindSprite = null;
            if (bindImage != null)
            {
                _bindSprite = bindImage.sprite;
            }

            return _bindSprite;
        }
    }

    [SerializeField]
    private PackTextureAttrSet _packerInfo;
    public PackTextureAttrSet packerInfo
    {
        set
        {
            if( _packerInfo != null 
                && _packerInfo.Equals(value) )
            {
                return; 
            }
            _packerInfo = value;
            SetPackSprite(_packerInfo); 
        }

        get
        {
            return _packerInfo; 
        }
    }

    [SerializeField]
    private string _spriteName; 
    public string SpriteName
    {
        get
        {
            return _spriteName; 
        }
        set
        {
            if (_spriteName.Equals(value) )
            {
                return; 
            }
            _spriteName = value;
            SetDirty();
        }

    }



    [SerializeField]
    private TextureVertexAttr _texVertexAttr;
    public TextureVertexAttr texVertexAttr
    {
        get
        {
            return _texVertexAttr;       
        }
    }


    private RectTransform bindRectTran
    {
        get
        {
            return gameObject.GetComponent<RectTransform>();
        }
    }


    private void SetPackSprite(PackTextureAttrSet info)
    {
        if( info != null 
            && info.packSprite 
            && bindImage != null )
        {
            bindImage.sprite = info.packSprite; 
        }
    }

    void OnValidate()
    {
        SetPackSprite(_packerInfo);

        if( !string.IsNullOrEmpty(SpriteName) )
        {
            SetDirty(); 
        }
    }


    public override void ModifyVertices(List<UIVertex> vbo)
    {
        List<UIVertex> tVBO = new List<UIVertex>(vbo);
        vbo.Clear();

        if ( bindImage == null 
            || bindSprite == null 
            || packerInfo == null 
            || string.IsNullOrEmpty(SpriteName) )
        {
            return; 
        }

        if( packerInfo.texVertexAttrList == null 
            || packerInfo.texVertexAttrList.Count <= 0 )
        {
            Debug.LogWarning("No Sprite Info ");
            return;
        }


        TextureVertexAttr vertexAttr = packerInfo.GetTextureVertexAttr(SpriteName); 
        if( !vertexAttr.IsVaild )
        {
            Debug.LogWarning(string.Format("No corresponding vertex attr : {0}", SpriteName));
            return; 
        }

        _texVertexAttr = vertexAttr; 

        int i = 0; 
        //bottom-left
        UIVertex blVertex = new UIVertex();
        blVertex.position =  tVBO[i].position  ;
        blVertex.uv0 =  vertexAttr.blockDetail.uvBL ;
        blVertex.uv1 = vertexAttr.blockDetail.uvBL ;  //uv留给拆图用
        blVertex.color = bindImage.color;//colors[i];
                                      
        vbo.Add(blVertex);

        ++i;
        //top-left
        UIVertex tlVertex = new UIVertex();
        tlVertex.position = tVBO[i].position;
        tlVertex.uv0 = vertexAttr.blockDetail.uvTL ;
        tlVertex.uv1 = vertexAttr.blockDetail.uvTL ;
        tlVertex.color = bindImage.color;
        
        vbo.Add(tlVertex);

        ++i;
        //top-right
        UIVertex trVertex = new UIVertex();
        trVertex.position = tVBO[i].position;
        trVertex.uv0 = vertexAttr.blockDetail.uvTR; 
        trVertex.uv1 = vertexAttr.blockDetail.uvTR;
        trVertex.color = bindImage.color;
        
        vbo.Add(trVertex);

        ++i;
        //bottom-right
        UIVertex brVertex = new UIVertex();
        brVertex.position = tVBO[i].position;
        brVertex.uv0 = vertexAttr.blockDetail.uvBR;
        brVertex.uv1 = vertexAttr.blockDetail.uvBR; 
        brVertex.color = bindImage.color;
      
        vbo.Add(brVertex);

    }

    [ContextMenu("Set Dirty")]
    void SetDirty()
    {
        if (bindImage)
        {
            bindImage.SetVerticesDirty();
        }
    }

    [ContextMenu("Set Native Size")]
    void SetNative()
    {
        if ( bindRectTran 
            && texVertexAttr.IsVaild )
        {
            Rect tRect = bindRectTran.rect;
           
            bindRectTran.sizeDelta = new Vector2( texVertexAttr.blockDetail.IsFilped ? texVertexAttr.blockDetail.rect.h : texVertexAttr.blockDetail.rect.w ,
                texVertexAttr.blockDetail.IsFilped ? texVertexAttr.blockDetail.rect.w : texVertexAttr.blockDetail.rect.h) ;
            SetDirty(); 
        }
    }

}
