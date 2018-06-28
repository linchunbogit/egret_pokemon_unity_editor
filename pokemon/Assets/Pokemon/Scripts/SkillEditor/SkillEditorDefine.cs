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
public class SkillHurmData
{
    public int i;              // 伤害id
    public float t;          // 伤害触发时间
    public float h;             // 伤害占总伤害的百分比 0~1 小数
    public int[] aD;  // 表现id集合
}

// 技能表现数据
public class SkillDisplayData
{
    public int i;              // 表现id
    public int d;     // 技能表现类型
    public int e;               // 特效id
}

// 技能行为数据
public class SkillActionData
{
    public int a;      // 技能行为类型
    public int s;           // 速度，子弹和冲
    public int e;        // 特效id
    public int x;            // 目的坐标x
    public int y;            // 目的坐标y
    public float t;          // 时间
    public int sH;   // 同时受击
    public int[] aH;     // 伤害id集合
}

// 技能数据
public class SkillData
{
    public SkillActionData[] aA;             // 技能行为数据
    public SkillHurmData[] aH;                 // 技能伤害数据
    public SkillDisplayData[] aD;           // 技能表现数据
}
