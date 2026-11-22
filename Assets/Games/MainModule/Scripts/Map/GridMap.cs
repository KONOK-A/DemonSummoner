using System.Collections.Generic;
using Games.MainModule.Scripts.Utility;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Games.MainModule.Scripts.Map
{
    [RequireComponent(typeof(Tilemap))]
    public class GridMap : MonoBehaviour
    {
        public GridType gridType;

        public IEnumerable<Vector2Int> GetMarkedCells()
        {
            var tilemap = GetComponent<Tilemap>();
            var bounds = tilemap.cellBounds;
            for (var x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (var y = bounds.yMin; y < bounds.yMax; y++)
                {
                    if (tilemap.HasTile(new Vector3Int(x, y, 0)))
                        yield return new Vector2Int(x, y);
                }
            }
        }
    }
}
