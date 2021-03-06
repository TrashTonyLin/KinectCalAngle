﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;   //For Point
using Microsoft.Kinect;  //For DepthImagePixel[]
using System.Windows.Media; //For DrawingContext dc
using System.Windows.Media.Media3D;
using System.Globalization;

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
   
    class Target
    {
        private double TheDeepestDetectDistance = 2.5;
        public int TargetID;
        Point3D XYZ;
        bool TrackState = false;
        double  UU, VV, AverageUU, AverageVV, CenterUU, CenterVV;// KinectHeight = 0.88;  //x, y, z, this uv,average uv, center uv
      
        int uvRange = 10,  SearchRange = 10, CenterUVrange = 20;

        Point ColorCenter = new Point(0, 0);
        private List<double> xray = new List<double>() { };
        private List<double> yoyo = new List<double>() { };
        private List<double> zooo = new List<double>() { };
        private List<double> high = new List<double>() { };       
 
        private readonly Pen PenGray = new Pen(Brushes.Gray, 1);

        public Target(int myID)
        {
            TargetID = myID;
        }


        public void Setting(int XX, int YY, double UU, double VV ,int[] boolPixels)
        {           
            this.ColorCenter.X = XX;
            this.ColorCenter.Y = YY;
            this.CenterUU = UU;
            this.CenterVV = VV;
            this.AverageUU = this.CenterUU;
            this.AverageVV = this.CenterVV;
            TrackState = true;      
         }



        public void Cal(byte[] colorPixels, DepthImagePixel[] depthPixels, SkeletonPoint[] ColorInSkel, DepthImagePixel[] depth4background, int[] boolpixel)
        {
            int ColorCenterXsum = 0, ColorCenterYsum = 0, uuSum = 0, vvSum = 0, Cnt = 0;

            List<double> Xsum = new List<double>();
            List<double> Ysum = new List<double>();
            List<double> Zsum = new List<double>();
          
            for (int i = (int)ColorCenter.X - SearchRange; i < ColorCenter.X + SearchRange; i++)
            {
                for (int j = (int)ColorCenter.Y - SearchRange; j < ColorCenter.Y + SearchRange; j++)
                {
                    if (i <= 0 || i >= 639 || j <= 0 || j >= 479) break;   //avoid edge prob.

                    int di = (i + j * 640);   //di = depthpixel index                 
                    int ci = di * 4;          //ci = colorPixel index

                    UU = -0.169 * colorPixels[ci + 2] - 0.331 * colorPixels[ci + 1] + 0.5 * colorPixels[ci] + 128;
                    VV = 0.5 * colorPixels[ci + 2] - 0.419 * colorPixels[ci + 1] - 0.081 * colorPixels[ci] + 128;

                    if (UU > AverageUU - uvRange && UU < AverageUU + uvRange
                     && VV > AverageVV - uvRange && VV < AverageVV + uvRange
                     && UU > CenterUU - CenterUVrange && UU < CenterUU + CenterUVrange
                     && VV > CenterVV - CenterUVrange && VV < CenterVV + CenterUVrange
                     && (boolpixel[di] == 0 || boolpixel[di] == TargetID))
                    //&& Math.Abs(depthPixels[di].Depth - depth4background[di].Depth) > DepthRange    //深度背景相減
                    //&& (ColorInSkel[di].Z != 0  &&  ColorInSkel[di].Z < 2.5 ))      //這裡先不管Z值部分，所以先不做
                    {                      
                        boolpixel[di] = TargetID;
                        uuSum += (int)UU;
                        vvSum += (int)VV;
                        colorPixels[ci + 0] = 0; // (byte)depthPixels[depthIndex].Depth;
                        colorPixels[ci + 1] = 0; // (byte)depthPixels[depthIndex].Depth;
                        colorPixels[ci + 2] = 0; // (byte)depthPixels[depthIndex].Depth;

                        ColorCenterXsum += i;
                        ColorCenterYsum += j;
                        Cnt ++;                  //顏色追蹤歸顏色追蹤，Z值追蹤歸Z值追蹤 ；即使zCnt值不夠，UV值、中心點仍繼續更新

                        if (ColorInSkel[di].Z != 0 && ColorInSkel[di].Z < TheDeepestDetectDistance)  //2.5  // 必須要先轉，因為在後面做平均之後值會跑掉(雖然理論上不會阿QAQ)
                        {
                            Xsum.Add(ColorInSkel[di].X);
                            Ysum.Add(ColorInSkel[di].Y);
                            Zsum.Add(ColorInSkel[di].Z);                           
                        }
                    }   
                    else boolpixel[di] = 0;
                }
            }           
            
            if (Cnt > 2)
            {
                AverageUU = uuSum / Cnt;
                AverageVV = vvSum / Cnt;
                ColorCenter.X = ColorCenterXsum / Cnt;
                ColorCenter.Y = ColorCenterYsum / Cnt;

                if (Zsum.Count > 3)
                {
                    XYZ.X = Xsum.Average();
                    XYZ.Y = Ysum.Average();
                    XYZ.Z = Zsum.Average();                       
                }
                //height = XYZ.Z*  Math.Sin((Math.PI / 180) * ((240 - ColorCenter.Y) / 240 * 27)) + KinectHeight;  // 21.5=>27
                SearchRange = 25; 
                TrackState = true;               
            }
                      
            else
            {
                TrackState = false;
                SearchRange = 50;
            }

            ColorCenterXsum = 0; ColorCenterYsum = 0; Cnt = 0; uuSum = 0; vvSum = 0;           
        }

        public bool IsTracked()
        {
           return TrackState;

        }

        public Point point2D()
        {
            return ColorCenter;
        }

        public Point3D point3D()
        {
            return XYZ;
        }

        public void LabelTarget(DrawingContext dc, Brush brush ,bool ShowXYZisChecked)
        {                       
            if (TrackState)
            {                
                dc.DrawEllipse(null, new Pen(brush,2), point2D(), null, SearchRange, null, SearchRange, null);
                if (ShowXYZisChecked) dc.DrawText(new FormattedText(point3D().X.ToString("f2") + "," + point3D().Y.ToString("f2") + "," + point3D().Z.ToString("f2"),//+ "," + height.ToString("f2"),
                                      CultureInfo.GetCultureInfo("en-us"),
                                      FlowDirection.LeftToRight,
                                      new Typeface("Verdana"),
                                      12, brush),
                                      new Point(point2D().X, point2D().Y + 10));
            }  
            else 
                dc.DrawEllipse(null, PenGray, point2D(), null, SearchRange, null, SearchRange, null); 
        }

        public void InsertData()
        {
            xray.Add(XYZ.X); yoyo.Add(XYZ.Y); zooo.Add(XYZ.Z); 
        }

        public void clearlist()
        {
            xray.Clear(); yoyo.Clear(); zooo.Clear(); high.Clear();
        }
              
        public void Del(Point Center, int[] boolPixels)
        {
            int SearchRange = 30;
            for (int i = (int)Center.X - SearchRange; i < Center.X + SearchRange; i++)
            {
                for (int j = (int)Center.Y - SearchRange; j < Center.Y + SearchRange; j++)
                {
                    if (i <= 0 || i >= 639 || j <= 0 || j >= 479) break;
                    int ci = (int)(i + 640 * j);
                    if (boolPixels[ci] == TargetID) boolPixels[ci] = 0;
                }
            }

            ColorCenter = new Point(0, 0);
            CenterUU = 0; 
            CenterVV = 0;
            AverageUU = 0;
            AverageVV = 0;
            TrackState = false;
        }
        
        public string UV()
        {
            string UV = AverageUU.ToString() + " , "+AverageVV.ToString();
            return  UV;
        }
    }
}
