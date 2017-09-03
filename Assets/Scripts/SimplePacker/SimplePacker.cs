using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Assets.UI.SimplePacker
{
    public class CNode
    {
        public CNode leftChild;
        public CNode rightChild;

        public TextureVertexAttr refTextureInfo; 

        public bool IsFilled
        {
            get
            {
                return !string.IsNullOrEmpty( refTextureInfo.szGUID ) && !string.IsNullOrEmpty( refTextureInfo.spriteName ) ;
            }
        }

        //是否为叶子结点
        public bool IsLeaf
        {
            get
            {
                return leftChild == null && rightChild == null; 
            }
        }

        //是否能放得下
        public bool CanFillIn( int weight ,int height , out bool outNeedFilp )
        {
            outNeedFilp = false;
            bool ret = false;
            if(refTextureInfo.blockDetail.rect.w >= weight && refTextureInfo.blockDetail.rect.h >= height)
            {
                ret = true;
            }
            else if(refTextureInfo.blockDetail.rect.h >= weight && refTextureInfo.blockDetail.rect.w >= height)
            {
                outNeedFilp = true;
                ret = true;
            }
            return  ret ;
        }

        //刚好塞得下
        public bool CanPerfectFillIn( int weight , int height )
        {
            return ( refTextureInfo.blockDetail.rect.w == weight && refTextureInfo.blockDetail.rect.h == height ) ; 
        }



        private void Fill( ref CNode retNode , Texture2D texture , bool bNeedFliped )
        {
            if( retNode != null )
            {
                string assetPath = AssetDatabase.GetAssetPath(texture);

                retNode.refTextureInfo.blockDetail.IsFilped = bNeedFliped;   //翻转的
                retNode.refTextureInfo.refSprite = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Sprite)) as Sprite;
                retNode.refTextureInfo.refTexture = texture;
                retNode.refTextureInfo.szGUID = AssetDatabase.AssetPathToGUID(assetPath);
                retNode.refTextureInfo.spriteName = Path.GetFileNameWithoutExtension(assetPath);
            }
        }


        private void SplitNode( int fillWidth ,int fillHeight , out CNode outLeftChild, out CNode outRightChild )
        {
            outLeftChild = new CNode();
            outRightChild = new CNode();

            int dw = refTextureInfo.blockDetail.rect.w - fillWidth;
            int dh = refTextureInfo.blockDetail.rect.h - fillHeight;

            if (dw >= dh)  //左右分割
            {
                outLeftChild.refTextureInfo.blockDetail.rect.Set(
                    refTextureInfo.blockDetail.rect.x,
                    refTextureInfo.blockDetail.rect.y,
                    fillWidth,
                    refTextureInfo.blockDetail.rect.h
                    );

                outRightChild.refTextureInfo.blockDetail.rect.Set(
                    refTextureInfo.blockDetail.rect.x + fillWidth,
                    refTextureInfo.blockDetail.rect.y,
                    dw,
                    refTextureInfo.blockDetail.rect.h
                    );
            }
            else  //上下分割
            {
                outLeftChild.refTextureInfo.blockDetail.rect.Set(
                   refTextureInfo.blockDetail.rect.x,
                   refTextureInfo.blockDetail.rect.y,
                   refTextureInfo.blockDetail.rect.w,
                   fillHeight
                   );

                outRightChild.refTextureInfo.blockDetail.rect.Set(
                    refTextureInfo.blockDetail.rect.x,
                    refTextureInfo.blockDetail.rect.y + fillHeight,
                    refTextureInfo.blockDetail.rect.w,
                    dh
                    );
            }
        }


        public CNode Insert( Texture2D texture )
        {
            CNode retNode = null;
            if (texture != null)
            {
                int texWidth = texture.width;
                int texHeight = texture.height; 

                if( !IsLeaf )  //不是叶子结点了
                {
                    if (leftChild != null)
                    {
                        retNode = leftChild.Insert(texture);  
                    }
                    if (retNode == null
                        && rightChild != null )
                    {
                        retNode = rightChild.Insert(texture);
                    }
                }
                else
                {
                    bool bNeedFliped = false;
                    if ( IsFilled )
                    {
                        retNode = null;
                    }
                    else if( CanFillIn(texWidth,texHeight,out bNeedFliped) )
                    {
                        bool bPerfectFillIn = bNeedFliped ?
                              CanPerfectFillIn(texHeight, texWidth) :
                              CanPerfectFillIn(texWidth, texHeight) ;
                          
                        //如果完美匹配
                        if ( bPerfectFillIn )
                        {
                            retNode = this;
                            Fill(ref retNode, texture, bNeedFliped); 
                        }
                        else
                        {
                            int fillWidth = (bNeedFliped ? texHeight : texWidth);
                            int fillHeight = (bNeedFliped ? texWidth : texHeight);

                            SplitNode(fillWidth, fillHeight, out leftChild, out rightChild); 

                            if( leftChild != null )
                            {
                                retNode = leftChild.Insert(texture);
                            }
                        }
                    }
                }
            }

            return retNode;
        }

    }

    public static class SimplePacker
    {
        private static Color mDefaultColor = new Color(0,0,0,0); 

        public enum enPOTSizeType
        {
            POT_32 = 32,
            POT_64 = 64,
            POT_128 = 128,
            POT_256 = 256,
            POT_512 = 512,
            POT_1024 = 1024,
        }

        public static void Pack( Texture2D[] splitTexs ,int width ,int height , out PackTextureAttrSet outPackTextureAttrSet )
        {
            outPackTextureAttrSet = null;
            if( splitTexs != null )
            {
                SortTexture(ref splitTexs);

                CNode rootNode = GenerateRootNode( splitTexs ,width,height );
                if( rootNode != null  )
                { 
                    //插一插
                    for (int i = 0; i < splitTexs.Length; ++i)
                    {
                        if( rootNode.Insert( splitTexs[i] ) == null )
                        {
                            string assetPath = AssetDatabase.GetAssetPath(splitTexs[i]); 
                            Debug.LogWarning(string.Format("no suitable area:{0}",assetPath));
                        }
                    }
                    //遍历一下节点树生成图集
                    GeneratePackTexture(rootNode, out outPackTextureAttrSet);
                } 
            }
        }

        public static void SetTextureReadble( Texture2D tex , bool bReadble )
        {
            if( tex != null )
            {
                TextureImporterSettings settings = GetTextureImporterSettings(tex);
                if( settings != null )
                {
                    settings.readable = bReadble;
                    SetTextureImporterSettings(tex,settings); 
                } 
            }
        }


        public static void Pack(Sprite[] sprites, int packTexWidth , int packTexHeight , out PackTextureAttrSet outPackTextureAttrSet)
        {
            outPackTextureAttrSet = null; 
            if( sprites != null )
            {
                Texture2D[] textures = new Texture2D[sprites.Length]; 
                for( int i = 0; i < sprites.Length; ++i)
                {
                    Texture2D readyTex = sprites[i].texture; ;
                    textures[i] = readyTex;

                    SetTextureReadble(readyTex, true);
                }

                Pack(textures,packTexWidth,packTexHeight, out outPackTextureAttrSet);

                //复原不可读
                for(int i = 0; i < textures.Length; ++i)
                {
                    Texture2D readyTex = textures[i]; 
                    if( readyTex != null )
                    {
                        SetTextureReadble(readyTex, false);
                    }
                }
            }
        }


        private static void SortTexture( ref Texture2D[] texs )
        {
            if( texs != null 
                && texs.Length > 1 )
            {
                Array.Sort( texs , delegate( Texture2D ta , Texture2D tb )
                {
                    if( ta.width * ta.height > tb.width * tb.height )
                    {
                        return -1; 
                    }     
                    else
                    {
                        return 1; 
                    }       
                }
                ); 
            }
        }

        public static TextureImporterSettings GetTextureImporterSettings( Texture2D tex )
        {
            TextureImporterSettings settings = null;
            if( tex != null )
            {
                TextureImporter importer = GetTextureImporter(tex);
                if (importer != null)
                {
                    settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                }
            }
            return settings; 
        }

        public static void SetTextureImporterSettings( Texture2D tex , TextureImporterSettings settings  )
        {
            if( tex != null 
                && settings != null )
            {
                TextureImporter importer = GetTextureImporter(tex);
                if( importer != null )
                {
                    importer.SetTextureSettings(settings);

                    string assetPath = AssetDatabase.GetAssetPath(tex);
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                }
            }
        }

        public static TextureImporter GetTextureImporter(Texture2D texture)
        {
            TextureImporter import = null;
            string assetPath = AssetDatabase.GetAssetPath(texture);
            return AssetImporter.GetAtPath(assetPath) as TextureImporter;
        }


        private static void GeneratePackTexture( CNode root, out PackTextureAttrSet outPackTextureAttrSet)
        {
            outPackTextureAttrSet = null; 
            if( root != null )
            {
                outPackTextureAttrSet = ScriptableObject.CreateInstance<PackTextureAttrSet>()  ;
                outPackTextureAttrSet.texVertexAttrList = new List<TextureVertexAttr>();
                //后面的格式指定
                outPackTextureAttrSet.packTexture = new Texture2D(root.refTextureInfo.blockDetail.rect.w, root.refTextureInfo.blockDetail.rect.h , TextureFormat.RGBA32 , false );
                SetDefaultColor(ref outPackTextureAttrSet.packTexture, mDefaultColor); 

                GeneratePackTextureImpl(root, ref outPackTextureAttrSet);
            }
        }

        private static void SetDefaultColor(  ref Texture2D tex , Color color )
        {
            if( tex != null  )
            {
                SetTextureReadble(tex,true); 
                for(int i = 0; i < tex.width; ++i)
                {
                    for(int j = 0; j < tex.height; ++j)
                    {
                        tex.SetPixel(i, j, color); 
                    }
                }

                tex.Apply(); 
                SetTextureReadble(tex, false);
            }
        }


        private static void GeneratePackTextureImpl(CNode node, ref PackTextureAttrSet packTextureAttrSet)
        {
            if( node != null 
                && packTextureAttrSet != null 
                && packTextureAttrSet.packTexture)
            {
                if( node.IsFilled )
                {
                    TextureVertexAttr attri = node.refTextureInfo;
                    CalcVertexAtrribute(
                        packTextureAttrSet.packTexWidth,
                        packTextureAttrSet.packTexHeight,
                        ref attri
                        );

                    packTextureAttrSet.texVertexAttrList.Add( attri );
                    Fill2PackTexture(packTextureAttrSet.packTexture, attri);         
                }
                if( node.leftChild != null  )
                {
                    GeneratePackTextureImpl(node.leftChild,ref packTextureAttrSet); 
                }
                if (node.rightChild != null)
                {
                    GeneratePackTextureImpl(node.rightChild, ref packTextureAttrSet);
                }
            }
        }

        private static void Fill2PackTexture( Texture2D packTex , TextureVertexAttr vertexAttr )
        {
            if( packTex != null 
                && vertexAttr.refTexture != null )
            {
                Texture2D refTex = vertexAttr.refTexture;
                
                if ( vertexAttr.blockDetail.IsFilped )
                {
                    //非得这样填充才是对的，我也不知道为啥
                    for (int i = 0; i < refTex.width; ++i)
                    {
                        for (int j = 0; j < refTex.height; ++j)
                        {
                            Color color = refTex.GetPixel(i, j);
                            if( i < vertexAttr.blockDetail.rect.h 
                                && j < vertexAttr.blockDetail.rect.w )
                            {
                                packTex.SetPixel(vertexAttr.blockDetail.rect.x + j ,vertexAttr.blockDetail.rect.y + i,color); 
                            }
                        }
                    }

                }
                else
                {
                    Color[] colors = refTex.GetPixels();
                    packTex.SetPixels(
                        vertexAttr.blockDetail.rect.x ,
                        vertexAttr.blockDetail.rect.y ,
                        vertexAttr.blockDetail.rect.w ,
                        vertexAttr.blockDetail.rect.h ,
                        colors
                        );
                }
            }     
        }


        private static void CalcVertexAtrribute(
            int packTexWidth,    //图集大小
            int packTexHeight,
            ref TextureVertexAttr vertexAttr
            )
        {
            if( !vertexAttr.IsVaild )
            {
                return; 
            }

            if (vertexAttr.refTexture == null)
            {
                return;
            }

            int x = vertexAttr.blockDetail.rect.x ;
            int y = vertexAttr.blockDetail.rect.y ;

            int texWidth = vertexAttr.blockDetail.rect.w ;
            int texHeight = vertexAttr.blockDetail.rect.h ;

            int halfPackTexWidth = packTexWidth / 2;
            int halfPackTexHeight = packTexHeight / 2;

            //输出时，纹理四个角的相对位置
            Vector2 packTextureBottomLeft = new Vector2(-halfPackTexWidth, -halfPackTexHeight);
            Vector2 packTextureTopLeft = new Vector2(-halfPackTexWidth, halfPackTexHeight);
            Vector2 packTextureTopRight = new Vector2(halfPackTexWidth, halfPackTexHeight);
            Vector2 packTextureBottomRight = new Vector2(halfPackTexWidth, -halfPackTexHeight);

            //顶点位置
            if (vertexAttr.blockDetail.IsFilped)
            {
                Vector3 texBottomLeft = new Vector3(x - halfPackTexWidth, y - halfPackTexHeight, 0);

                //填充顶点位置
                vertexAttr.blockDetail.posBL = texBottomLeft;
                vertexAttr.blockDetail.posTL = new Vector3(texBottomLeft.x, texBottomLeft.y + texHeight);       //top-left ;
                vertexAttr.blockDetail.posTR = new Vector3(texBottomLeft.x + texWidth, texBottomLeft.y + texHeight);
                vertexAttr.blockDetail.posBR = new Vector3(texBottomLeft.x + texWidth, texBottomLeft.y);

                Vector2 uvBottomLeft = new Vector2((float)x / (float)packTexWidth, (float)y / (float)packTexHeight);
                Vector2 uvTopRight = new Vector2((float)(x + texWidth) / (float)packTexWidth, (float)(y + texHeight) / (float)packTexHeight);

                //注意，这个是翻转了的
                Vector2 ucTopLeft = new Vector2((float)uvTopRight.x, (float)uvBottomLeft.y);
                Vector2 uvBottomRight = new Vector2((float)uvBottomLeft.x, (float)uvTopRight.y);

                vertexAttr.blockDetail.uvBL = uvBottomLeft;
                vertexAttr.blockDetail.uvTL = ucTopLeft;
                vertexAttr.blockDetail.uvTR = uvTopRight;
                vertexAttr.blockDetail.uvBR = uvBottomRight;

            }
            else
            {
                //填充顶点位置
                Vector3 texBottomLeft = new Vector3(x - halfPackTexWidth, y - halfPackTexHeight, 0 ); 

                vertexAttr.blockDetail.posBL = texBottomLeft ;
                vertexAttr.blockDetail.posTR = new Vector3(texBottomLeft.x + texWidth, texBottomLeft.y + texHeight);
                vertexAttr.blockDetail.posTL = new Vector3(texBottomLeft.x, texBottomLeft.y + texHeight);       //top-left ;
                vertexAttr.blockDetail.posBR = new Vector3(texBottomLeft.x + texWidth, texBottomLeft.y);

                //uv使用的采样纹理
                Vector2 uvBottomLeft = new Vector2((float)x / (float)packTexWidth, (float)y / (float)packTexHeight);
                Vector2 uvTopRight = new Vector2((float)(x + texWidth) / (float)packTexWidth, (float)(y + texHeight) / (float)packTexHeight);
                Vector2 ucTopLeft = new Vector2((float)uvBottomLeft.x, (float)uvTopRight.y);
                Vector2 uvBottomRight = new Vector2((float)uvTopRight.x, (float)uvBottomLeft.y);

                vertexAttr.blockDetail.uvBL = uvBottomLeft;
                vertexAttr.blockDetail.uvTL = ucTopLeft;
                vertexAttr.blockDetail.uvTR = uvTopRight;
                vertexAttr.blockDetail.uvBR = uvBottomRight;
            }
        }


        //随便的算法，不是最优的
        public static  CNode GenerateRootNode( Texture2D[] splitTexs ,int packTexWidth, int packTexHeight )
        {
            CNode rootNode = null; 
            if( splitTexs != null )
            {
                //统计一下所有图片的总像素数量
                //找出最长的宽和高
                int pixelCnt = 0;
                int maxWidth = 0;
                int maxHeight = 0; 
                for(int i = 0; i < splitTexs.Length ; ++i)
                {
                    int tWidth = splitTexs[i].width;
                    int tHeight = splitTexs[i].height;
                    pixelCnt += tWidth * tHeight ; 

                    if( tWidth > maxWidth )
                    {
                        maxWidth = tWidth; 
                    }

                    if( tHeight > maxHeight )
                    {
                        maxHeight = tHeight; 
                    }
                }

                int packTexSize = packTexWidth * packTexHeight  ;
                if( packTexSize >= pixelCnt )
                {
                    if ((packTexWidth >= maxWidth && packTexHeight >= maxHeight) ||
                          (packTexWidth >= maxHeight && packTexHeight >= maxWidth))
                    {
                        rootNode = new CNode();
                        rootNode.refTextureInfo.blockDetail.rect.Set(0, 0, packTexWidth, packTexHeight);
                    }
                    else
                    {
                        Debug.LogError("width or height too small");
                    }
                }
                else
                {
                    Debug.LogError("Size too small"); 
                }
            }
            return rootNode; 
        } 


    }
}
