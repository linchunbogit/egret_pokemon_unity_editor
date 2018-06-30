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
        "场景播放特效", "冲向目标", "发射子弹", "自身特效",
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
        for (int i = 0; i < m_skillData.aH.Length; ++i)
        {
            int id = m_skillData.aH[i].i;
            if (m_dicHurmNum.ContainsKey(id))
                m_dicHurmNum[id]++;
            else
                m_dicHurmNum.Add(id, 1);
        }

        m_dicDispNum.Clear();
        for (int i = 0; i < m_skillData.aD.Length; ++i)
        {
            int id = m_skillData.aD[i].i;
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
            string path = "SkillData/skilljson" + m_skillId;
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

            for(int i = 0; i < m_skillData.aA.Length; ++i)
            {
                if(!CheckActionIsValid(m_skillData.aA[i]))
                {
                    ShowNotification(new GUIContent("保存失败！存在技能行为报错"));
                    return;
                }
            }

            for (int i = 0; i < m_skillData.aH.Length; ++i)
            {
                if (!CheckHurmIsValid(m_skillData.aH[i].i))
                {
                    ShowNotification(new GUIContent("保存失败！存在技能伤害报错"));
                    return;
                }
            }

            for (int i = 0; i < m_skillData.aD.Length; ++i)
            {
                if (!CheckDispIsValid(m_skillData.aD[i].i))
                {
                    ShowNotification(new GUIContent("保存失败！存在技能表现报错"));
                    return;
                }
            }

            string path = Application.dataPath + "/Pokemon/Resources/SkillData/skilljson" + m_skillId + ".txt";
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

        for (int i = 0; i < m_skillData.aA.Length; ++i)
        {
            SkillActionData actionData = m_skillData.aA[i];
            DrawActionItem(actionData);
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndScrollView();
        GUILayout.EndVertical(); 
        GUI.backgroundColor = m_defaultBgClr;

        // +按钮
        if (GUILayout.Button("+", GUILayout.Width(MENU_WIDTH), GUILayout.Height(40)))
        {
            List<SkillActionData> listAction = new List<SkillActionData>(m_skillData.aA);
            SkillActionData actData = new SkillActionData();
            actData.aH = new int[0];
            listAction.Add(actData);

            m_skillData.aA = listAction.ToArray();
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

        for (int i = 0; i < m_skillData.aH.Length; ++i)
        {
            SkillHurmData hurmData = m_skillData.aH[i];
            DrawHurmItem(hurmData);
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
        GUI.backgroundColor = m_defaultBgClr;

        // +按钮
        if (GUILayout.Button("+", GUILayout.Width(MENU_WIDTH), GUILayout.Height(40)))
        {
            List<SkillHurmData> listHurm = new List<SkillHurmData>(m_skillData.aH);
            SkillHurmData hurmData = new SkillHurmData();
            hurmData.aD = new int[0];

            listHurm.Add(hurmData);

            m_skillData.aH = listHurm.ToArray();
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

        for (int i = 0; i < m_skillData.aD.Length; ++i)
        {
            SkillDisplayData dispData = m_skillData.aD[i];
            DrawDisplayItem(dispData);
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
        GUI.backgroundColor = m_defaultBgClr;

        // +按钮
        if (GUILayout.Button("+", GUILayout.Width(MENU_WIDTH), GUILayout.Height(40)))
        {
            List<SkillDisplayData> listDisp = new List<SkillDisplayData>(m_skillData.aD);
            listDisp.Add(new SkillDisplayData());

            m_skillData.aD = listDisp.ToArray();
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
        GUILayout.Label("行为类型:", GUILayout.Width(75));
        actionData.a = (int)EditorGUILayout.Popup(actionData.a, ARR_ACTION_TYPE_NAME, GUILayout.Width(100));
        //GUILayout.FlexibleSpace();

        if (GUILayout.Button("-", GUILayout.Width(20)))
        {
            List<SkillActionData> listAction = new List<SkillActionData>(m_skillData.aA);
            listAction.Remove(actionData);

            m_skillData.aA = listAction.ToArray();
        }

        GUILayout.EndHorizontal();

        if (actionData.a == (int)SkillActionType.PLAY_EFFECT || actionData.a == (int)SkillActionType.PLAY_EFFECT_SELF) // 播放特效
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("行为播放时间:", GUILayout.Width(75));
            actionData.t = EditorGUILayout.FloatField(actionData.t, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("特效ID:", GUILayout.Width(75));
            actionData.e1 = EditorGUILayout.IntField(actionData.e1, GUILayout.Width(100));

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("坐标X:", GUILayout.Width(75));
            actionData.x = EditorGUILayout.IntField(actionData.x, GUILayout.Width(100));
            GUILayout.Label("坐标Y:", GUILayout.Width(75));
            actionData.y = EditorGUILayout.IntField(actionData.y, GUILayout.Width(100));
            GUILayout.EndHorizontal();
        }
        else if (actionData.a == (int)SkillActionType.CRASH_TARGET) // 冲向目标
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("行为播放时间:", GUILayout.Width(75));
            actionData.t = EditorGUILayout.FloatField(actionData.t, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("移动速度:", GUILayout.Width(75));
            actionData.s = EditorGUILayout.IntField(actionData.s, GUILayout.Width(100));
            GUILayout.Label("限制移动时间:", GUILayout.Width(75));
            bool limitMove = actionData.l2 == 1;
            limitMove = EditorGUILayout.Toggle(limitMove, GUILayout.Width(40));
            actionData.l2 = limitMove ? 1 : 0;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("前排特效ID:", GUILayout.Width(75));
            actionData.e1 = EditorGUILayout.IntField(actionData.e1, GUILayout.Width(100));
            GUILayout.Label("后排特效ID:", GUILayout.Width(75));
            actionData.e2 = EditorGUILayout.IntField(actionData.e2, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("特效锁目标:", GUILayout.Width(75));
            bool lookTarget = actionData.l == 1;
            lookTarget = EditorGUILayout.Toggle(lookTarget, GUILayout.Width(40));
            actionData.l = lookTarget ? 1 : 0;
            GUILayout.EndHorizontal();
        }
        //else if (actionData.a == (int)SkillActionType.CRASH_POS) // 冲向位置
        //{
        //    GUILayout.BeginHorizontal();
        //    GUILayout.Label("坐标X:", GUILayout.Width(55));
        //    actionData.x = EditorGUILayout.IntField(actionData.x, GUILayout.Width(100));
        //    GUILayout.Label("坐标Y:", GUILayout.Width(55));
        //    actionData.y = EditorGUILayout.IntField(actionData.y, GUILayout.Width(100));
        //    GUILayout.EndHorizontal();
        //}
        else if (actionData.a == (int)SkillActionType.SHOOT_BULLET) // 发射子弹
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("行为播放时间:", GUILayout.Width(75));
            actionData.t = EditorGUILayout.FloatField(actionData.t, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("子弹速度:", GUILayout.Width(75));
            actionData.s = EditorGUILayout.IntField(actionData.s, GUILayout.Width(100));
            GUILayout.Label("限制移动时间:", GUILayout.Width(75));
            bool limitMove = actionData.l2 == 1;
            limitMove = EditorGUILayout.Toggle(limitMove, GUILayout.Width(40));
            actionData.l2 = limitMove ? 1 : 0;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("前排特效ID:", GUILayout.Width(75));
            actionData.e1 = EditorGUILayout.IntField(actionData.e1, GUILayout.Width(100));
            GUILayout.Label("后排特效ID:", GUILayout.Width(75));
            actionData.e2 = EditorGUILayout.IntField(actionData.e2, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("发射点偏移X:", GUILayout.Width(75));
            actionData.x = EditorGUILayout.IntField(actionData.x, GUILayout.Width(100));
            GUILayout.Label("发射点偏移Y:", GUILayout.Width(75));
            actionData.y = EditorGUILayout.IntField(actionData.y, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("特效锁目标:", GUILayout.Width(75));
            bool lookTarget = actionData.l == 1;
            lookTarget = EditorGUILayout.Toggle(lookTarget, GUILayout.Width(40));
            actionData.l = lookTarget ? 1 : 0;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("子弹时间1-3:", GUILayout.Width(75));
            for(int i = 0; i < 3; ++i)
            {
                actionData.aB[i] = EditorGUILayout.FloatField(actionData.aB[i], GUILayout.Width(60));
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("子弹时间4-6:", GUILayout.Width(75));
            for (int i = 3; i < 6; ++i)
            {
                actionData.aB[i] = EditorGUILayout.FloatField(actionData.aB[i], GUILayout.Width(60));
            }

            GUILayout.EndHorizontal();
        }


        GUILayout.BeginVertical();
        for(int i = 0; i < actionData.aH.Length; ++i)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("伤害id:", GUILayout.Width(75));
            actionData.aH[i] = EditorGUILayout.IntField(actionData.aH[i], GUILayout.Width(100));

            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                List<int> listId = new List<int>(actionData.aH);
                listId.RemoveAt(i);
                actionData.aH = listId.ToArray();
                break;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
       
        GUILayout.EndVertical();

        // +按钮
        if (GUILayout.Button("+", GUILayout.Width(MENU_WIDTH), GUILayout.Height(20), GUILayout.Width(345)))
        {
            List<int> listId = new List<int>(actionData.aH);
            listId.Add(0);

            actionData.aH = listId.ToArray();
        }

        GUILayout.EndVertical();

    }

    // 绘制伤害图标
    // @hurmData:伤害数据
    private void DrawHurmItem(SkillHurmData hurmData)
    {
        if(CheckHurmIsValid(hurmData.i))
            GUI.backgroundColor = Color.green;
        else
            GUI.backgroundColor = Color.red;

        GUILayout.BeginVertical(new GUIStyle(EditorStyles.textField), GUILayout.Width(MENU_WIDTH-20));
        GUI.backgroundColor = m_defaultBgClr;

        GUILayout.BeginHorizontal();
        GUILayout.Label("伤害id:", GUILayout.Width(55));
        hurmData.i = EditorGUILayout.IntField(hurmData.i, GUILayout.Width(100));
        //GUILayout.FlexibleSpace();

        if (GUILayout.Button("-", GUILayout.Width(20)))
        {
            List<SkillHurmData> listHurm = new List<SkillHurmData>(m_skillData.aH);
            listHurm.Remove(hurmData);

            m_skillData.aH = listHurm.ToArray();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("触发时间:", GUILayout.Width(55));
        hurmData.t = EditorGUILayout.FloatField(hurmData.t, GUILayout.Width(100));
        GUILayout.Label("伤害比重:", GUILayout.Width(55));
        hurmData.h = EditorGUILayout.FloatField(hurmData.h, GUILayout.Width(100));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();


        GUILayout.BeginVertical();
        for (int i = 0; i < hurmData.aD.Length; ++i)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("表现id:", GUILayout.Width(55));
            hurmData.aD[i] = EditorGUILayout.IntField(hurmData.aD[i], GUILayout.Width(100));

            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                List<int> listId = new List<int>(hurmData.aD);
                listId.RemoveAt(i);
                hurmData.aD = listId.ToArray();
                break;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();

        // +按钮
        if (GUILayout.Button("+", GUILayout.Width(MENU_WIDTH), GUILayout.Height(20), GUILayout.Width(345)))
        {
            List<int> listId = new List<int>(hurmData.aD);
            listId.Add(0);

            hurmData.aD = listId.ToArray();
        }




        GUILayout.EndVertical();
    }

    // 绘制表现图标
    // @dispData:伤害表现数据
    private void DrawDisplayItem(SkillDisplayData dispData)
    {
        if(CheckDispIsValid(dispData.i))
            GUI.backgroundColor = Color.green;
        else
            GUI.backgroundColor = Color.red;

        GUILayout.BeginVertical(new GUIStyle(EditorStyles.textField), GUILayout.Width(MENU_WIDTH), GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
        GUI.backgroundColor = m_defaultBgClr;

        GUILayout.BeginHorizontal();
        GUILayout.Label("表现id:", GUILayout.Width(55));
        dispData.i = EditorGUILayout.IntField(dispData.i, GUILayout.Width(100));

        if (GUILayout.Button("-", GUILayout.Width(20)))
        {
            List<SkillDisplayData> listDisp = new List<SkillDisplayData>(m_skillData.aD);
            listDisp.Remove(dispData);

            m_skillData.aD = listDisp.ToArray();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal(); // 第2行
        GUILayout.Label("表现类型:", GUILayout.Width(55));
        dispData.d = (int)EditorGUILayout.Popup(dispData.d, ARR_DISPLAY_TYPE_NAME, GUILayout.Width(100));
           
        if(dispData.d == (int)SkillDisplayType.EFFECT)
        {
            GUILayout.Label("特效id:", GUILayout.Width(55));
            dispData.e = EditorGUILayout.IntField(dispData.e, GUILayout.Width(100));
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal(); // 第2行



        GUILayout.EndVertical();
    }

    // 检测行为是否有效
    // @actionData:行为数据
    // return:有效返回true；否则false
    private bool CheckActionIsValid(SkillActionData actionData)
    {
        for(int i = 0; i < actionData.aH.Length; ++i)
        {
            int harmId = actionData.aH[i];
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
        if (!m_dicHurmNum.ContainsKey(hurmId))
            return false;

        if (m_dicHurmNum[hurmId] != 1)
            return false;

        for(int i = 0; i < m_skillData.aH.Length; ++i)
        {
            SkillHurmData hurmData = m_skillData.aH[i];
            if(hurmData.i != hurmId)
                continue; 

            for(int j = 0; j < hurmData.aD.Length; ++j)
            {
                int dispId = hurmData.aD[j];
                if(!CheckDispIsValid(dispId))
                {
                    return false;
                }
            }
        }

        return true;
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
        skillData.aA = new SkillActionData[0];
        skillData.aD = new SkillDisplayData[0];
        skillData.aH = new SkillHurmData[0];

        return skillData;
    }
}
