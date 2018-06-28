using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectEditorUtil
{
    // 计算特效时间
    // @effData:特效数据
    // return:返回秒数
    public static float CalcEffectTime(EffectData effData)
    {
        int frameNum = effData.arrFrameData.Length;
        if (effData.rate == 0 || frameNum < 1)
            return 0;

        int lastFrameIdx = effData.arrFrameData[frameNum - 1].frameNo + 1; ;
        return (1.0f / effData.rate) * lastFrameIdx;
    }

    // 计算帧编号
    // @runTime:已经运行时间
    // @rate:帧率
    // return:帧编号
    public static int CalcFrameNo(float runTime, int rate)
    {
        return Mathf.FloorToInt(runTime * rate);
    }
    // 计算插值
    // @leftVal:左边记录的帧的值
    // @rightVal:右边记录的帧的值
    // @frameLeft:左边记录的帧的编号
    // @frameRight:右边记录的帧的编号
    // @frameNow:当前帧的编号
    // return:直线插值计算的值
    public static float EvaluteInperpolation(float leftVal, float rightVal, int frameLeft, int frameRight, int frameNow)
    {
        float pct = (frameNow - frameLeft) * 1.0f / (frameRight - frameLeft);
        return leftVal + (rightVal - leftVal) * pct;
    }


    // 计算插值
    //public static float EvaluteInperpolation(float time, Keyframe lKf, Keyframe rKf)
    //{
    //    float totalTime = rKf.time - lKf.time;

    //    float timePct;
    //    float leftFactor;
    //    float rightFactor;

    //    if (totalTime <= 0.0001f)
    //    {
    //        timePct = (time - lKf.time) / totalTime;
    //        leftFactor = lKf.outTangent * totalTime;
    //        rightFactor = rKf.inTangent * totalTime;
    //    }
    //    else
    //    {
    //        timePct = 0.0F;
    //        leftFactor = 0;
    //        rightFactor = 0;
    //    }

    //    return HermiteInterpolate(timePct, lKf.value, leftFactor, rightFactor, rKf.value);
    //}

    //// 埃尔米特插值
    //public static float HermiteInterpolate(float t, float p0, float m0, float m1, float p1)
    //{
    //    float t2 = t * t;
    //    float t3 = t2 * t;

    //    float a = 2.0F * t3 - 3.0F * t2 + 1.0F;
    //    float b = t3 - 2.0F * t2 + t;
    //    float c = t3 - t2;
    //    float d = -2.0F * t3 + 3.0F * t2;

    //    return a * p0 + b * m0 + c * m1 + d * p1;
    //}
}
