using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EG
{
	public class Vector3Jason
	{
        public static string SerializeVector3(object obj)
        {
            Vector3 vec = (Vector3)obj;
            return vec.x + "|" + vec.y + "|" + vec.z;
        }

        public static object DeserializeVector3(string str)
        {
            string[] vs = JsonUtil.StringToStringArray(str);
            Vector3 vc = Vector3.zero;
            if(vs.Length == 3)
            {
                vc.x = float.Parse(vs[0]);
                vc.y = float.Parse(vs[1]);
                vc.z = float.Parse(vs[2]);
            }
            return vc;
        }
	}
    public class Vector4Jason
    {
        public static string SerializeVector4(object obj)
        {
            Vector4 vec = (Vector4)obj;
            return vec.x + "|" + vec.y + "|" + vec.z + "|" + vec.w;
        }

        public static object DeserializeVector4(string str)
        {
            string[] vs = JsonUtil.StringToStringArray(str);
            Vector4 vc = Vector4.zero;
            if (vs.Length == 4)
            {
                vc.x = float.Parse(vs[0]);
                vc.y = float.Parse(vs[1]);
                vc.z = float.Parse(vs[2]);
                vc.w = float.Parse(vs[3]);
            }
            return vc;
        }
    }
    /// <summary>
    /// 供fastJSON使用的Vector2类型序列化解析器
    /// </summary>
    public class Vector2Jason
    {
        /// <summary>
        /// 将Vector2序列化为文本
        /// </summary>
        /// <param name="obj">需要序列化的Vector2数值</param>
        /// <returns>序列化后的文本</returns>
        public static string SerializeVector2(object obj)
        {
            Vector2 vec = (Vector2)obj;
            return vec.x + "|" + vec.y;
        }

        /// <summary>
        /// 将文本反序列化成Vector2
        /// </summary>
        /// <param name="str">需要反序列化的文本</param>
        /// <returns>得到的Vector2数值</returns>
        public static object DeserializeVector2(string str)
        {
            string[] strArray = JsonUtil.StringToStringArray(str);
            Vector2 vec = Vector2.zero;
            if (strArray.Length == 2)
            {
                vec.x = float.Parse(strArray[0]);
                vec.y = float.Parse(strArray[1]);
            }
            return vec;
        }
    }

    public class QuaternionJason
    {
        public static string SerilizeQuaternion(object obj)
        {
            Quaternion vec = (Quaternion)obj;
            return vec.w + "|" + vec.x + "|" + vec.y + "|" + vec.z;
        }

        public static object DserializeQuaternion(string str)
        {
            string[] vs = JsonUtil.StringToStringArray(str);
            Quaternion vc = Quaternion.identity;
            if (vs.Length == 4)
            {
                vc.w = float.Parse(vs[0]);
                vc.x = float.Parse(vs[1]);
                vc.y = float.Parse(vs[2]);
                vc.z = float.Parse(vs[3]);
            }
            return vc;
        }
    }
}
