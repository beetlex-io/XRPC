using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EventNext;
namespace Actors
{
    public interface IAmountService
    {
        Task<int> Income(int amount);
        Task<int> Payout(int amount);
        Task<int> Get();
    }
    [Service(typeof(IAmountService))]
    public class AmountService : ActorState, IAmountService
    {

        private int mAmount;

        public override Task ActorInit(string id)
        {
            return base.ActorInit(id);
        }

        public Task<int> Get()
        {
            return mAmount.ToTask();
        }

        public Task<int> Income(int amount)
        {
            mAmount += amount;
            return mAmount.ToTask();
        }

        public Task<int> Payout(int amount)
        {
            mAmount -= amount;
            return mAmount.ToTask();
        }
    }

}
