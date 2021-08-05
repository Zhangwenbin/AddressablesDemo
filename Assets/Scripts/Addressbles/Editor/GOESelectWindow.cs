#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Runtime.Remoting;
namespace EG
{
    public class MUSelectWindow : EditorWindow
    {
        //当前编辑的类型名字//

        protected string m_strNew = "";                                                     //新建文件名//
        protected string m_strFilter = "";                                                  //文件名过滤器//
        protected List<string> m_filesFiltered = new List<string>();                //过滤后的文件名//
        protected List<string> m_filesFilteredDisp = new List<string>();        //过滤后的文件名，用于显示，不包含路径//
        protected int m_indexSelectedFile = -1;                                         //当前选中的文件索引//
        protected int m_SelectedFileName = -1;                                          //当前选中的文件索引//

        protected object mCurEditingObject = null;                      //当前编辑的SI//

        MUPackUIHelperV5 Helper = new MUPackUIHelperV5();

        void SelectType()
        {
            //重新搜索//
            DoSearch(m_strFilter);

            onSelectFile(-1);
        }
        protected virtual void ShowInEditAndPropertyWindow()
        {

            if (mCurEditingObject != null)
            {
                if ((Application.isPlaying && Helper.DoesNeedStarted()) || (!Helper.DoesNeedStarted()))
                {

                    // open Property Window 
                    JsonObjectPropertyWindow propertyWindow = EditorWindow.GetWindow(typeof(JsonObjectPropertyWindow), false) as JsonObjectPropertyWindow;
                    propertyWindow.SetCurStringInterface(mCurEditingObject, Helper);
                }

            }
        }



        string GetCurrentSelectFileName()
        {
            if (null != m_filesFiltered && m_filesFiltered.Count > 0 && m_indexSelectedFile >= 0 && m_indexSelectedFile < m_filesFiltered.Count && null != m_filesFiltered[m_indexSelectedFile] && m_filesFiltered[m_indexSelectedFile].Length > 0)
                return m_filesFiltered[m_indexSelectedFile];
            else
                return null;
        }

        protected void OnFileChanged()
        {
            AssetDatabase.Refresh();
        }


        protected virtual void onNewFile(string m_strNew)
        {
            //m_strNew = "默认act";
            string strFullName = Helper.GetSearchDir() + m_strNew + Helper.GetFileExt();
            strFullName = strFullName.Replace("\\", "/");
            //如文件已存在则提醒//
            FileInfo fi = new FileInfo(strFullName);
            bool overwrite = true;
            if (string.IsNullOrEmpty(m_strNew))
            {
                overwrite = false;
                EditorUtility.DisplayDialog("错误",
                    "请先输入文件名!", "OK");
            }
            if (fi.Exists)
            {
                overwrite = false;
                overwrite = EditorUtility.DisplayDialog("信息",
                    "文件 : " + m_strNew + Helper.GetFileExt() + "已经存在\r\n确定要覆盖吗？",
                    "是", "否");
            }
            if (overwrite)
            {
                //构造对象//
                mCurEditingObject = Helper.OnNew();
                if (null != mCurEditingObject)
                {
                    //写入文件//
                    Helper.OnSave(mCurEditingObject, strFullName);

                    OnFileChanged();

                    //重新搜索//
                    DoSearch(m_strNew);
                    onSelectFile(strFullName);
                }
            }
        }

        protected void onSaveFile()
        {
            string strFullName = GetCurrentSelectFileName();
            if (null != strFullName)
            {
                Helper.OnSave(mCurEditingObject, strFullName);
                OnFileChanged();
            }
        }

        protected void onSaveOther()
        {
            //显示保存窗口//
            string strFullName = EditorUtility.SaveFilePanel("Save As...",
                Path.GetFullPath(Helper.GetSearchDir()), Path.GetFileName(GetCurrentSelectFileName()),
                "");
            if (null != strFullName && strFullName.Length > 0)
            {
                Helper.OnSave(mCurEditingObject, strFullName);
                OnFileChanged();

                //重新搜索//
                DoSearch(m_strFilter);
                onSelectFile(strFullName);
            }
        }

