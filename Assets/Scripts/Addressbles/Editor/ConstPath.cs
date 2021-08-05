using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EG
{

    public static class ConstPath
    {
        public static string Pack_UIGeneratorSrcPath = "Assets/Res/Gui/";                                                   //打包UI 原路径

        public static string Pack_V5HelperSearchDir = "Assets/BundleConfig/";                        //Unity5打包Helper搜索路径

        public static string Pack_LuaSrcPath = "../lua/";                                                                //Unity5打包 Lua原路径
        public static string Pack_LuaOutPath = "Assets/Lua/out/";                                                           //Unity5打包 Lua输出路径
        public static string Pack_LuaGeneratePath = "Assets/Lua/generate/";                                                 //Unity5打包 Lua Generate路径

        //Story目标路径
        public static string Pack_LuaPackTxtPath = ConstPath.Pack_V5HelperSearchDir + "lua.pack.txt";               //lua.pack.txt路径

        public static string Pack_CharacterPackTxtPath = ConstPath.Pack_V5HelperSearchDir + "character.pack.txt";   //character.pack.txt路径
        public static string Pack_UIPackTxtPath = ConstPath.Pack_V5HelperSearchDir + "ui.pack.txt";                 //ui.pack.txt路径

        public static string Pack_ShadervariantsMat = "Assets/Res/shaderlib/Materials/";                                    //Materials
        public static string Pack_SceneExport = "Assets/Res/Scene/Export";
        public static string Pack_sceneBasic = "Assets/Res/Scene/Basic";

        public const string Pack_StreamAssetsDir = "Assets/StreamingAssets";
        public const string Pack_StreamAssetsDiriOS = "Assets/StreamingAssets";

        public const string Pack_ConfigDir = "Assets/Res/Config";



        public const string Scene_SceneSrcDir = "Assets/Res/Scene";
   


    }

}
