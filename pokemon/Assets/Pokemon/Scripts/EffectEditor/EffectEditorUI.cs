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
    private List<EffectFrameData> m_listFrameData;      // 模拟后得到的帧数据

    public bool IsPlay { get { return m_isPlay; } }

	void Awake ()
    {
        s_inst = this;
        m_isPlay = false;
        m_listImg = new List<Image>();
        m_listFrameData = new List<EffectFrameData>();

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

        int no = EffectEditorUtil.CalcFrameNo(m_runTime, m_effectData.rate);
        if(no < m_listFrameData.Count)
            UpdateByFrame(m_effectData, m_listFrameData[no]);
        else
            m_isPlay = false;


        if (m_runTime >= m_endTime)
        {
            m_isPlay = false;
        }

        m_runTime += Time.deltaTime;

        Debug.Log(m_runTime);

    }

    // 播放特效
    // @effData:特效数据
    public void Play(EffectData effData)
    {
        if (effData.arrFrameData.Length < 2)
            return;

        m_listFrameData.Clear();

        m_isPlay = true;
        m_effectData = effData;
        m_runTime = 0;
        m_endTime = EffectEditorUtil.CalcEffectTime(effData);

        SimulateFrameData();
    }

    // 模拟帧数据
    private void SimulateFrameData()
    {
        int defFrameNum = m_effectData.arrFrameData.Length;
        for (int i = 0; i < defFrameNum - 1; ++i)
        {
            EffectFrameData frame1 = m_effectData.arrFrameData[i];
            EffectFrameData frame2 = m_effectData.arrFrameData[i + 1];

            m_listFrameData.Add(frame1);

            for (int j = frame1.frameNo + 1; j < frame2.frameNo; ++j)
            {
                EffectFrameData nFrameData = new EffectFrameData();
                nFrameData.frameNo = j;

                m_listFrameData.Add(nFrameData);

                List<EffectImageData> listImgData = new List<EffectImageData>();

                for (int k = 0; k < frame1.arrImgData.Length; ++k)
                {
                    EffectImageData imgData1 = frame1.arrImgData[k];
                    EffectImageData imgData2 = GetImgDataFromFrame(frame2, imgData1.imgId);

                    if(imgData2 != null)
                    {
                        EffectImageData nImgData = new EffectImageData();
                        nImgData.imgId = imgData1.imgId;
                        nImgData.scaleX = EffectEditorUtil.EvaluteInperpolation(imgData1.scaleX, imgData2.scaleX, frame1.frameNo, frame2.frameNo, j);
                        nImgData.scaleY = EffectEditorUtil.EvaluteInperpolation(imgData1.scaleY, imgData2.scaleY, frame1.frameNo, frame2.frameNo, j);
                        nImgData.posX = EffectEditorUtil.EvaluteInperpolation(imgData1.posX, imgData2.posX, frame1.frameNo, frame2.frameNo, j);
                        nImgData.posY = EffectEditorUtil.EvaluteInperpolation(imgData1.posY, imgData2.posY, frame1.frameNo, frame2.frameNo, j);
                        nImgData.rotate = EffectEditorUtil.EvaluteInperpolation(imgData1.rotate, imgData2.rotate, frame1.frameNo, frame2.frameNo, j);
                        nImgData.color[0] = (int)EffectEditorUtil.EvaluteInperpolation(imgData1.color[0], imgData2.color[0], frame1.frameNo, frame2.frameNo, j);
                        nImgData.color[1] = (int)EffectEditorUtil.EvaluteInperpolation(imgData1.color[1], imgData2.color[1], frame1.frameNo, frame2.frameNo, j);
                        nImgData.color[2] = (int)EffectEditorUtil.EvaluteInperpolation(imgData1.color[2], imgData2.color[2], frame1.frameNo, frame2.frameNo, j);
                        nImgData.color[3] = (int)EffectEditorUtil.EvaluteInperpolation(imgData1.color[3], imgData2.color[3], frame1.frameNo, frame2.frameNo, j);

                        listImgData.Add(nImgData);
                    }
                    else
                    {
                        listImgData.Add(imgData1);
                    }

                    nFrameData.arrImgData = listImgData.ToArray();
                }
            }
        }

        m_listFrameData.Add(m_effectData.arrFrameData[defFrameNum - 1]);
    }

    // 从帧数据里面获取指定id的图片数据
    private EffectImageData GetImgDataFromFrame(EffectFrameData frameData, int imgId)
    {
        for(int i =0; i < frameData.arrImgData.Length; ++i)
        {
            EffectImageData imgData = frameData.arrImgData[i];
            if (imgData.imgId == imgId)
                return imgData;
        }

        return null;
    }

    // 根据指定的帧更新显示
    // @effData:特效数据
    // @frameData:帧数据
    public void UpdateByFrame(EffectData effData, EffectFrameData frameData)
    {
        // 先创建足够的图片资源
        if(m_listImg.Count < frameData.arrImgData.Length)
        {
            for(int i = m_listImg.Count; i < frameData.arrImgData.Length; ++i)
            {
                Image img = Image.Instantiate(m_imgTemplate);
                img.transform.SetParent(transform);
                img.hideFlags = HideFlags.HideInHierarchy;

                m_listImg.Add(img);
            }
        }

        for(int i = 0; i < frameData.arrImgData.Length; ++i)
        {
            Image img = m_listImg[i];
            EffectImageData imgData = frameData.arrImgData[i];
            EffectAssetData assetData = GetEffectImgAssetData(effData, imgData.imgId);
            if(assetData == null)
                continue;

            img.gameObject.SetActive(true);

            img.sprite = Resources.Load<Sprite>("EffectRaw/" + assetData.imgName);
            img.rectTransform.sizeDelta = new Vector2(assetData.width, assetData.height);
            img.transform.localPosition = new Vector3(imgData.posX, imgData.posY, 0);
            img.transform.localRotation = Quaternion.Euler(0, 0, imgData.rotate);
            img.transform.localScale = new Vector2(imgData.scaleX, imgData.scaleY);
            img.color = new Color32((byte)imgData.color[0], (byte)imgData.color[1], (byte)imgData.color[2], (byte)imgData.color[3]);
        }

        for(int i = frameData.arrImgData.Length; i < m_listImg.Count; ++i)
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
        for (int i = 0; i < effData.arrAssetData.Length; ++i)
        {
            EffectAssetData assetData = effData.arrAssetData[i];
            if (assetData.imgId == id)
            {
                return assetData;
            }
        }

        return null;
    }
}
