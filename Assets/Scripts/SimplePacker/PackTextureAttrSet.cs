using UnityEngine;
using System;
using System.Collections.Generic;


namespace Assets.UI.SimplePacker
{
    public enum TextureVertexAttrType
    {
        Normal = 0 ,
        Breakdown = 1 ,
    }

    [System.Serializable]
    public struct BlockDetails
    {
        //在新图的UV信息
        public Vector2 uvBL; //bottom-left ;
        public Vector2 uvTL; //top-left 
        public Vector2 uvTR; //top-right ;
        public Vector2 uvBR; //bottom-BR ;

        //顶点位置 
        public Vector3 posBL;
        public Vector3 posTL;
        public Vector3 posTR;
        public Vector3 posBR;

        //相对PackTexut拆分块宽高
        public RectWraper rect;
        //相对原来的Textrue的碎片块
        public RectWraper chipRect; 
        public bool IsFilped;  //是否翻转过了

    }


    [System.Serializable]
    public struct TextureVertexAttr
    {
        TextureVertexAttrType dataType;

        //整块
        public BlockDetails blockDetail;

        public bool IsVaild
        {
            get
            {
                return !string.IsNullOrEmpty( szGUID ) && !string.IsNullOrEmpty(spriteName); 
            }
        }

        public string szGUID;   //引用的资源
        public string spriteName;   //用名字索引好使用
        public Sprite refSprite;    //引用的精灵
        public Texture2D refTexture;
    }



    [System.Serializable]
    public struct RectWraper
    {
        public int x; //左下角
        public int y; //右下角
        public int w; //宽
        public int h; //高

        public void Set(int xPos, int yPos, int weight, int height)
        {
            x = xPos;
            y = yPos;
            w = weight;
            h = height;
        }
    }

    public class PackTextureAttrSet : ScriptableObject
    {
        public int packTexWidth
        {
            get
            {
                int ret = 0;
                if (packTexture != null)
                {
                    ret = packTexture.width;
                }
                return ret;
            }
        }

        public int packTexHeight
        {
            get
            {
                int ret = 0;
                if (packTexture != null)
                {
                    ret = packTexture.height;
                }
                return ret;
            }
        }

        public Sprite packSprite; 
        public Texture2D packTexture;
        public List<TextureVertexAttr> texVertexAttrList = new List<TextureVertexAttr>();


        public void CopyForm(PackTextureAttrSet other)
        {
            if (other != null)
            {
                packTexture = other.packTexture;
                packSprite = other.packSprite;
                texVertexAttrList = new List<TextureVertexAttr>(other.texVertexAttrList);
            }
        }

        public TextureVertexAttr GetTextureVertexAttr(  string szSpriteName )
        {
            TextureVertexAttr ret = new TextureVertexAttr() ; 
            if( !string.IsNullOrEmpty(szSpriteName))
            {
                for( int i = 0 ; i < texVertexAttrList.Count; ++i)
                {
                    var attr = texVertexAttrList[i]; 
                    if( attr.IsVaild
                        && attr.spriteName.Equals(szSpriteName) )
                    {
                        ret = attr;
                        break;
                    }
                }            
            }
            return ret; 
        }

    }

}





