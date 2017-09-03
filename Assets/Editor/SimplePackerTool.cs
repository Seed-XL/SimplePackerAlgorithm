using UnityEngine;
using UnityEditor;
using Assets.UI.SimplePacker;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;

namespace Assets.EditorUI
{
    public class SimplePackerTool : EditorWindow
    {
        private static GUIStyle splitter;
        private static readonly Color splitterColor = new Color(0, 0f, 0f);

        static readonly string RootGroupStyleName = "GroupBox";
        static readonly string SubGroupStyleName = "ObjectFieldThumb";
        static readonly string ToolTitleStyleName = "MeTransOffRight";

        //
        private static int mSpriteCount = 1;
        private static Sprite[] mRawSprites = null;

        //
        private static string mPackName;
        private static string mOutputPath;
        private static string mInputPath;
        private static Texture2D mPackTexture;
        private static PackTextureAttrSet mPackTextureAttrSet;
        private static TextureImporterFormat mPackTextureFormat = TextureImporterFormat.RGBA32; 

        private static SimplePacker.enPOTSizeType mWidthType = SimplePacker.enPOTSizeType.POT_32 ;
        private static SimplePacker.enPOTSizeType mHeightType = SimplePacker.enPOTSizeType.POT_32 ; 


        [MenuItem("GameTools/UI/SimplePackerTool")]
        static void Init()
        {
            SimplePackerTool window = EditorWindow.GetWindow<SimplePackerTool>("SimplePackerTool");
            window.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.BeginVertical(RootGroupStyleName);

            Splitter();

            DoShowOutput(); 

            Splitter();

            DoPackSprites();

            Splitter();

            DoPackSpritesFolder(); 

            Splitter();

            EditorGUILayout.EndVertical();
        }

        void CreateSpriteField(string label, ref Sprite outSprite)
        {
            EditorGUILayout.BeginHorizontal();
            outSprite = EditorGUILayout.ObjectField(label, outSprite, typeof(Sprite), true) as Sprite;
            EditorGUILayout.EndHorizontal();
        }

        void CreateStringField(string label, ref string outString)
        {
            EditorGUILayout.BeginHorizontal();
            outString = EditorGUILayout.TextField(label, outString);
            EditorGUILayout.EndHorizontal();
        }


        void CreateTextureField(string label, ref Texture2D outTexture)
        {
            EditorGUILayout.BeginHorizontal();
            outTexture = EditorGUILayout.ObjectField(label, outTexture, typeof(Texture2D), true) as Texture2D;
            EditorGUILayout.EndHorizontal();
        }


        void CreateIntField(string label, ref int outCnt)
        {
            EditorGUILayout.BeginHorizontal();
            outCnt = EditorGUILayout.IntField(label, outCnt);
            EditorGUILayout.EndHorizontal();
        }


        void CreateDropTextField(string label, ref string pathStr)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(label);
            var rect = EditorGUILayout.GetControlRect();
            pathStr = EditorGUI.TextField(rect, pathStr);

