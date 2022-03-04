using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;//游戏的实例
    public static GameManager Instance//代理模式
    {
        get
        {
            if(instance == null)
            {
                instance = Transform.FindObjectOfType<GameManager>();
            }
            return instance;
        }
    }
    [Header("-----UI----------")]
    public List<PlayingUI> playingUIList = new List<PlayingUI>();//游戏UI
    [Header("-----游戏数据-----")]
    [Header("1.Combo数量")]
    public int combo = 0;//Combo数量
    [Header("2.分数")]
    public int score = 0;//分数
    [Header("3.评分")]
    public string grade = "";//评分
    [Header("4.当前精准度")]
    public float currentDeviation = 0;//当前精准度
    [Header("5.各个音符的偏移")]
    public List<float> deviationList = new List<float>();//各个音符的偏移
    [Header("6.理论最高分")]
    public int maxScore = 0;//理论最高分
    [Header("7.各个评分数量")]
    public int excatNum = 0;
    public int greatNum = 0;
    public int rightNum = 0;
    public int badNum = 0;
    public int missNum = 0;
    public void RefreshPlayingUI(int combo,int score,string grade)
    {
        foreach(var item in playingUIList)
        {
            item.Refresh(combo,score,grade,-1);
        }
    }
    public void RefreshData(int addCombo,int addScore,string grade,float currentDeviation)
    {
        if(addCombo < 0)
        {
            combo = 0;
        }
        else
        {
            this.combo += addCombo;
            this.score += addScore;
        }
        
        this.grade = grade;
        if(grade == "Excat")
        {
            excatNum++;
        }
        else if(grade == "Great")
        {
            greatNum++;
        }
        else if(grade == "Right")
        {
            rightNum++;
        }
        else if(grade == "Bad")
        {
            badNum++;
        }
        else if(grade == "Miss")
        {
            missNum++;
        }
        if(currentDeviation < 10000)
        {
            this.currentDeviation = currentDeviation;
            deviationList.Add(currentDeviation);
        }    
        RefreshPlayingUI(combo,score,grade);
    }
}
//This code is writed by Ybr.