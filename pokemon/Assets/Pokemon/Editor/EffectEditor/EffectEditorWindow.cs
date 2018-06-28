using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class EffectEditorWindow : EditorWindow
{
    [MenuItem("Tools/特效编辑器")]
    public static void Create()
    {
        EffectEditorWindow window = EditorWindow.GetWindow<EffectEditorWindow>();
        window.maxSize = new Vector2(WINDOW_WIDTH, 750);
        window.minSize = new Vector2(WINDOW_WIDTH, 750);

        window.m_defaultBgClr = GUI.backgroundColor;
        window.m_defaultConClr = GUI.contentColor;

        window.m_styleTittle = new GUIStyle(EditorStyles.textField);
        window.m_styleTittle.fontSize = 30;
        window.m_styleTittle.alignment = TextAnchor.MiddleCenter;

        window.m_styleFrame = new GUIStyle(EditorStyles.textField);
        //window.m_styleFrame.padding = new UnityEngine.RectOffset(1, 1, 1, 1);
        //window.m_styleFrame.margin = new UnityEngine.RectOffset(1, 1, 1, 1);

        window.m_styleFrameFont = new GUIStyle(EditorStyles.label);
        window.m_styleFrameFont.alignment = TextAnchor.MiddleCenter;
        window.m_styleFrameFont.normal.textColor = Color.white;
    }

    private Color m_defaultBgClr;       // 默认背景颜色
    private Color m_defaultConClr;      // 默认内容颜色

    private GUIStyle m_styleTittle;     // 标题风格
    private GUIStyle m_styleFrame;      // 帧风格
    private GUIStyle m_styleFrameFont;  // 帧字体风格
    private Vector2 m_frameScroPos;     // 帧滚动条位置
    private Vector2 m_assetScroPos;     // 资源滚动条位置
    private Vector2 m_frameDataScroPos; // 帧数据滚动位置

    private int m_effectId;             // 特效id
    private EffectData m_effectData;    // 特效数据
    private EffectFrameData m_frameData;// 当前选中的帧

    // 资源id次数映射
    // key => 资源id
    // val => id使用次数
    private Dictionary<int, int> m_dicAssetNum = new Dictionary<int, int>();

    // 帧id次数映射
    // key => 帧的编号
    // val => 使用次数
    private Dictionary<int, int> m_dicFrameNum = new Dictionary<int, int>();

    private const int WINDOW_WIDTH = 1100;  // 窗口宽度
    private readonly Color32 COLOR_ORANGE = new Color32(255, 180, 121, 255);

    private void OnGUI()
    {
        GUILayout.BeginHorizontal();

        DrawMenu();

        GUILayout.EndHorizontal();

        if (m_effectData == null)
            return;

        m_dicAssetNum.Clear();
        for(int i = 0; i < m_effectData.aA.Length; ++i)
        {
            EffectAssetData assetData = m_effectData.aA[i];
            if (m_dicAssetNum.ContainsKey(assetData.i))
                m_dicAssetNum[assetData.i]++;
            else
                m_dicAssetNum.Add(assetData.i, 1);
        }

        m_dicFrameNum.Clear();
        for(int i = 0; i < m_effectData.aF.Length; ++i)
        {
            EffectFrameData frameData = m_effectData.aF[i];
            if (m_dicFrameNum.ContainsKey(frameData.n))
                m_dicFrameNum[frameData.n]++;
            else
                m_dicFrameNum.Add(frameData.n, 1);
        }

        GUILayout.BeginVertical();

        DrawSetting();

        GUILayout.BeginHorizontal();

        DrawAsset();
        DrawFrame();

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

        ResortFrame();

        if (EffectEditorUI.Inst != null)
        {
            if (m_frameData != null && !EffectEditorUI.Inst.IsPlay)
            {
                PlayCurrentFrame();
            }
        }
    }

    // 重新排序帧
    private void ResortFrame()
    {
        List<EffectFrameData> listFrameData = new List<EffectFrameData>(m_effectData.aF);
        listFrameData.Sort((a, b)=>{
            if (a.n > b.n)
                return 1;
            if (a.n < b.n)
                return -1;
            return 0;
        });

        m_effectData.aF = listFrameData.ToArray();
    }

    // 绘制菜单
    private void DrawMenu()
    {
        GUI.backgroundColor = Color.yellow;
        GUILayout.BeginHorizontal(new GUIStyle(EditorStyles.textField), GUILayout.Width(WINDOW_WIDTH), GUILayout.Height(32));
        GUI.backgroundColor = m_defaultBgClr;

        GUILayout.Label("特效ID:", GUILayout.Width(55));
        m_effectId = EditorGUILayout.IntField(m_effectId, GUILayout.Width(100));

        if (GUILayout.Button("新建", GUILayout.Height(30), GUILayout.Width(50)))
        {
            m_effectData = new EffectData();
            m_frameData = null;
        }

        if (GUILayout.Button("加载", GUILayout.Height(30), GUILayout.Width(50)))
        {
            string path = "EffectData/effectjson" + m_effectId;
            TextAsset ta = Resources.Load<TextAsset>(path);
            if (ta == null)
            {
                ShowNotification(new GUIContent("加载失败！没有对应特效的配置"));
                return;
            }

            m_effectData = Deserializer.Deserialize<EffectData>(ta.text);
            if (m_effectData == null)
            {
                ShowNotification(new GUIContent("加载失败！配置数据有问题"));
                return;
            }

            if (m_effectData.aF.Length > 0)
                m_frameData = m_effectData.aF[0];
            else
                m_frameData = null;
        }

        if (GUILayout.Button("保存", GUILayout.Height(30), GUILayout.Width(50)))
        {
            if (m_effectData == null)
            {
                ShowNotification(new GUIContent("保存失败！无效的技能"));
                return;
            }
            
            if(!CheckEffectDataIsValid())
            {
                ShowNotification(new GUIContent("保存失败！特效有报错"));
                return;
            }

            string path = Application.dataPath + "/Pokemon/Resources/EffectData/effectjson" + m_effectId + ".txt";
            string json = Serializer.Serialize(m_effectData);

            StreamWriter sw = new StreamWriter(path, false);
            sw.Write(json);
            sw.Dispose();
            sw.Close();

            AssetDatabase.Refresh();
            ShowNotification(new GUIContent("保存成功！"));
            return;
        }


        GUILayout.EndHorizontal();
    }

    // 绘制设置
    private void DrawSetting()
    {
        GUI.backgroundColor = COLOR_ORANGE;
        GUILayout.BeginHorizontal(new GUIStyle(EditorStyles.textField), GUILayout.Width(WINDOW_WIDTH), GUILayout.Height(32));
        GUI.backgroundColor = m_defaultBgClr;

        GUILayout.Label("帧率:", GUILayout.Width(55));
        m_effectData.r = EditorGUILayout.IntField(m_effectData.r, GUILayout.Width(100));
        GUILayout.Label("总播放时间:" + EffectEditorUtil.CalcEffectTime(m_effectData) + "s", GUILayout.Width(200));

        GUILayout.EndHorizontal();
    }

    // 绘引用
    private void DrawAsset()
    {
        GUI.backgroundColor = Color.black;
        GUILayout.BeginVertical(new GUIStyle(EditorStyles.textField), GUILayout.Width(250), GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
        GUI.backgroundColor = m_defaultBgClr;

        GUILayout.Label("资源管理", m_styleTittle, GUILayout.Width(250), GUILayout.Height(40));

        m_assetScroPos = GUILayout.BeginScrollView(m_assetScroPos, true, false, GUILayout.ExpandWidth(true)); // true, false, GUILayout.Width(200), GUILayout.Height(100));

        for (int i = 0; i < m_effectData.aA.Length; ++i)
        {
            EffectAssetData assData = m_effectData.aA[i];
            DrawAssetItem(assData);
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndScrollView();

        // +按钮
        if (GUILayout.Button("+", GUILayout.Height(40), GUILayout.Width(245)))
        {
            List<EffectAssetData> listAsset = new List<EffectAssetData>(m_effectData.aA);
            listAsset.Add(new EffectAssetData());

            m_effectData.aA = listAsset.ToArray();
        }

        GUILayout.EndVertical();
    }

    // 绘制帧
    private void DrawFrame()
    {
        GUI.backgroundColor = Color.black;
        GUILayout.BeginVertical(new GUIStyle(EditorStyles.textField), GUILayout.Width(WINDOW_WIDTH - 250), GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
        GUI.backgroundColor = m_defaultBgClr;
        m_frameScroPos = GUILayout.BeginScrollView(m_frameScroPos, true, false, GUILayout.Height(120), GUILayout.Width(WINDOW_WIDTH - 270)); // true, false, GUILayout.Width(200), GUILayout.Height(100));

        EffectFrameData[] arrFrameData = m_effectData.aF;
        GUILayout.BeginHorizontal();
        for (int i = 0; i < arrFrameData.Length; ++i)
        {
            EffectFrameData frameData = arrFrameData[i];

            if(CheckFrameIsValid(frameData))
                GUI.contentColor = Color.white;
            else
                GUI.contentColor = Color.red;

            GUILayout.Label((frameData.n + 1).ToString(), m_styleFrameFont, GUILayout.Width(20));
            GUI.contentColor = m_defaultConClr;
        }

        GUILayout.Space(20);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        for (int i = 0; i < arrFrameData.Length; ++i)
        {
            EffectFrameData frameData = arrFrameData[i];

            if (m_frameData == frameData)
                GUI.backgroundColor = Color.green;

            if (GUILayout.Button("", m_styleFrame, GUILayout.Width(20), GUILayout.Height(80)))
            {
                m_frameData = frameData;
            }

            GUI.backgroundColor = m_defaultBgClr;
        }

        GUILayout.Space(20);
        GUILayout.EndHorizontal();

        GUILayout.EndScrollView(); // 帧


        GUI.backgroundColor = Color.black;
        GUILayout.BeginHorizontal(new GUIStyle(EditorStyles.textField), GUILayout.Width(WINDOW_WIDTH - 250), GUILayout.Height(50));
        GUI.backgroundColor = m_defaultBgClr;

        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal(new GUIStyle(EditorStyles.textField), GUILayout.Width(WINDOW_WIDTH - 250), GUILayout.Height(32));

        if (m_frameData == null)
        {
            GUI.contentColor = Color.white;
            GUILayout.Label("当前未选中帧");
            GUI.contentColor = m_defaultConClr;
        }
        else
        {
            GUI.contentColor = Color.white;
            GUILayout.Label("当前帧：", GUILayout.Width(50));
            GUI.contentColor = m_defaultConClr;
            int no = EditorGUILayout.IntField(m_frameData.n+1, GUILayout.Width(100));
            if (no < 1)
                no = 1;

            m_frameData.n = no - 1;

            if(GUILayout.Button("播放", GUILayout.Width(50)))
            {
                if(!Application.isPlaying || EffectEditorUI.Inst == null)
                {
                    ShowNotification(new GUIContent("播放失败!,UNITY为在运行状态"));
                    return;
                }

                EffectEditorUI.Inst.Play(m_effectData);
            }
        }

        GUILayout.EndHorizontal();

        //if (m_frameData == null)
        //{
        //    GUILayout.Label("当前未选中帧", m_styleTittle, GUILayout.Width(WINDOW_WIDTH - 250), GUILayout.Height(32));
        //}
        //else
        //{
        //    GUILayout.BeginHorizontal();
        //    GUILayout.Label("当前选中第" + (m_frameData.frameNo + 1) + "帧:", GUILayout.Width(WINDOW_WIDTH - 250), GUILayout.Height(32));

        //    GUILayout.EndHorizontal();
        //    //GUILayout.Label("当前选中第" + (m_frameData.frameNo + 1) + "帧", m_styleTittle, GUILayout.Width(WINDOW_WIDTH - 250), GUILayout.Height(32));
        //}

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("添加显示"))
        {
            if (m_frameData == null)
            {
                ShowNotification(new GUIContent("添加失败！当前没选中帧"));
                return;
            }

            List<EffectImageData> listData = new List<EffectImageData>(m_frameData.aI);
            listData.Add(new EffectImageData());
            m_frameData.aI = listData.ToArray();
        }
        if (GUILayout.Button("添加新帧"))
        {
            List<EffectFrameData> listFrame = new List<EffectFrameData>(m_effectData.aF);
            int frameNum = listFrame.Count;
            EffectFrameData nFrameData = frameNum > 0 ? CopyFrameData(listFrame[frameNum-1]) : new EffectFrameData();
            
            if(frameNum > 0)
                nFrameData.n++;

            listFrame.Add(nFrameData);
            m_effectData.aF = listFrame.ToArray();
            m_frameData = nFrameData;
        }
        if (GUILayout.Button("复制选中帧到末尾"))
        {
            if (m_frameData == null)
            {
                ShowNotification(new GUIContent("复制失败！当前没选中帧"));
                return;
            }

            List<EffectFrameData> listFrame = new List<EffectFrameData>(m_effectData.aF);
            int frameNum = listFrame.Count;
            EffectFrameData nFrameData = frameNum > 0 ? CopyFrameData(m_frameData) : new EffectFrameData();

            if (frameNum > 0)
            {
                nFrameData.n = listFrame[frameNum - 1].n + 1;
            }

            listFrame.Add(nFrameData);
            m_effectData.aF = listFrame.ToArray();
            m_frameData = nFrameData;
        }
        if (GUILayout.Button("删除当前帧"))
        {
            if(m_frameData == null)
            {
                ShowNotification(new GUIContent("删除失败！当前没选中帧"));
                return;
            }

            List<EffectFrameData> listFrame = new List<EffectFrameData>(m_effectData.aF);
            int idx = listFrame.IndexOf(m_frameData);
            listFrame.Remove(m_frameData);

            m_effectData.aF = listFrame.ToArray();
            m_frameData = null;

            if(listFrame.Count > 0)
            {
                if (idx == 0)
                    m_frameData = listFrame[idx];
                else if (idx >= listFrame.Count - 1)
                    m_frameData = listFrame[listFrame.Count - 1];
                else
                    m_frameData = listFrame[idx];
            }
        }

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        // 帧数据
        m_frameDataScroPos = GUILayout.BeginScrollView(m_frameDataScroPos, true, false, GUILayout.Height(500), GUILayout.Width(WINDOW_WIDTH - 270)); // true, false, GUILayout.Width(200), GUILayout.Height(100));
        GUILayout.BeginHorizontal();
        if(m_frameData != null)
        {
            for(int i = 0; i < m_frameData.aI.Length; ++i)
            {
                EffectImageData imgData = m_frameData.aI[i];
                DrawFrameImageItem(imgData);
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.EndScrollView(); // 帧数据





        GUILayout.FlexibleSpace();
        GUILayout.EndVertical(); // 大

    }

    // 绘制资源图标
    // @assetData:资源数据
    private void DrawAssetItem(EffectAssetData assetData)
    {
        if (CheckAssetIsValid(assetData))
            GUI.backgroundColor = Color.green;
        else
            GUI.backgroundColor = Color.red;

        GUILayout.BeginVertical(new GUIStyle(EditorStyles.textField), GUILayout.Width(250), GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
        GUI.backgroundColor = m_defaultBgClr;

        GUILayout.BeginHorizontal();
        GUILayout.Label("资源id:", GUILayout.Width(55));
        assetData.i = EditorGUILayout.IntField(assetData.i, GUILayout.Width(100));

        if (GUILayout.Button("-", GUILayout.Width(20)))
        {
            List<EffectAssetData> listData = new List<EffectAssetData>(m_effectData.aA);
            listData.Remove(assetData);

            m_effectData.aA = listData.ToArray();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("资源:", GUILayout.Width(55));

        Texture2D tx2D = null;
        if(!string.IsNullOrEmpty(assetData.n))
            tx2D = Resources.Load<Texture2D>("EffectRaw/" + assetData.n);

        Texture2D nTx2D = (Texture2D)EditorGUILayout.ObjectField(tx2D, typeof(Texture2D), false, GUILayout.Width(120));
        if (nTx2D != null)
        {
            assetData.n = nTx2D.name;
            assetData.w = nTx2D.width;
            assetData.h = nTx2D.height;
        }
        else
        {
            assetData.n = null;
        }

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    // 绘制帧图像图标
    // @imgData:图像数据
    private void DrawFrameImageItem(EffectImageData imgData)
    {
        if (CheckImgIsValid(imgData.i))
            GUI.backgroundColor = Color.green;
        else
            GUI.backgroundColor = Color.red;

        GUILayout.BeginVertical(new GUIStyle(EditorStyles.textField), GUILayout.Width(200), GUILayout.Height(480)); // 图标
        GUI.backgroundColor = m_defaultBgClr;

        GUILayout.BeginHorizontal(); // 第1行
        GUILayout.Label("资源id:", GUILayout.Width(55));
        imgData.i = EditorGUILayout.IntField(imgData.i, GUILayout.Width(100));

        if (GUILayout.Button("-", GUILayout.Width(20)))
        {
            List<EffectImageData> listData = new List<EffectImageData>(m_frameData.aI);
            listData.Remove(imgData);

            m_frameData.aI = listData.ToArray();
        }

        GUILayout.EndHorizontal(); // 第1行


        GUILayout.BeginHorizontal(); // 第2行
        Vector2 pos = new Vector2(imgData.pX, imgData.pY);
        pos = EditorGUILayout.Vector2Field("位置：", pos);
        imgData.pX = pos.x;
        imgData.pY = pos.y;
        GUILayout.EndHorizontal(); // 第2行

        GUILayout.BeginHorizontal(); // 第3行
        Vector2 scale = new Vector2(imgData.sX, imgData.sY);
        scale = EditorGUILayout.Vector2Field("缩放：", scale);
        imgData.sX = scale.x;
        imgData.sY = scale.y;
        GUILayout.EndHorizontal(); // 第3行

        GUILayout.BeginHorizontal(); // 第4行
        imgData.r = EditorGUILayout.FloatField("旋转：", imgData.r);
        GUILayout.EndHorizontal(); // 第4行

        GUILayout.BeginHorizontal(); // 第5行
        Color32 clr = new Color32((byte)imgData.c[0], (byte)imgData.c[1], (byte)imgData.c[2], (byte)imgData.c[3]);
        clr = EditorGUILayout.ColorField("颜色：", clr);
        imgData.c[0] = clr.r;
        imgData.c[1] = clr.g;
        imgData.c[2] = clr.b;
        imgData.c[3] = clr.a;
        GUILayout.EndHorizontal(); // 第5行

        GUILayout.FlexibleSpace();


        GUILayout.BeginHorizontal(); // 最后1行

        if(GUILayout.Button("<"))
        {
            List<EffectImageData> listData = new List<EffectImageData>(m_frameData.aI);
            int idx = listData.IndexOf(imgData);

            if (idx == 0)
                return;

            listData.Remove(imgData);
            listData.Insert(idx - 1, imgData);
            m_frameData.aI = listData.ToArray();
        }

        GUILayout.Label("改层级", GUILayout.Width(40));

        if(GUILayout.Button(">"))
        {
            List<EffectImageData> listData = new List<EffectImageData>(m_frameData.aI);
            int idx = listData.IndexOf(imgData);

            if (idx == listData.Count - 1)
                return;

            listData.Remove(imgData);
            listData.Insert(idx + 1, imgData);
            m_frameData.aI = listData.ToArray();
        }

        GUILayout.EndHorizontal(); // 最后1行

        GUILayout.EndVertical(); // 图标
    }

    // 检测资源数据是否有效
    // @assetData:资源数据
    // return:有效返回ture；否则false
    private bool CheckAssetIsValid(EffectAssetData assetData)
    {
        if (string.IsNullOrEmpty(assetData.n))
            return false;

        return CheckImgIsValid(assetData.i);
    }

    // 检测图片是否有效
    // @imgId:图片id
    // return:有效返回true；否则false
    private bool CheckImgIsValid(int imgId)
    {
        if (!m_dicAssetNum.ContainsKey(imgId))
            return false;

        return m_dicAssetNum[imgId] == 1;
    }

    // 检测帧是否有效
    // @frameData:帧数据
    // return:有效返回true；否则false
    private bool CheckFrameIsValid(EffectFrameData frameData)
    {
        for(int i = 0; i < frameData.aI.Length; ++i)
        {
            if (!CheckImgIsValid(frameData.aI[i].i))
                return false;
        }

        return m_dicFrameNum[frameData.n] == 1;
    }

    // 检测特效数据是否有效
    // return:有效返回true；否则false
    private bool CheckEffectDataIsValid()
    {
        for(int i = 0; i < m_effectData.aA.Length; ++i)
        {
            EffectAssetData assetData = m_effectData.aA[i];
            if (!CheckImgIsValid(assetData.i))
                return false;
        }

        for(int i = 0; i < m_effectData.aF.Length; ++i)
        {
            EffectFrameData frameData = m_effectData.aF[i];
            if (!CheckFrameIsValid(frameData))
                return false;
        }

        return true;
    }

    // 播放当帧
    private void PlayCurrentFrame()
    {
        if (!Application.isPlaying)
            return;

        EffectEditorUI.Inst.UpdateByFrame(m_effectData, m_frameData);
    }

    // 复制一个帧数据
    // @frameData:要复制的帧数据
    // return:帧数据
    public static EffectFrameData CopyFrameData(EffectFrameData frameData)
    {
        EffectFrameData nFrameData = new EffectFrameData();
        nFrameData.n = frameData.n;
        nFrameData.aI = new EffectImageData[frameData.aI.Length];

        for(int i = 0; i < frameData.aI.Length; ++i)
        {
            EffectImageData imgData = frameData.aI[i];
            EffectImageData nImgData = new EffectImageData();

            nImgData.i = imgData.i;
            nImgData.sX = imgData.sX;
            nImgData.sY = imgData.sY;
            nImgData.r = imgData.r;

            nImgData.c[0] = imgData.c[0];
            nImgData.c[1] = imgData.c[1];
            nImgData.c[2] = imgData.c[2];
            nImgData.c[3] = imgData.c[3];

            nFrameData.aI[i] = nImgData;
        }

        return nFrameData;
    }

}