            //如果鼠标正在拖拽中或拖拽结束时，并且鼠标所在位置在文本输入框内
            if ((Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragExited) && rect.Contains(Event.current.mousePosition))
            {
                //改变鼠标的外表
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                {
                    if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                    {
                        pathStr = DragAndDrop.paths[0];
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }


        private static void WriteResult(
            string folder ,  
            string fileName ,
            TextureImporterFormat texImportFormat ,
            ref PackTextureAttrSet packTextureAttrSet
        )
        {
            if (string.IsNullOrEmpty(folder)
                || string.IsNullOrEmpty(fileName)
                || packTextureAttrSet == null 
                || packTextureAttrSet.packTexture == null 
                || packTextureAttrSet.texVertexAttrList == null 
                || packTextureAttrSet.texVertexAttrList.Count == 0 )
            {
                return;
            }

           
            if( !Directory.Exists(folder) )
            {
                Directory.CreateDirectory(folder); 
            }

            //写纹理
            string texPath = string.Format("{0}/{1}.png", folder, fileName);
            //写纹理
            File.WriteAllBytes(texPath, packTextureAttrSet.packTexture.EncodeToPNG());
            AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            //关联
            Texture2D tPackTexture = AssetDatabase.LoadAssetAtPath(texPath, typeof(Texture2D)) as Texture2D;
            if (tPackTexture != null)
            {
                //设置为Sprite
                TextureImporter importer = SimplePacker.GetTextureImporter(tPackTexture);
                TextureImporterSettings settings = SimplePacker.GetTextureImporterSettings(tPackTexture);
                if (importer != null
                    && settings != null)
                {
                    settings.ApplyTextureType(TextureImporterType.Advanced, false);
                    SimplePacker.SetTextureImporterSettings(tPackTexture, settings);

                    //要来回设置一下
                    settings.ApplyTextureType(TextureImporterType.Sprite, false);
                    settings.textureFormat = texImportFormat ; 
                    SimplePacker.SetTextureImporterSettings(tPackTexture, settings);
                }

                packTextureAttrSet.packTexture = tPackTexture;
                packTextureAttrSet.packSprite = AssetDatabase.LoadAssetAtPath(texPath, typeof(Sprite)) as Sprite;
            }

            string attrSetPath = string.Format("{0}/{1}.asset", folder, fileName);
            //存在就更新，不能创建，会丢失引用
            PackTextureAttrSet tPacktextureAttrSet = AssetDatabase.LoadAssetAtPath(attrSetPath, typeof(PackTextureAttrSet)) as PackTextureAttrSet;
            if (tPacktextureAttrSet != null)
            {
                tPacktextureAttrSet.CopyForm(packTextureAttrSet);
                //修改引用为已经存在的Asset文件
                packTextureAttrSet = tPacktextureAttrSet;
            }
            else
            {
                AssetDatabase.CreateAsset(packTextureAttrSet,attrSetPath);
            }
        }


        void CreatePackTextureAttrSetField(string label, ref PackTextureAttrSet outAttrSet)
        {
            EditorGUILayout.BeginHorizontal();
            outAttrSet = EditorGUILayout.ObjectField(label, outAttrSet, typeof(PackTextureAttrSet), true) as PackTextureAttrSet;
            EditorGUILayout.EndHorizontal();
        }


        void CreateAtlasEnumField(string label, ref SimplePacker.enPOTSizeType sizeType )
        {
            EditorGUILayout.BeginHorizontal();
            sizeType = (SimplePacker.enPOTSizeType)EditorGUILayout.EnumPopup(label ,sizeType) ;
            EditorGUILayout.EndHorizontal();
        }

        void CreatePackTextureImportFormatField(  string label , ref TextureImporterFormat importType )
        {
            EditorGUILayout.BeginHorizontal();
            importType = (TextureImporterFormat)EditorGUILayout.EnumPopup(label,importType); 
            EditorGUILayout.EndHorizontal(); 
        }


        public static void ShowTitle(string titleContent)
        {
            GUIStyle fontStyle = new GUIStyle();
            fontStyle.font = (Font)EditorGUIUtility.Load("EditorFont.TTF");
            fontStyle.fontSize = 15;
            fontStyle.alignment = TextAnchor.MiddleCenter;
            fontStyle.normal.textColor = Color.white;
            fontStyle.hover.textColor = Color.white;

            EditorGUILayout.BeginVertical(ToolTitleStyleName);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("",
                        titleContent, fontStyle, GUILayout.Height(15));
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }



        public void Splitter(float thickness = 5)
        {
            if (splitter == null)
            {
                splitter = new GUIStyle();
                splitter.normal.background = EditorGUIUtility.whiteTexture;
                splitter.stretchWidth = true;
                splitter.margin = new RectOffset(7, 7, 7, 7);
            }

            Rect position = GUILayoutUtility.GetRect(GUIContent.none, splitter, GUILayout.Height(thickness));

            if (Event.current.type == EventType.Repaint)
            {
                Color restoreColor = GUI.color;
                GUI.color = splitterColor;
                splitter.Draw(position, false, false, false, false);
                GUI.color = restoreColor;
            }
        }


        private void DoShowOutput()
        {
            GUILayout.BeginVertical(SubGroupStyleName);

            ShowTitle("输出");

            CreateDropTextField("输出文件夹：", ref mOutputPath);
            CreateStringField("输出的名字：", ref mPackName);

            GUILayout.BeginHorizontal(SubGroupStyleName);
            CreatePackTextureImportFormatField("输出纹理的格式：",ref mPackTextureFormat); 
            CreateAtlasEnumField("输出Pack Texture的宽：",ref mWidthType);
            CreateAtlasEnumField("输出Pack Texture的高：", ref mHeightType);
            GUILayout.EndHorizontal();

            CreateTextureField("输出的Pack Texture：", ref mPackTexture);
            CreatePackTextureAttrSetField("输出的Asset文件：", ref mPackTextureAttrSet);



            GUILayout.EndVertical();
        }

        private void DoPackSprites()
        {
            GUILayout.BeginVertical(SubGroupStyleName);

            ShowTitle("指定Pack Sprite");

            CreateIntField("Sprite数目：", ref mSpriteCount);
            if (mSpriteCount <= 0)
            {
                mRawSprites = null;
            }
            else
            {
                if (mRawSprites == null || mRawSprites.Length != mSpriteCount)
                {
                    Sprite[] tSprites = new Sprite[mSpriteCount];
                    for (int i = 0; i < mSpriteCount && mRawSprites != null && i < mRawSprites.Length; ++i)
                    {
                        tSprites[i] = mRawSprites[i];
                    }
                    mRawSprites = tSprites;
                }
            }

            for (int i = 0; i < mSpriteCount; ++i)
            {
                GUILayout.BeginHorizontal(SubGroupStyleName);

                CreateSpriteField("Sprite：", ref mRawSprites[i]);
          
                GUILayout.EndHorizontal();
            }

            string btnLabel = string.Format("==Pack {0} Sprites==",mSpriteCount); 
            if (GUILayout.Button(btnLabel) && mSpriteCount > 0 )
            {
                if (string.IsNullOrEmpty(mOutputPath))
                {
                    ShowNotification(new GUIContent("请指定输出路径"));
                    return;
                }

                if ( string.IsNullOrEmpty(mPackName))
                {
                    ShowNotification(new GUIContent("请指定输出文件的名字"));
                    return;
                }

                SimplePacker.Pack(mRawSprites,(int)mWidthType,(int)mHeightType, out mPackTextureAttrSet);
                WriteResult(mOutputPath, mPackName,mPackTextureFormat , ref mPackTextureAttrSet);

                AssetDatabase.SaveAssets(); 
                AssetDatabase.Refresh();

                //显示引用的图片
                mPackTexture = mPackTextureAttrSet.packTexture; 
            }

            GUILayout.EndVertical();
        }


        private void DoPackSpritesFolder()
        {
            GUILayout.BeginVertical(SubGroupStyleName);

            ShowTitle("指定文件夹Pack Sprite");
           
            CreateDropTextField("Sprites文件夹：", ref mInputPath);

            string[] tSpriteAssets = AssetDatabase.FindAssets("t:Sprite", new string[] { mInputPath } );
            if( tSpriteAssets != null 
                && tSpriteAssets.Length > 0 )
            {
                int tCnt = tSpriteAssets.Length; 
                string btnLabel = string.Format("==Pack {0} Sprites==",tCnt);
                if ( GUILayout.Button(btnLabel) )
                {
                    if (string.IsNullOrEmpty(mOutputPath))
                    {
                        ShowNotification(new GUIContent("请指定输出路径"));
                        return;
                    }

                    if (string.IsNullOrEmpty(mPackName))
                    {
                        ShowNotification(new GUIContent("请指定输出文件的名字"));
                        return;
                    }

                    Sprite[] tSprites = new Sprite[tCnt];
                    int i = 0; 
                    foreach (string guid in tSpriteAssets)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                        {
                            Sprite sprite = asset as Sprite;
                            if ( sprite != null )
                            {
                                tSprites[i++] = sprite; 
                            }
                        }
                    }

                    SimplePacker.Pack(tSprites, (int)mWidthType, (int)mHeightType, out mPackTextureAttrSet);
                    WriteResult(mOutputPath, mPackName,mPackTextureFormat, ref mPackTextureAttrSet);

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    //显示引用的图片
                    if( mPackTextureAttrSet != null )
                    {
                        mPackTexture = mPackTextureAttrSet.packTexture;
                    }
                }
            }
            
            GUILayout.EndVertical();
        }

    }

}

