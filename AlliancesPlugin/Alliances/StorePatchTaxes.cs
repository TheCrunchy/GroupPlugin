using AlliancesPlugin.Alliances.NewTerritories;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Torch.Managers.PatchManager;
using VRage.Game;
using VRageMath;

namespace AlliancesPlugin.Alliances
{
    [PatchShim]
    public class StorePatchTaxes
    {

        internal static readonly MethodInfo update =
                typeof(MyStoreBlock).GetMethod("SendSellItemResult", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(long), typeof(string), typeof(long), typeof(int), typeof(MyStoreSellItemResults) }, null) ??
                throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo storePatch =
            typeof(StorePatchTaxes).GetMethod(nameof(StorePatchMethod), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo update2 =
         typeof(MyStoreBlock).GetMethod("SellToStation", BindingFlags.Instance | BindingFlags.NonPublic) ??
         throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo storePatch2 =
            typeof(StorePatchTaxes).GetMethod(nameof(StorePatchMethod2), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo update3 =
 typeof(MyStoreBlock).GetMethod("SellToPlayer", BindingFlags.Instance | BindingFlags.NonPublic) ??
 throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo storePatch3 =
            typeof(StorePatchTaxes).GetMethod(nameof(StorePatchMethod3), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");
        public static void Patch(PatchContext ctx)
        {

            ctx.GetPattern(update).Suffixes.Add(storePatch);
            ctx.GetPattern(update2).Prefixes.Add(storePatch2);
            ctx.GetPattern(update3).Prefixes.Add(storePatch3);
        }
        public class TaxItem
        {
            public long playerId;
            public long price;
            public Guid territory;
        }
        public static Dictionary<long, long> Ids = new Dictionary<long, long>();
        public static Dictionary<long, Guid> territorytax = new Dictionary<long, Guid>();
        public static void StorePatchMethod2(long id,
      int amount,
      MyPlayer player,
      MyStation station,
      long sourceEntityId,
      long lastEconomyTick)
        {
            if (!Ids.ContainsKey(id))
            {
                Ids.Add(id, player.Identity.IdentityId);
            }

            return;
        }
        public static void StorePatchMethod3(long id, int amount, long sourceEntityId, MyPlayer player)
        {
            if (!Ids.ContainsKey(id))
            {
                Ids.Add(id, player.Identity.IdentityId);
            }

            return;
        }


        public static void StorePatchMethod(long id, string name, long price, int amount, MyStoreSellItemResults result)
        {

            //  AlliancePlugin.Log.Info("sold to store");
            if (result == MyStoreSellItemResults.Success && Ids.ContainsKey(id))
            {
                if (territorytax.ContainsKey(Ids[id]))
                {
                    TaxItem item = new TaxItem();
                    item.playerId = Ids[id];
                    item.price = price;
                    item.territory = territorytax[Ids[id]];

                    AlliancePlugin.TerritoryTaxes.Add(item);
                }
                else
                {
                    if (AlliancePlugin.TaxesToBeProcessed.ContainsKey(Ids[id]))
                    {
                        AlliancePlugin.TaxesToBeProcessed[Ids[id]] += price;
                    }
                    else
                    {
                        AlliancePlugin.TaxesToBeProcessed.Add(Ids[id], price);
                    }
                }
             
            }
            if (Ids.ContainsKey(id)){
                territorytax.Remove(Ids[id]);
            }
            Ids.Remove(id);
          
            return;
        }

    }
}
