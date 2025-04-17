using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace kuro
{
    [Serializable]
    public class AtlasDb
    {
        public List<DynamicAtlasData> DynamicAtlasList = new();
    }
}