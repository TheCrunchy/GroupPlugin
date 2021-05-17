using Sandbox.Game.GameSystems.BankingAndCurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin
{
   public class EconUtils
    {
        public static long getBalance(long walletID)
        {
            MyAccountInfo info;
            if (MyBankingSystem.Static.TryGetAccountInfo(walletID, out info))
            {
                return info.Balance;
            }
            return 0L;
        }
        public static void addMoney(long walletID, Int64 amount)
        {
            MyBankingSystem.ChangeBalance(walletID, amount);

            return;
        }
        public static void takeMoney(long walletID, Int64 amount)
        {
            if (getBalance(walletID) > amount)
            {
                amount = amount * -1;
                MyBankingSystem.ChangeBalance(walletID, amount);
            }
            return;
        }

        public static void doPayment2Players(long walletIDSender, long walletIDRecipient, Int64 amount)
        {
            if (getBalance(walletIDSender) > amount)
            {
                MyBankingSystem.ChangeBalance(walletIDSender, amount);
                // Int64 amountTaxApplied = amount * tax;
                Int64 amountTaxApplied = amount;
                MyBankingSystem.ChangeBalance(walletIDRecipient, amountTaxApplied);
            }
            return;
        }
    }
}
