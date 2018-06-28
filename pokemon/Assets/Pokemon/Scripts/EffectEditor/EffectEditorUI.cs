using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EffectEditorUI : MonoBehaviour
{
    private static EffectEditorUI s_inst;
    public static EffectEditorUI Inst
    {
        get { return s_inst; }
    }

    public AnimationCurve ani;
    public Image m_imgTemplate;         // 图片模板

    private bool m_isPlay;              // 是否在播放的标识
    private float m_runTime;            // 运行时间
    private float m_endTime;            // 播放结束时间
    private List<Image> m_listImg;      // 图片实例列表

    private EffectData m_effectData;    // 当前播放的特效的数据

    public bool IsPlay { get { return m_isPlay; } }

	void Awake ()
    {
        s_inst = this;
        m_isPlay = false;
        m_listImg = new List<Image>();

        //gameObject.hideFlags = HideFlags.NotEditable;
        //m_imgTemplate.gameObject.hideFlags = HideFlags.NotEditable;
    }
	
	void Update ()
    {
        //Debug.Log(ani.keys[0].inTangent);
        //Debug.Log(ani.keys[0].outTangent);
        //Debug.Log(ani.keys[1].inTangent);
        //Debug.Log(ani.keys[1].outTangent);


        if (!m_isPlay || m_effectData == null)
            return;

        if(m_runTime > m_endTime)
        {
            m_isPlay = false;
            HideAllImg();
            return;
        }

        int no = EffectEditorUtil.CalcFrameNo(m_runTime, m_effectData.r);
        EffectFrameData effData = EffectEditorUtil.SimulateFrameData(m_effectData, no);

        if (effData == null)
            m_isPlay = false;
        else
            UpdateByFrame(m_effectData, effData);

        m_runTime += Time.deltaTime;

        Debug.Log(m_runTime);
    }

    // 播放特效
    // @effData:特效数据
    public void Play(EffectData effData)
    {
        if (effData.aF.Length < 2)
            return;

        m_isPlay = true;
        m_effectData = effData;
        m_runTime = 0;
        m_endTime = EffectEditorUtil.CalcEffectTime(effData);

        //SimulateFrameData();
    }

    // 模拟帧数据
    //private void SimulateFrameData()
    //{
    //    int defFrameNum = m_effectData.aF.Length;
    //    for (int i = 0; i < defFrameNum - 1; ++i)
    //    {
    //        EffectFrameData frame1 = m_effectData.aF[i];
    //        EffectFrameData frame2 = m_effectData.aF[i + 1];

    //        m_listFrameData.Add(frame1);

    //        for (int j = frame1.n + 1; j < frame2.n; ++j)
    //        {
    //            EffectFrameData nFrameData = new EffectFrameData();
    //            nFrameData.n = j;

    //            m_listFrameData.Add(nFrameData);

    //            List<EffectImageData> listImgData = new List<EffectImageData>();

    //            for (int k = 0; k < frame1.aI.Length; ++k)
    //            {
    //                EffectImageData imgData1 = frame1.aI[k];
    //                EffectImageData imgData2 = GetImgDataFromFrame(frame2, imgData1.i);

    //                if(imgData2 != null)
    //                {
    //                    EffectImageData nImgData = new EffectImageData();
    //                    nImgData.i = imgData1.i;
    //                    nImgData.sX = EffectEditorUtil.EvaluteInperpolation(imgData1.sX, imgData2.sX, frame1.n, frame2.n, j);
    //                    nImgData.sY = EffectEditorUtil.EvaluteInperpolation(imgData1.sY, imgData2.sY, frame1.n, frame2.n, j);
    //                    nImgData.pX = EffectEditorUtil.EvaluteInperpolation(imgData1.pX, imgData2.pX, frame1.n, frame2.n, j);
    //                    nImgData.pY = EffectEditorUtil.EvaluteInperpolation(imgData1.pY, imgData2.pY, frame1.n, frame2.n, j);
    //                    nImgData.r = EffectEditorUtil.EvaluteInperpolation(imgData1.r, imgData2.r, frame1.n, frame2.n, j);
    //                    nImgData.c[0] = (int)EffectEditorUtil.EvaluteInperpolation(imgData1.c[0], imgData2.c[0], frame1.n, frame2.n, j);
    //                    nImgData.c[1] = (int)EffectEditorUtil.EvaluteInperpolation(imgData1.c[1], imgData2.c[1], frame1.n, frame2.n, j);
    //                    nImgData.c[2] = (int)EffectEditorUtil.EvaluteInperpolation(imgData1.c[2], imgData2.c[2], frame1.n, frame2.n, j);
    //                    nImgData.c[3] = (int)EffectEditorUtil.EvaluteInperpolation(imgData1.c[3], imgData2.c[3], frame1.n, frame2.n, j);

    //                    listImgData.Add(nImgData);
    //                }
    //                else
    //                {
    //                    listImgData.Add(imgData1);
    //                }

    //                nFrameData.aI = listImgData.ToArray();
    //            }
    //        }
    //    }

    //    m_listFrameData.Add(m_effectData.aF[defFrameNum - 1]);
    //}

    //// 从帧数据里面获取指定id的图片数据
    //private EffectImageData GetImgDataFromFrame(EffectFrameData frameData, int imgId)
    //{
    //    for(int i =0; i < frameData.aI.Length; ++i)
    //    {
    //        EffectImageData imgData = frameData.aI[i];
    //        if (imgData.i == imgId)
    //            return imgData;
    //    }

    //    return null;
    //}

    // 根据指定的帧更新显示
    // @effData:特效数据
    // @frameData:帧数据
    public void UpdateByFrame(EffectData effData, EffectFrameData frameData)
    {
        // 先创建足够的图片资源
        if(m_listImg.Count < frameData.aI.Length)
        {
            for(int i = m_listImg.Count; i < frameData.aI.Length; ++i)
            {
                Image img = Image.Instantiate(m_imgTemplate);
                img.transform.SetParent(transform);
                img.hideFlags = HideFlags.HideInHierarchy;

                m_listImg.Add(img);
            }
        }

        for(int i = 0; i < frameData.aI.Length; ++i)
        {
            Image img = m_listImg[i];
            EffectImageData imgData = frameData.aI[i];
            EffectAssetData assetData = GetEffectImgAssetData(effData, imgData.i);
            if(assetData == null)
                continue;

            img.gameObject.SetActive(true);

            img.sprite = Resources.Load<Sprite>("EffectRaw/" + assetData.n);
            img.rectTransform.sizeDelta = new Vector2(assetData.w, assetData.h);
            img.transform.localPosition = new Vector3(imgData.pX, imgData.pY, 0);
            img.transform.localRotation = Quaternion.Euler(0, 0, imgData.r);
            img.transform.localScale = new Vector2(imgData.sX, imgData.sY);
            img.color = new Color32((byte)imgData.c[0], (byte)imgData.c[1], (byte)imgData.c[2], (byte)imgData.c[3]);
        }

        for(int i = frameData.aI.Length; i < m_listImg.Count; ++i)
        {
            Image img = m_listImg[i];
            img.gameObject.SetActive(false);
        }
    }

    // 隐藏所有的图片
    private void HideAllImg()
    {
        for (int i = 0; i < m_listImg.Count; ++i)
        {
            Image img = m_listImg[i];
            img.gameObject.SetActive(false);
        }
    }

    // 获取特效的资源数据
    // 加载特效图片资源
    // @effData:特效数据
    // @id:图片id
    // return:资源数据
    private EffectAssetData GetEffectImgAssetData(EffectData effData, int id)
    {
        for (int i = 0; i < effData.aA.Length; ++i)
        {
            EffectAssetData assetData = effData.aA[i];
            if (assetData.i == id)
            {
                return assetData;
            }
        }

        return null;
    }
}
