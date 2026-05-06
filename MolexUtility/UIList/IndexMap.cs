using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace MolexUtility.UIList
{
    /// <summary>
    /// list的列和产品序号以及对应测试项在alltestinfo对应index的对应关系，方便后续list和测试信息对应
    /// </summary>
     [Serializable]
    public class IndexMap
    {
        /// <summary>
        /// 行号
        /// </summary>
        public int RowIndex { get; set; }

        /// <summary>
        /// 产品序号
        /// </summary>
        public int ProductIndex { get; set; }

        /// <summary>
        /// 参数在ShowContent中testinfo对应的序列号
        /// </summary>
        public List<int> ParamIndex { get; set; }

        
        public IndexMap()
        {
            RowIndex = -1;
            ProductIndex = -1;
            ParamIndex = new List<int>();
           
        }

        /// <summary>
        /// 克隆一个对象
        /// </summary>
        /// <returns>返回克隆对象</returns>
        public IndexMap Clone()
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, this);
            stream.Position = 0;
            return formatter.Deserialize(stream) as IndexMap;
        }
    }
}
