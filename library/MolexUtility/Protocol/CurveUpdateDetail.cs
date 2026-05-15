using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MolexUtility.Protocol
{
    public class CurveUpdateDetail
    {
        /// <summary>
        /// x轴标题，UpdateType为Init时才有作用
        /// </summary>
        public string XAixsTitle { get; set; }

        /// <summary>
        /// Y轴标题，UpdateType为Init时才有作用
        /// </summary>
        public string YAxisTitle { get; set; }

        /// <summary>
        /// 曲线类型，可以为线，也可以为点，UpdateType为Init时才有作用
        /// </summary>
        public CurveType Type { get; set; }

        public System.Drawing.Color CurveColor { get; set; }

        /// <summary>
        /// x轴起始坐标，UpdateType为Init时才有作用
        /// </summary>
        public double XAixsBegin { get; set; }

        /// <summary>
        /// x轴终止坐标，会根据实际情况显示最大的值，UpdateType为Init时才有作用
        /// </summary>
        public double XAxisEnd { get; set; }

        /// <summary>
        /// x轴标题，UpdateType为Init、FirstPoint、AddPoint时才有作用
        /// </summary>
        public string SeriesName { get; set; }

        /// <summary>
        /// x轴步长，UpdateType为FirstPoint、AddPoint时只有第一个值有效，如果为AllPoint时多个值有效
        /// </summary>
        public List<double> XAxisStep { get; set; }

        /// <summary>
        /// Y轴当前点的值，UpdateType为FirstPoint、AddPoint时只有第一个值有效，如果为AllPoint时多个值有效
        /// </summary>
        public List<double> YAxisValue { get; set; }

        /// <summary>
        /// 通信的类型
        /// </summary>
        public CurveUpdate UpdateType { get; set; }

        /// <summary>
        /// 需要更新的对象名称
        /// </summary>
        public string TargetName { get; set; }

        /// <summary>
        /// X轴刻度个数
        /// </summary>
        public int XScaleCount { get; set; }
        public CurveUpdateDetail()
        {
            TargetName = "";
            XAixsTitle = "";
            YAxisTitle = "";
            Type = CurveType.Line;
            XAixsBegin = 0;
            XAxisEnd = -1;
            SeriesName = "";
            XAxisStep = new List<double>();
            YAxisValue = new List<double>();
            UpdateType = CurveUpdate.Default;
            CurveColor = System.Drawing.Color.Black;
            XScaleCount = 10;
        }
    }

    public enum CurveType
    {
        //类型为线
        Line=0,
        //类型为点
        Point=1
    }

    public enum CurveUpdate
    {
        Default=0,
        //初始化显示区域
        Init,
        //曲线第一个点
        FirstPoint,
        //曲线增加点
        AddPoint,
        //所有点
        AllPoint
    }
}
