using System;
using System.Collections.Generic;
using Games.MainModule.Configs.Summons;
using Games.MainModule.Scripts.Const;
using Games.MainModule.Scripts.Map;
using Games.MainModule.Scripts.Model;
using Games.MainModule.Scripts.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Games.MainModule.Scripts.Controller
{
    public class MapInfoController : XFGameFramework.Controller
    {
        public MapModel GetMapModel()
        {
            MapModel model = Module.GetModel<MapModel>();
            if (model == null)
            {
                model = new MapModel();
                Module.AddModel(model);
            }
            return model;
        }
        public void InitDataList(Vector2Int adventurerStart, int adventurerCount)
        {
            ClearBattleMap();
            var scene = SceneManager.GetActiveScene();
            var tiles = new Dictionary<Vector2Int, TileDetails>();
            int laneEnd = adventurerStart.x;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var layer in root.GetComponentsInChildren<GridMap>(includeInactive: true))
                {
                    bool canSummon = layer.gridType == GridType.CanSummon;
                    foreach (var cell in layer.GetMarkedCells())
                    {
                        if (cell.y != adventurerStart.y)
                            throw new InvalidOperationException(
                                $"Battle scene '{scene.name}' has a grid marker outside the lane at {cell}.");

                        if (tiles.TryGetValue(cell, out var existing))
                        {
                            if (existing.CanSummon != canSummon)
                                throw new InvalidOperationException(
                                    $"Battle scene '{scene.name}' has conflicting grid markers at {cell}.");
                            continue;
                        }

                        tiles.Add(cell, new TileDetails
                        {
                            GridX = cell.x,
                            GridY = cell.y,
                            CanSummon = canSummon
                        });
                        laneEnd = Mathf.Max(laneEnd, cell.x);
                    }
                }
            }

            if (tiles.Count == 0)
                throw new InvalidOperationException($"Battle scene '{scene.name}' has no marked grid cells.");

            for (int i = 0; i < adventurerCount; i++)
            {
                var cell = adventurerStart - Vector2Int.right * i;
                if (!tiles.ContainsKey(cell))
                    throw new InvalidOperationException(
                        $"Battle scene '{scene.name}' is missing adventurer starting cell {cell}.");
            }

            if (laneEnd == adventurerStart.x)
                throw new InvalidOperationException($"Battle scene '{scene.name}' has no lane ahead of {adventurerStart}.");

            for (int x = adventurerStart.x + 1; x < laneEnd; x++)
            {
                var cell = new Vector2Int(x, adventurerStart.y);
                if (!tiles.ContainsKey(cell))
                    throw new InvalidOperationException($"Battle scene '{scene.name}' has a gap in the lane at {cell}.");
            }

            GetMapModel().TileDetailsDict = tiles;
        }

        public void ClearBattleMap()
        {
            Module.GetModel<MapModel>()?.TileDetailsDict.Clear();
        }

        public TileDetails GetTileDetailsByGridPosition(Vector2Int gridPos)
        {
            return GetMapModel().TileDetailsDict.GetValueOrDefault(gridPos);
        }
        
        internal void SetSummonMagicCircle(SummonDataSo summonData, Vector2Int gridPos, int paidManaCost)
        {
            TileDetails tileDetails = GetTileDetailsByGridPosition(gridPos);
            tileDetails.CanSummon = false;
            tileDetails.SummonData = summonData;
            tileDetails.PaidManaCost = paidManaCost;
            tileDetails.AboutToSummon = true;
            tileDetails.IsHaveCharacter = false;
        }
        public void MagicCircleLaunch()
        {
            bool playedSummonSound = false;
            foreach (var tile in GetMapModel().TileDetailsDict)
            {
                if (tile.Value.AboutToSummon)
                {
                    
                    if (!playedSummonSound)
                    {
                        Module.LoadController<AudioController>().PlaySound(AudioCueNames.Summonsound1);
                        playedSummonSound = true;
                    }
                    
                    //实际生成召唤物
                    var summon = Module.LoadController<SummonsController>()
                        .GenerateSummon(tile.Key, tile.Value.SummonData, tile.Value.PaidManaCost);
                    if (summon == null)
                        continue;
                    tile.Value.AboutToSummon = false;
                    tile.Value.SummonData = null;
                    tile.Value.PaidManaCost = null;
                    tile.Value.CharacterType = CharacterType.Monster;
                    tile.Value.IsHaveCharacter = true;
                    tile.Value.CanSummon = false;
                }
            }
        }

    }
}
