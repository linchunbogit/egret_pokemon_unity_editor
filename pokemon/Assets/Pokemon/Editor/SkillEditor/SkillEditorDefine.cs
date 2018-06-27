using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillEditorDefine
{

}









// 技能攻击方式
public enum SkillAtkType
{
    EFFECT = 0,     // 播放特效
    CRASH,          // 冲向敌人
    BULLET,         // 子弹
}

// 技能受击表现类型
public enum SkillDisplayType
{
    EFFECT = 0,     // 特效
    BACK,           // 后退
    WHITE,          // 变白
}

// 技能行为类型
public enum SkillActionType
{
    PLAY_EFFECT = 0,        // 播放特效
    CRASH_TARGET,           // 冲向目标
    SHOOT_BULLET,           // 发射子弹
    CRASH_POS,              // 冲向坐标
}

// 技能伤害数据
[System.Serializable]
public class SkillHurmData
{
    public int id;              // 伤害id
    public float time;          // 伤害触发时间
    public float hurmPct;       // 伤害占总伤害的百分比
    public int[] arrDisplayId;  // 表现id集合
}

// 技能表现数据
[System.Serializable]
public class SkillDisplayData
{
    public int id;              // 表现id
    public int displayType;     // 技能表现类型
}

// 技能行为数据
[System.Serializable]
public class SkillActionData
{
    public int actionType;      // 技能行为类型
    public int speed;           // 速度，子弹和冲
    public int effectId;        // 特效id
    public int posX;            // 目的坐标x
    public int posY;            // 目的坐标y
    public float time;          // 时间
    public int sameTimeHited;   // 同时受击
    public int[] arrHurmId;     // 伤害id集合
}

// 技能数据
[System.Serializable]
public class SkillData
{
    public SkillActionData[] arrActionData;             // 技能行为数据
    public SkillHurmData[] arrHurmData;                 // 技能伤害数据
    public SkillDisplayData[] arrDisplayData;           // 技能表现数据
}
