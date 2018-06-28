using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectEditorDefine
{

}

// 特效的资源数据
public class EffectAssetData
{
    public int i;           // 图片id
    public string n;      // 图片名称
    public int w;           // 图片宽度
    public int h;          // 图片高度
}


// 特效图片数据
public class EffectImageData
{
    public int i;           // 图片id
    public float sX;        // 缩放x
    public float sY;        // 缩放y
    public float pX;          // 位置x
    public float pY;          // 位置y
    public float r;        // 旋转
    public int[] c;         // 颜色

    public EffectImageData()
    {
        i = 0;
        sX = 1;
        sY = 1;
        pX = 0;
        pY = 0;
        r = 0;
        c = new int[] { 255, 255, 255, 255 };
    }
}

// 帧数据
public class EffectFrameData
{
    public int n;                     // 帧编号 0开始
    public EffectImageData[] aI;    // 图片数据

    public EffectFrameData()
    {
        n = 0;
        aI = new EffectImageData[0];
    }
}

// 特效配置数据
public class EffectData
{
    public int r;                            // 帧率
    public EffectAssetData[] aA;      // 引用数据
    public EffectFrameData[] aF;      // 所有的帧数据

    public EffectData()
    {
        aA = new EffectAssetData[0];
        aF = new EffectFrameData[0];
    }
}