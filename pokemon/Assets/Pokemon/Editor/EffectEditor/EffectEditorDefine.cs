using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectEditorDefine
{

}

// 特效的资源数据
public class EffectAssetData
{
    public int imgId;           // 图片id
    public string imgName;      // 图片名称
    public int width;           // 图片宽度
    public int height;          // 图片高度
}


// 特效图片数据
public class EffectImageData
{
    public int imgId;           // 图片id
    public float scaleX;        // 缩放x
    public float scaleY;        // 缩放y
    public float rotate;        // 旋转
    public float[] color;       // 颜色

    public EffectImageData()
    {
        imgId = 0;
        scaleX = 1;
        scaleY = 1;
        rotate = 0;
        color = new float[] { 1, 1, 1, 1 };
    }
}

// 帧数据
public class EffectFrameData
{
    public int frameNo;                     // 帧编号
    public EffectImageData[] arrImgData;    // 图片数据

    public EffectFrameData()
    {
        frameNo = 0;
        arrImgData = new EffectImageData[0];
    }
}

// 特效配置数据
public class EffectData
{
    public int rate;                            // 帧率
    public EffectAssetData[] arrAssetData;      // 引用数据
    public EffectFrameData[] arrFrameData;      // 所有的帧数据

    public EffectData()
    {
        arrAssetData = new EffectAssetData[0];
        arrFrameData = new EffectFrameData[0];
    }
}