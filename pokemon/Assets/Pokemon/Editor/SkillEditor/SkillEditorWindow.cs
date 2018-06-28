using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public class SkillEditorWindow : EditorWindow
{
    [MenuItem("Tools/技能编辑器")]
    public static void Create()
    {
        SkillEditorWindow window = EditorWindow.GetWindow<SkillEditorWindow>();
        window.maxSize = new Vector2(1100, 750);
        window.minSize = new Vector2(1100, 750);

       // window.m_skillData = SkillEditorWindow.CreateNewSkillData();

        window.m_defaultBgClr = GUI.backgroundColor;

        window.m_styleTittle = new GUIStyle(EditorStyles.textField);
        window.m_styleTittle.fontSize = 30;
        window.m_styleTittle.alignment = TextAnchor.MiddleCenter;
    }

    private int m_skillId;              // 技能id

    private SkillData m_skillData;      // 技能数据
    private Color m_defaultBgClr;       // 默认背景颜色
    private GUIStyle m_styleTittle;     // 标题风格
    private Vector2 m_actionScroPos;    // 行为数据的滚动位置
    private Vector2 m_hurmScroPos;      // 伤害数据的滚动位置
    private Vector2 m_displayScroPos;   // 表现数据的滚动数据

    // 伤害数据id数量映射
    // key => 伤害id
    // val => 存在次数
    private Dictionary<int, int> m_dicHurmNum = new Dictionary<int, int>();

    // 表现数据id数量映射
    // key => 表现id
    // val => 存在次数
    private Dictionary<int, int> m_dicDispNum = new Dictionary<int, int>();

    private const int MENU_WIDTH = 350; // 菜单宽度

    private static readonly string[] ARR_ACTION_TYPE_NAME = new string[]
    {
        "播放特效", "冲向目标", "发射子弹", "冲向坐标"
    }; // 技能行为类型的中文名称

    private static readonly string[] ARR_DISPLAY_TYPE_NAME = new string[]
    {
        "特效", "后退", "变白"
    }; // 技能表现类型的中文名称


    private void OnGUI()
    {
        GUILayout.BeginHorizontal();

        DrawMenu();

        GUILayout.EndHorizontal();

        if (m_skillData == null)
            return;

        GUILayout.BeginHorizontal();

        m_dicHurmNum.Clear();
        for (int i = 0; i < m_skillData.arrHurmData.Length; ++i)
        {
            int id = m_skillData.arrHurmData[i].id;
            if (m_dicHurmNum.ContainsKey(id))
                m_dicHurmNum[id]++;
            else
                m_dicHurmNum.Add(id, 1);
        }

        m_dicDispNum.Clear();
        for (int i = 0; i < m_skillData.arrDisplayData.Length; ++i)
        {
            int id = m_skillData.arrDisplayData[i].id;
            if (m_dicDispNum.ContainsKey(id))
                m_dicDispNum[id]++;
            else
                m_dicDispNum.Add(id, 1);
        }

        DrawAction();
        DrawHurm();
        DrawDisplay();

        GUILayout.EndHorizontal(); 
    }

    // 绘制菜单
    private void DrawMenu()
    {
        GUI.backgroundColor = Color.yellow;
        GUILayout.BeginHorizontal(new GUIStyle(EditorStyles.textField), GUILayout.Width(1100), GUILayout.Height(32));
        GUI.backgroundColor = m_defaultBgClr;

        GUILayout.Label("技能ID:", GUILayout.Width(55));
        m_skillId = EditorGUILayout.IntField(m_skillId, GUILayout.Width(100));

        if (GUILayout.Button("新建", GUILayout.Height(30), GUILayout.Width(50)))
        {
            m_skillData = CreateNewSkillData();
        }

        if (GUILayout.Button("加载", GUILayout.Height(30), GUILayout.Width(50)))
        {
            string path = "SkillData/" + m_skillId;
            TextAsset ta = Resources.Load<TextAsset>(path);
            if(ta == null)
            {
                ShowNotification(new GUIContent("加载失败！没有对应技能的配置"));
                return;
            }

            m_skillData = Deserializer.Deserialize<SkillData>(ta.text);
            if(m_skillData == null)
            {
                ShowNotification(new GUIContent("加载失败！配置数据有问题"));
                return;
            }
        }

        if (GUILayout.Button("保存", GUILayout.Height(30), GUILayout.Width(50)))
        {
            if(m_skillData == null)
            {
                ShowNotification(new GUIContent("保存失败！无效的技能"));
                return;
            }

            for(int i = 0; i < m_skillData.arrActionData.Length; ++i)
            {
                if(!CheckActionIsValid(m_skillData.arrActionData[i]))
                {
                    ShowNotification(new GUIContent("保存失败！存在技能行为报错"));
                    return;
                }
            }

            for (int i = 0; i < m_skillData.arrHurmData.Length; ++i)
            {
                if (!CheckHurmIsValid(m_skillData.arrHurmData[i].id))
                {
                    ShowNotification(new GUIContent("保存失败！存在技能伤害报错"));
                    return;
                }
            }

            for (int i = 0; i < m_skillData.arrDisplayData.Length; ++i)
            {
                if (!CheckDispIsValid(m_skillData.arrDisplayData[i].id))
                {
                    ShowNotification(new GUIContent("保存失败！存在技能表现报错"));
                    return;
                }
            }

            string path = Application.dataPath + "/Pokemon/Resources/SkillData/" + m_skillId + ".txt";
            string json = Serializer.Serialize(m_skillData);

            StreamWriter sw = new StreamWriter(path, false);
            sw.Write(json);
            sw.Dispose();
            sw.Close();

            AssetDatabase.Refresh();
            ShowNotification(new GUIContent("保存成功！"));
            return;
        }

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("复制数据", GUILayout.Height(30), GUILayout.Width(60)))
        {
            if (m_skillData == null)
            {
                ShowNotification(new GUIContent("复制失败！无效的技能"));
                return;
            }

            string json = Serializer.Serialize(m_skillData);
            TextEditor te = new TextEditor();
            te.text = json;
            te.OnFocus();
            te.Copy();
        }

        GUILayout.EndHorizontal();
    }

    // 绘制行为
    private void DrawAction()
    {
        GUI.backgroundColor = Color.blue;
        GUILayout.BeginVertical(new GUIStyle(EditorStyles.textField), GUILayout.Width(MENU_WIDTH), GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
        GUI.backgroundColor = m_defaultBgClr;

        // 标题
        GUI.backgroundColor = Color.white;
        GUILayout.BeginVertical();
        GUILayout.Label("技能行为", m_styleTittle, GUILayout.Width(MENU_WIDTH), GUILayout.Height(40));
        GUILayout.EndVertical(); // 标题
        GUI.backgroundColor = m_defaultBgClr;

        GUI.backgroundColor = Color.white;
        GUILayout.BeginVertical(GUILayout.Height(630), GUILayout.ExpandHeight(true));
        m_actionScroPos = GUILayout.BeginScrollView(m_actionScroPos, false, false);

        for (int i = 0; i < m_skillData.arrActionData.Length; ++i)
        {
            SkillActionData actionData = m_skillData.arrActionData[i];
            DrawActionItem(actionData);
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndScrollView();
        GUILayout.EndVertical(); 
        GUI.backgroundColor = m_defaultBgClr;

        // +按钮
        if (GUILayout.Button("+", GUILayout.Width(MENU_WIDTH), GUILayout.Height(40)))
        {
            List<SkillActionData> listAction = new List<SkillActionData>(m_skillData.arrActionData);
            SkillActionData actData = new SkillActionData();
            actData.arrHurmId = new int[0];
            listAction.Add(actData);

            m_skillData.arrActionData = listAction.ToArray();
        }

        GUILayout.EndVertical(); // 绘制行为
    }

    // 绘制伤害
    private void DrawHurm()
    {
        GUI.backgroundColor = Color.blue;
        GUILayout.BeginVertical(new GUIStyle(EditorStyles.textField), GUILayout.Width(MENU_WIDTH), GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
        GUI.backgroundColor = m_defaultBgClr;

        // 标题
        GUI.backgroundColor = Color.white;
        GUILayout.BeginVertical();
        GUILayout.Label("伤害", m_styleTittle, GUILayout.Width(MENU_WIDTH), GUILayout.Height(40));
        GUILayout.EndVertical(); // 标题
        GUI.backgroundColor = m_defaultBgClr;

        GUI.backgroundColor = Color.white;
        GUILayout.BeginVertical(GUILayout.Height(630), GUILayout.ExpandHeight(true));
        m_hurmScroPos = GUILayout.BeginScrollView(m_hurmScroPos, false, false);

        for (int i = 0; i < m_skillData.arrHurmData.Length; ++i)
        {
            SkillHurmData hurmData = m_skillData.arrHurmData[i];
            DrawHurmItem(hurmData);
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
        GUI.backgroundColor = m_defaultBgClr;

        // +按钮
        if (GUILayout.Button("+", GUILayout.Width(MENU_WIDTH), GUILayout.Height(40)))
        {
            List<SkillHurmData> listHurm = new List<SkillHurmData>(m_skillData.arrHurmData);
            SkillHurmData hurmData = new SkillHurmData();
            hurmData.arrDisplayId = new int[0];

            listHurm.Add(hurmData);

            m_skillData.arrHurmData = listHurm.ToArray();
        }

        GUILayout.EndVertical(); // 绘制行为
    }

    // 绘制表现
    private void DrawDisplay()
    {
        GUI.backgroundColor = Color.blue;
        GUILayout.BeginVertical(new GUIStyle(EditorStyles.textField), GUILayout.Width(MENU_WIDTH), GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
        GUI.backgroundColor = m_defaultBgClr;

        // 标题
        GUI.backgroundColor = Color.white;
        GUILayout.BeginVertical();
        GUILayout.Label("表现", m_styleTittle, GUILayout.Width(MENU_WIDTH), GUILayout.Height(40));
        GUILayout.EndVertical(); // 标题
        GUI.backgroundColor = m_defaultBgClr;

        GUI.backgroundColor = Color.white;
        GUILayout.BeginVertical(GUILayout.Height(630), GUILayout.ExpandHeight(true));
        m_displayScroPos = GUILayout.BeginScrollView(m_displayScroPos, false, false);

        for (int i = 0; i < m_skillData.arrDisplayData.Length; ++i)
        {
            SkillDisplayData dispData = m_skillData.arrDisplayData[i];
            DrawDisplayItem(dispData);
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
        GUI.backgroundColor = m_defaultBgClr;

        // +按钮
        if (GUILayout.Button("+", GUILayout.Width(MENU_WIDTH), GUILayout.Height(40)))
        {
            List<SkillDisplayData> listDisp = new List<SkillDisplayData>(m_skillData.arrDisplayData);
            listDisp.Add(new SkillDisplayData());

            m_skillData.arrDisplayData = listDisp.ToArray();
        }

        GUILayout.EndVertical(); // 绘制行为
    }

    // 绘制行为图标
    // @actionData:行为数据
    private void DrawActionItem(SkillActionData actionData)
    {
        if(CheckActionIsValid(actionData))
            GUI.backgroundColor = Color.green;
        else
            GUI.backgroundColor = Color.red;

        GUILayout.BeginVertical(new GUIStyle(EditorStyles.textField), GUILayout.Width(MENU_WIDTH-20), GUILayout.ExpandHeight(true));
        GUI.backgroundColor = m_defaultBgClr;

        GUILayout.BeginHorizontal();
        GUILayout.Label("行为类型:", GUILayout.Width(55));
        actionData.actionType = (int)EditorGUILayout.Popup(actionData.actionType, ARR_ACTION_TYPE_NAME, GUILayout.Width(100));
        //GUILayout.FlexibleSpace();

        if (GUILayout.Button("-", GUILayout.Width(20)))
        {
            List<SkillActionData> listAction = new List<SkillActionData>(m_skillData.arrActionData);
            listAction.Remove(actionData);

            m_skillData.arrActionData = listAction.ToArray();
        }

        GUILayout.EndHorizontal();

        if (actionData.actionType == (int)SkillActionType.PLAY_EFFECT) // 播放特效
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("特效ID:", GUILayout.Width(55));
            actionData.effectId = EditorGUILayout.IntField(actionData.effectId, GUILayout.Width(100));
            GUILayout.Label("播放时间:", GUILayout.Width(55));
            actionData.time = EditorGUILayout.FloatField(actionData.time, GUILayout.Width(100));
            GUILayout.EndHorizontal();
        }
        else if (actionData.actionType == (int)SkillActionType.CRASH_TARGET) // 冲向目标
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("移动速度:", GUILayout.Width(55));
            actionData.speed = EditorGUILayout.IntField(actionData.speed, GUILayout.Width(100));
            GUILayout.EndHorizontal();
        }
        else if (actionData.actionType == (int)SkillActionType.CRASH_POS) // 冲向位置
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("坐标X:", GUILayout.Width(55));
            actionData.posX = EditorGUILayout.IntField(actionData.posX, GUILayout.Width(100));
            GUILayout.Label("坐标Y:", GUILayout.Width(55));
            actionData.posY = EditorGUILayout.IntField(actionData.posY, GUILayout.Width(100));
            GUILayout.EndHorizontal();
        }
        else if (actionData.actionType == (int)SkillActionType.SHOOT_BULLET) // 发射子弹
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("特效ID:", GUILayout.Width(55));
            actionData.effectId = EditorGUILayout.IntField(actionData.effectId, GUILayout.Width(100));
            GUILayout.Label("子弹速度:", GUILayout.Width(55));
            actionData.speed = EditorGUILayout.IntField(actionData.speed, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("同时受击:", GUILayout.Width(55));
            bool sameTimeHit = actionData.sameTimeHited == 1;
            sameTimeHit = EditorGUILayout.Toggle(sameTimeHit, GUILayout.Width(100));
            actionData.sameTimeHited = sameTimeHit ? 1 : 0;
            GUILayout.EndHorizontal();
        }


        GUILayout.BeginVertical();
        for(int i = 0; i < actionData.arrHurmId.Length; ++i)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("伤害id:", GUILayout.Width(55));
            actionData.arrHurmId[i] = EditorGUILayout.IntField(actionData.arrHurmId[i], GUILayout.Width(100));

            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                List<int> listId = new List<int>(actionData.arrHurmId);
                listId.RemoveAt(i);
                actionData.arrHurmId = listId.ToArray();
                break;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
       
        GUILayout.EndVertical();

        // +按钮
        if (GUILayout.Button("+", GUILayout.Width(MENU_WIDTH), GUILayout.Height(20), GUILayout.Width(345)))
        {
            List<int> listId = new List<int>(actionData.arrHurmId);
            listId.Add(0);

            actionData.arrHurmId = listId.ToArray();
        }

        GUILayout.EndVertical();

    }

    // 绘制伤害图标
    // @hurmData:伤害数据
    private void DrawHurmItem(SkillHurmData hurmData)
    {
        if(CheckHurmIsValid(hurmData.id))
            GUI.backgroundColor = Color.green;
        else
            GUI.backgroundColor = Color.red;

        GUILayout.BeginVertical(new GUIStyle(EditorStyles.textField), GUILayout.Width(MENU_WIDTH-20));
        GUI.backgroundColor = m_defaultBgClr;

        GUILayout.BeginHorizontal();
        GUILayout.Label("伤害id:", GUILayout.Width(55));
        hurmData.id = EditorGUILayout.IntField(hurmData.id, GUILayout.Width(100));
        //GUILayout.FlexibleSpace();

        if (GUILayout.Button("-", GUILayout.Width(20)))
        {
            List<SkillHurmData> listHurm = new List<SkillHurmData>(m_skillData.arrHurmData);
            listHurm.Remove(hurmData);

            m_skillData.arrHurmData = listHurm.ToArray();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("触发时间:", GUILayout.Width(55));
        hurmData.time = EditorGUILayout.FloatField(hurmData.time, GUILayout.Width(100));
        GUILayout.Label("伤害比重:", GUILayout.Width(55));
        hurmData.hurmPct = EditorGUILayout.FloatField(hurmData.hurmPct, GUILayout.Width(100));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();


        GUILayout.BeginVertical();
        for (int i = 0; i < hurmData.arrDisplayId.Length; ++i)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("表现id:", GUILayout.Width(55));
            hurmData.arrDisplayId[i] = EditorGUILayout.IntField(hurmData.arrDisplayId[i], GUILayout.Width(100));

            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                List<int> listId = new List<int>(hurmData.arrDisplayId);
                listId.RemoveAt(i);
                hurmData.arrDisplayId = listId.ToArray();
                break;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();

        // +按钮
        if (GUILayout.Button("+", GUILayout.Width(MENU_WIDTH), GUILayout.Height(20), GUILayout.Width(345)))
        {
            List<int> listId = new List<int>(hurmData.arrDisplayId);
            listId.Add(0);

            hurmData.arrDisplayId = listId.ToArray();
        }




        GUILayout.EndVertical();
    }

    // 绘制表现图标
    // @dispData:伤害表现数据
    private void DrawDisplayItem(SkillDisplayData dispData)
    {
        if(CheckDispIsValid(dispData.id))
            GUI.backgroundColor = Color.green;
        else
            GUI.backgroundColor = Color.red;

        GUILayout.BeginVertical(new GUIStyle(EditorStyles.textField), GUILayout.Width(MENU_WIDTH), GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
        GUI.backgroundColor = m_defaultBgClr;

        GUILayout.BeginHorizontal();
        GUILayout.Label("表现id:", GUILayout.Width(55));
        dispData.id = EditorGUILayout.IntField(dispData.id, GUILayout.Width(100));

        if (GUILayout.Button("-", GUILayout.Width(20)))
        {
            List<SkillDisplayData> listDisp = new List<SkillDisplayData>(m_skillData.arrDisplayData);
            listDisp.Remove(dispData);

            m_skillData.arrDisplayData = listDisp.ToArray();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("表现类型:", GUILayout.Width(55));
        dispData.displayType = (int)EditorGUILayout.Popup(dispData.displayType, ARR_DISPLAY_TYPE_NAME, GUILayout.Width(100));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();


        GUILayout.EndVertical();
    }

    // 检测行为是否有效
    // @actionData:行为数据
    // return:有效返回true；否则false
    private bool CheckActionIsValid(SkillActionData actionData)
    {
        for(int i = 0; i < actionData.arrHurmId.Length; ++i)
        {
            int harmId = actionData.arrHurmId[i];
            if (!CheckHurmIsValid(harmId))
                return false;
        }

        return true;
    }

    // 检测伤害是否有效
    // @hurmId:伤害配置id
    // return:有效返回true；否则false
    private bool CheckHurmIsValid(int hurmId)
    {
        if (m_dicHurmNum.ContainsKey(hurmId))
            return m_dicHurmNum[hurmId] == 1;

        return false;
    }

    // 检测表现是否有效
    // @dispId:表现id
    // return:有效返回true；否则false
    private bool CheckDispIsValid(int dispId)
    {
        if (m_dicDispNum.ContainsKey(dispId))
            return m_dicDispNum[dispId] == 1;

        return false;
    }



    // 创建新的技能数据
    // return:技能数据
    private static SkillData CreateNewSkillData()
    {
        SkillData skillData = new SkillData();
        skillData.arrActionData = new SkillActionData[0];
        skillData.arrDisplayData = new SkillDisplayData[0];
        skillData.arrHurmData = new SkillHurmData[0];

        return skillData;
    }
}
