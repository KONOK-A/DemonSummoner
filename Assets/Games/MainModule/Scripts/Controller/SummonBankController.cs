using System;
using System.Collections.Generic;
using System.Linq;
using Games.MainModule.Configs.SummonMagic;
using Games.MainModule.Configs.Summons;
using Games.MainModule.Scripts.Model;
using Games.MainModule.Scripts.Utility;
using XFABManager;
using Random = UnityEngine.Random;

namespace Games.MainModule.Scripts.Controller
{
    public class SummonBankController : XFGameFramework.Controller
    {
        private SummonBankModel GetSummonBankModel()
        {
            SummonBankModel model = Module.GetModel<SummonBankModel>();
            if (model == null)
            {
                model = new SummonBankModel();
                Module.AddModel(model);
            }

            return model;
        }

        public void InitDataList()
        {
            var datas = AssetBundleManager.LoadAllAssets<SummonDataSo>(Module.ProjectName, "configs");
            GetSummonBankModel().SummonDataSoList = datas.Where(summon => summon != null).Distinct().ToList();
        }

        public SummonDataSo GetSummonDataByName(string name)
        {
            return GetSummonBankModel().SummonDataSoList.FirstOrDefault(so => so.summonName == name);
        }

        private SummonMagic FindSpell(SummonMagicType type) =>
            Module.LoadController<SummonerController>().GetSummonerModel().SummonMagicConfig.SummonMagicList
                .Find(spell => spell.summonMagicType == type);

        private IReadOnlyList<SummonDataSo> GetUniformPool(SummonMagic spell)
        {
            var summons = GetSummonBankModel().SummonDataSoList;
            if (spell.UniformOccupation == OccupationType.None)
                return summons;
            return summons.Where(summon => summon.occupationType == spell.UniformOccupation).ToArray();
        }

        public IReadOnlyList<(string Name, int Weight)> GetSummonWeights(SummonMagicType magicType)
        {
            var spell = FindSpell(magicType);
            if (spell == null || magicType == SummonMagicType.Orientation)
                return Array.Empty<(string Name, int Weight)>();

            if (magicType is SummonMagicType.Random or SummonMagicType.Skull)
                return GetUniformPool(spell).Select(summon => (summon.summonName, 1)).ToArray();

            var summons = GetSummonBankModel().SummonDataSoList;
            var weights = spell.Weights.Select(row => (Name: row.Summon?.summonName, Weight: Math.Max(0, row.Weight))).ToArray();
            return weights.Where(row => row.Name != null
                ? summons.Any(summon => summon.summonName == row.Name)
                : summons.Any(summon => weights.All(named => named.Name != summon.summonName))).ToArray();
        }

        internal IReadOnlyList<SummonDataSo> DrawRandomPair()
        {
            var pool = GetUniformPool(FindSpell(SummonMagicType.Random));
            if (pool.Count < 2)
                return Array.Empty<SummonDataSo>();

            int first = Random.Range(0, pool.Count);
            int second = Random.Range(0, pool.Count - 1);
            if (second >= first)
                second++;
            return Array.AsReadOnly(new[] { pool[first], pool[second] });
        }

        public SummonDataSo DrawSummon(SummonMagicType summonMagicType)
        {
            var weights = GetSummonWeights(summonMagicType);
            int total = weights.Sum(entry => entry.Weight);
            if (total == 0)
                return null;

            int roll = Random.Range(0, total);
            foreach (var entry in weights)
            {
                roll -= entry.Weight;
                if (roll >= 0)
                    continue;

                if (entry.Name != null)
                    return GetSummonDataByName(entry.Name);

                var others = GetSummonBankModel().SummonDataSoList
                    .Where(summon => weights.All(row => row.Name != summon.summonName)).ToArray();
                return others[Random.Range(0, others.Length)];
            }

            throw new InvalidOperationException($"Cannot draw a summon for {summonMagicType}.");
        }
    }
}
