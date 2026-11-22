using System.Collections.Generic;
using Game;
using UnityEngine;

namespace Games.MainModule.Scripts.Model
{
    public class SummonsModel : XFGameFramework.Model
    {
        public readonly Dictionary<Vector2Int, SummonBase> SummonsOnScene = new ();
    }
}