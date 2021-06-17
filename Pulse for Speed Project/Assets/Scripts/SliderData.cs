using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderData : MonoBehaviour
{
    // Retrive average and max heart rate from PortReading
    private uint p1_avg;
    private uint p1_max;
    private uint p2_avg;
    private uint p2_max;   

    //PLAYER1'S DISPLAY
    [Header("D1: AVG and MAX")]
    [Header("DISPLAY 1")]
  
    public Text p1_avgTxt1;
    public Text p1_maxTxt1;
    public Text p2_avgTxt1;
    public Text p2_maxTxt1;

    [Header("D1: Data positions")]
    public Slider sliderP1_1;
    public Slider sliderP2_1;

    //PLAYER2'S DISPLAY
    [Header("D2: AVG and MAX")]
    [Header("DISPLAY 2")]

    public Text p1_avgTxt2;
    public Text p1_maxTxt2;
    public Text p2_avgTxt2;
    public Text p2_maxTxt2;
 
    [Header("D2: Data positions")]
    public Slider sliderP1_2;
    public Slider sliderP2_2;

    // Start is called before the first frame update
    void Start()
    {

	    p1_avg = PortReading.AverageHeartRate(1);
		p1_max = PortReading.MaxHeartRate(1);
		p2_avg = PortReading.AverageHeartRate(2);
		p2_max = PortReading.MaxHeartRate(2);   

        //DISPLAY TEXTS: THE PLAYERS AVERAGE AND MAXIMUM HEART RATES

        //DISPLAY 1
        p1_avgTxt1.text = p1_avg.ToString();
        p1_maxTxt1.text = p1_max.ToString();
        p2_avgTxt1.text = p2_avg.ToString();
        p2_maxTxt1.text = p2_max.ToString();

        //DISPLAY 2
        p1_avgTxt2.text = p1_avg.ToString();
        p1_maxTxt2.text = p1_max.ToString();
        p2_avgTxt2.text = p2_avg.ToString();
        p2_maxTxt2.text = p2_max.ToString();

        //PLOT DATA ON SLIDERS: DATA POSITIONS
        /* Examples avg max HRs: Kid (200), Teen (190), Adult (180), Elder (160)
           Slider' range = 200-160 Note that it's reversed
        */

        //DISPLAY 1    
        sliderP1_1.value = p1_max;
        sliderP2_1.value = p2_max;

        //DISPLAY 2
        sliderP1_2.value = p1_max;
        sliderP2_2.value = p2_max;
    }
}
