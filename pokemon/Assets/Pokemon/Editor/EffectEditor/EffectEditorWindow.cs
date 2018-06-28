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
        window.m_defaultConClr = GUI.color;

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

    private int m_effectId;             // 特效id
    private EffectData m_effectData;    // 特效数据
    private EffectFrameData m_frameData;// 当前选中的帧

    // 资源id次数映射
    // key => 资源id
    // val => id使用次数
    private Dictionary<int, int> m_dicAssetNum = new Dictionary<int, int>();

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
        for(int i = 0; i < m_effectData.arrAssetData.Length; ++i)
        {
            EffectAssetData assetData = m_effectData.arrAssetData[i];
            if (m_dicAssetNum.ContainsKey(assetData.imgId))
                m_dicAssetNum[assetData.imgId]++;
            else
                m_dicAssetNum.Add(assetData.imgId, 1);
        }

        GUILayout.BeginVertical();

        DrawSetting();

        GUILayout.BeginHorizontal();

        DrawAsset();
        DrawFrame();

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
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
        }

        if (GUILayout.Button("加载", GUILayout.Height(30), GUILayout.Width(50)))
        {
            string path = "EffectData/" + m_effectId;
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
        }

        if (GUILayout.Button("保存", GUILayout.Height(30), GUILayout.Width(50)))
        {
            if (m_effectData == null)
            {
                ShowNotification(new GUIContent("保存失败！无效的技能"));
                return;
            }
            
            string path = Application.dataPath + "/Pokemon/Resources/EffectData/" + m_effectId + ".txt";
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
        m_effectData.rate = EditorGUILayout.IntField(m_effectData.rate, GUILayout.Width(100));
        GUILayout.Label("总播放时间:" + CalcEffectTime() + "s", GUILayout.Width(200));

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

        for (int i = 0; i < m_effectData.arrAssetData.Length; ++i)
        {
            EffectAssetData assData = m_effectData.arrAssetData[i];
            DrawAssetItem(assData);
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndScrollView();

        // +按钮
        if (GUILayout.Button("+", GUILayout.Height(40), GUILayout.Width(245)))
        {
            List<EffectAssetData> listAsset = new List<EffectAssetData>(m_effectData.arrAssetData);
            listAsset.Add(new EffectAssetData());

            m_effectData.arrAssetData = listAsset.ToArray();
        }

        GUILayout.EndVertical();
    }

    // 绘制帧
    private void DrawFrame()
    {
        GUI.backgroundColor = Color.black;
        GUILayout.BeginVertical(new GUIStyle(EditorStyles.textField), GUILayout.Width(WINDOW_WIDTH - 250), GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
        GUI.backgroundColor = m_defaultBgClr;
        m_frameScroPos = GUILayout.BeginScrollView(m_frameScroPos, true, false, GUILayout.Height(120), GUILayout.Width(WINDOW_WIDTH - 260)); // true, false, GUILayout.Width(200), GUILayout.Height(100));

        EffectFrameData[] arrFrameData = m_effectData.arrFrameData;
        GUILayout.BeginHorizontal();
        for (int i = 0; i < arrFrameData.Length; ++i)
        {
            EffectFrameData frameData = arrFrameData[i];

            GUI.contentColor = Color.white;
            GUILayout.Label((frameData.frameNo + 1).ToString(), m_styleFrameFont, GUILayout.Width(20));
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
         
        if(m_frameData == null)
            GUILayout.Label("当前未选中帧", m_styleTittle, GUILayout.Width(WINDOW_WIDTH - 250), GUILayout.Height(32));
        else
            GUILayout.Label("当前选中第" + (m_frameData.frameNo + 1) + "帧", m_styleTittle, GUILayout.Width(WINDOW_WIDTH - 250), GUILayout.Height(32));

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("添加显示"))
        {

        }
        if (GUILayout.Button("添加新帧"))
        {
            List<EffectFrameData> listFrame = new List<EffectFrameData>(m_effectData.arrFrameData);
            int frameNum = listFrame.Count;
            EffectFrameData nFrameData = frameNum > 0 ? CopyFrameData(listFrame[frameNum-1]) : new EffectFrameData();
            
            if(frameNum > 0)
                nFrameData.frameNo++;

            listFrame.Add(nFrameData);
            m_effectData.arrFrameData = listFrame.ToArray();
        }
        if (GUILayout.Button("删除当前帧"))
        {
            if(m_frameData == null)
            {
                ShowNotification(new GUIContent("删除失败！当前没选中帧"));
            }

            List<EffectFrameData> listFrame = new List<EffectFrameData>(m_effectData.arrFrameData);
            listFrame.Remove(m_frameData);

            m_effectData.arrFrameData = listFrame.ToArray();
            m_frameData = null;
        }

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();






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
        assetData.imgId = EditorGUILayout.IntField(assetData.imgId, GUILayout.Width(100));

        if (GUILayout.Button("-", GUILayout.Width(20)))
        {
            List<EffectAssetData> listData = new List<EffectAssetData>(m_effectData.arrAssetData);
            listData.Remove(assetData);

            m_effectData.arrAssetData = listData.ToArray();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("资源:", GUILayout.Width(55));

        Texture2D tx2D = null;
        if(!string.IsNullOrEmpty(assetData.imgName))
            tx2D = Resources.Load<Texture2D>("EffectRaw/" + assetData.imgName);

        Texture2D nTx2D = (Texture2D)EditorGUILayout.ObjectField(tx2D, typeof(Texture2D), false, GUILayout.Width(120));
        if (nTx2D != null)
        {
            assetData.imgName = nTx2D.name;
            assetData.width = nTx2D.width;
            assetData.height = nTx2D.height;
        }
        else
        {
            assetData.imgName = null;
        }

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    // 检测资源数据是否有效
    // @assetData:资源数据
    // return:有效返回ture；否则false
    private bool CheckAssetIsValid(EffectAssetData assetData)
    {
        if (string.IsNullOrEmpty(assetData.imgName))
            return false;

        return m_dicAssetNum[assetData.imgId] == 1;
    }

    // 计算特效时间
    // return:返回秒数
    private float CalcEffectTime()
    {
        int frameNum = m_effectData.arrFrameData.Length;
        if (m_effectData.rate == 0 || frameNum < 1)
            return 0;

        int lastFrameIdx = m_effectData.arrFrameData[frameNum - 1].frameNo + 1; ;
        return (1.0f / m_effectData.rate) * lastFrameIdx;
    }

    // 复制一个帧数据
    // @frameData:要复制的帧数据
    // return:帧数据
    public static EffectFrameData CopyFrameData(EffectFrameData frameData)
    {
        EffectFrameData nFrameData = new EffectFrameData();
        nFrameData.frameNo = frameData.frameNo;
        nFrameData.arrImgData = new EffectImageData[frameData.arrImgData.Length];

        for(int i = 0; i < frameData.arrImgData.Length; ++i)
        {
            EffectImageData imgData = frameData.arrImgData[i];
            EffectImageData nImgData = new EffectImageData();

            nImgData.imgId = imgData.imgId;
            nImgData.scaleX = imgData.scaleX;
            nImgData.scaleY = imgData.scaleY;
            nImgData.rotate = imgData.rotate;

            nImgData.color[0] = imgData.color[0];
            nImgData.color[1] = imgData.color[1];
            nImgData.color[2] = imgData.color[2];
            nImgData.color[3] = imgData.color[3];
        }

        return nFrameData;
    }

}
