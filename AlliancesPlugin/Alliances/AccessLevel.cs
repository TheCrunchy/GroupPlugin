using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin
{
   public enum AccessLevel
    {
        HangarSave,
        HangarLoad,
        HangarLoadOther,
        BankWithdraw,
        ShipyardStart,
        ShipyardClaim,
        ShipyardClaimOther,
        DividendPay,
        Invite,
        Kick,
        RevokeLowerTitle,
        GrantLowerTitle,
        UnlockUpgrades,
        RemoveEnemy,
        AddEnemy,
        PayFromBank,
        UnableToParse

    }
}
