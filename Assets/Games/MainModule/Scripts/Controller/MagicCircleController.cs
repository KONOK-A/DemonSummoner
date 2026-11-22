using Game;
using Games.MainModule.Scripts.Model;
using Games.MainModule.Scripts.Utility;
using Games.MainModule.View.MagicCircle;
using UnityEngine;

namespace Games.MainModule.Scripts.Controller
{
    public class MagicCircleController : XFGameFramework.Controller
    {
        private MagicCircleModel GetMagicCircleModel()
        {
            MagicCircleModel model = Module.GetModel<MagicCircleModel>();
            if (model == null)
            {
                model = new MagicCircleModel();
                Module.AddModel(model);
            }
            return model;
        }

        internal bool TryGenerateMagicCircle(SummonMagicType summonMagicType, Vector2Int gridPos)
        {
            //根据法术类型释放法阵
            var circle = Module.LoadView<MagicCircle>("MagicCircle", null, summonMagicType, gridPos);
            if (circle == null)
                return false;

            circle.transform.position = new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, 0);
            GetMagicCircleModel().MagicCircleList.Add(circle);
            return true;
        }

        public void ClearAllMagicCircles()
        {
            if(GetMagicCircleModel().MagicCircleList.Count == 0) return;
            foreach (var magic in GetMagicCircleModel().MagicCircleList)
            {
                magic.Close();
            }
            GetMagicCircleModel().MagicCircleList.Clear();
        }
    }
}