        private void OnDeleteFile()
        {
            string strCurFileName = GetCurrentSelectFileName();
            if (null != strCurFileName)
            {
                FileInfo fi = new FileInfo(strCurFileName);
                if (fi.Exists && EditorUtility.DisplayDialog("警告",
                        "文件 : " + strCurFileName + "\r\n确定要删除吗？",
                        "是", "否"))
                {
                    AssetDatabase.DeleteAsset(strCurFileName);
                    Helper.OnDelete(strCurFileName);
                    OnFileChanged();
                    DoSearch(m_strFilter);
                    onSelectFile(-1);

                    EditorUtility.DisplayDialog("信息",
                        "文件已删除 : " + strCurFileName,
                        "确定");
                }
            }
        }

        void OnGUI()
        {

            GUILayoutOption[] voptionsNoMaxWidth = {
            GUILayout.MaxWidth (10000),
            GUILayout.ExpandHeight (true),
            GUILayout.ExpandWidth (true)};

            //选择区//
            GUILayout.BeginVertical();

            //类型//
            GUILayout.Label("编辑器类型");

            //新建//
            if (Helper.CanNew())
            {
                GUILayout.Label("新建");
                GUILayout.BeginHorizontal();

                m_strNew = EditorGUILayout.TextField(m_strNew);
                if (GUILayout.Button("新建配置", GUILayout.Width(60)))
                {
                    onNewFile(m_strNew);
                }
                GUILayout.EndHorizontal();
            }

            if (mCurEditingObject != null)
            {

                GUILayout.BeginHorizontal();
                //保存//
                if (Helper.CanSave())
                {
                    if (GUILayout.Button("保存配置"))
                    {
                        onSaveFile();
                    }
                }
                //另存为//
                if (Helper.CanNew())
                {
                    if (GUILayout.Button("另存配置"))
                    {
                        onSaveOther();
                    }
                }
                //删除//
                if (Helper.CanDelete())
                {
                    if (GUILayout.Button("删除"))
                    {
                        OnDeleteFile();
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(10);
                Helper.MakeEditUI(mCurEditingObject);
            }
            else
            {
                SelectType();
            }
            //选择//
            GUILayout.Label("选择");

            GUILayout.BeginHorizontal();

            string strFilter = EditorGUILayout.TextField(m_strFilter);

            if (GUILayout.Button("搜索配置", GUILayout.Width(60)) || strFilter != m_strFilter)
            {
                string name = GetCurrentSelectFileName();
                DoSearch(strFilter);
                if (name != null)
                    onSelectFile(name);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            int idx = GUILayout.SelectionGrid(m_indexSelectedFile, m_filesFilteredDisp.ToArray(), 1);
            if (idx != m_indexSelectedFile)
            {
                onSelectFile(idx);
            }

            GUILayout.EndVertical();

        }


        protected void onSelectFile(string name)
        {
            name = Path.GetFileNameWithoutExtension(name);
            onSelectFile(m_filesFilteredDisp.IndexOf(name));
        }

        private void onSelectFile(int idx)
        {
            string selectFileName = null;
            //加载并设为当前StringInterface//
            if (null != m_filesFiltered && m_filesFiltered.Count > 0)
            {
                m_indexSelectedFile = idx;
                //选中响应//
                selectFileName = GetCurrentSelectFileName();
                if (null != selectFileName)
                    mCurEditingObject = Helper.OnSelect(selectFileName);
                else
                    mCurEditingObject = null;
            }
            //显示到编辑窗口和属性窗口//
            //如此行在大括号外面则会一直更新，导致Search框无法输入文字//
            int count = 0;
            if (selectFileName != null && selectFileName.Length > 12)
            {
                count = selectFileName.Length - 12;
                selectFileName = selectFileName.Substring(count, 12);
            }

            ShowInEditAndPropertyWindow();
        }

        protected virtual void DoSearch(string strFilter)
        {
            m_strFilter = strFilter;
            string[] filenames = Directory.GetFiles(Helper.GetSearchDir(), "*" + Helper.GetFileExt(), SearchOption.AllDirectories);
            m_filesFiltered = new List<string>();
            m_filesFilteredDisp.Clear();
            for (int i = 0; i < filenames.Length; i++)
            {
                string str = filenames[i];
                if (str.Contains(m_strFilter))
                {
                    m_filesFiltered.Add(str.Replace("\\", "/"));
                }
            }
            m_filesFiltered.Sort(Helper.SortFiles);
            foreach (string str in m_filesFiltered)
            {
                m_filesFilteredDisp.Add(Path.GetFileNameWithoutExtension(str));
            }
        }
    }
#endif
}