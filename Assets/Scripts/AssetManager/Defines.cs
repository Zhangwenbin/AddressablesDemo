using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MUEngine
{
    public enum LoadPriority
    {
        Default = 0,            //默认加载 异步 队列
        Prior = 10,             // 异步  不队列
    }

    public enum ECacheType
    {
        AutoDestroy = 0,
        LongTime = 1,
        ReloadDestroy = 2,
        Persistent = 3,
    }

}