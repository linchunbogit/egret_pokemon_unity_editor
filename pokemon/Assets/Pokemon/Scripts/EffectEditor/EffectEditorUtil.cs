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
        int frameNum = effData.aF.Length;
        if (effData.r == 0 || frameNum < 1)
            return 0;

        int lastFrameIdx = effData.aF[frameNum - 1].n + 1; ;
        return (1.0f / effData.r) * lastFrameIdx;
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

    // 模拟特效指定帧的帧数据
    // @effectData:特效数据
    // @frameNo:帧的编号
    // return:有效的帧序号，返回对应的帧数据；否则null
    public static EffectFrameData SimulateFrameData(EffectData effectData, int frameNo)
    {
        int defFrameNum = effectData.aF.Length;
        if (effectData.aF.Length < 2 || frameNo < 0)
            return null;

        EffectFrameData lastFrameData = effectData.aF[defFrameNum - 1];
        if (frameNo > lastFrameData.n)
            return null;

        // 找出最近的帧数据索引
        int targetLeftFrameIdx = 0;
        for (int i = 0; i < defFrameNum - 1; ++i)
        {
            EffectFrameData tmpFrame = effectData.aF[i];
            if (frameNo >= tmpFrame.n)
            {
                targetLeftFrameIdx = i;
            }
        }

        EffectFrameData frame1 = effectData.aF[targetLeftFrameIdx];
        EffectFrameData frame2 = effectData.aF[targetLeftFrameIdx + 1];

        EffectFrameData nFrameData = new EffectFrameData();
        nFrameData.n = frameNo;

        List<EffectImageData> listImgData = new List<EffectImageData>();

        for (int k = 0; k < frame1.aI.Length; ++k)
        {
            EffectImageData imgData1 = frame1.aI[k];
            EffectImageData imgData2 = GetImgDataFromFrame(frame2, imgData1.i);

            if (imgData2 != null)
            {
                EffectImageData nImgData = new EffectImageData();
                nImgData.i = imgData1.i;
                nImgData.sX = EffectEditorUtil.EvaluteInperpolation(imgData1.sX, imgData2.sX, frame1.n, frame2.n, frameNo);
                nImgData.sY = EffectEditorUtil.EvaluteInperpolation(imgData1.sY, imgData2.sY, frame1.n, frame2.n, frameNo);
                nImgData.pX = EffectEditorUtil.EvaluteInperpolation(imgData1.pX, imgData2.pX, frame1.n, frame2.n, frameNo);
                nImgData.pY = EffectEditorUtil.EvaluteInperpolation(imgData1.pY, imgData2.pY, frame1.n, frame2.n, frameNo);
                nImgData.r = EffectEditorUtil.EvaluteInperpolation(imgData1.r, imgData2.r, frame1.n, frame2.n, frameNo);
                nImgData.c[0] = (int)EffectEditorUtil.EvaluteInperpolation(imgData1.c[0], imgData2.c[0], frame1.n, frame2.n, frameNo);
                nImgData.c[1] = (int)EffectEditorUtil.EvaluteInperpolation(imgData1.c[1], imgData2.c[1], frame1.n, frame2.n, frameNo);
                nImgData.c[2] = (int)EffectEditorUtil.EvaluteInperpolation(imgData1.c[2], imgData2.c[2], frame1.n, frame2.n, frameNo);
                nImgData.c[3] = (int)EffectEditorUtil.EvaluteInperpolation(imgData1.c[3], imgData2.c[3], frame1.n, frame2.n, frameNo);

                listImgData.Add(nImgData);
            }
            else
            {
                listImgData.Add(imgData1);
            }

            nFrameData.aI = listImgData.ToArray();
        }

        return nFrameData;
    }

    // 从帧数据里面获取指定id的图片数据
    // @frameData:帧数据
    // @imgId:要获取的图片id
    public static EffectImageData GetImgDataFromFrame(EffectFrameData frameData, int imgId)
    {
        for (int i = 0; i < frameData.aI.Length; ++i)
        {
            EffectImageData imgData = frameData.aI[i];
            if (imgData.i == imgId)
                return imgData;
        }

        return null;
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
