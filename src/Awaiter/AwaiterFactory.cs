using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeetleX.Dispatchs;
namespace BeetleX.XRPC.Awaiter
{
    public class AwaiterFactory
    {
        static AwaiterFactory()
        {


        }

        const int COUNT = 1024 * 1024;

        public AwaiterFactory(int startid = 1, int end = 100000000)
        {
            mID = startid;
            mStartID = startid;
            mEndID = end;
            mTimer = new System.Threading.Timer(OnTimeout, null, 1000, 1000);
            mTimeDispatch = new DispatchCenter<AwaiterItem>(OnTimeProcess);
        }

        private System.Threading.Timer mTimer;

        private int mID;

        private int mStartID;

        private int mEndID;

        private DispatchCenter<AwaiterItem> mTimeDispatch;

        private AwaiterGroup mAwaiterItemGroup = new AwaiterGroup();

        private void OnTimeProcess(AwaiterItem item)
        {
            Response response = new Response();
            response.Status = (short)ResponseCode.REQUEST_TIMEOUT;
            response.Data = new object[] { $"Request {item.Request.Url} time out!" };
            Completed(item, response);
        }

        private void OnTimeout(object state)
        {
            try
            {
                mTimer.Change(-1, -1);
                long timeout = TimeWatch.GetElapsedMilliseconds();      
                var items = mAwaiterItemGroup.GetTimeouts(timeout);
                if (items.Count > 0)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        mTimeDispatch.Enqueue(items[i]);
                    }
                   
                }
            }
            catch
            {

            }
            finally
            {
                mTimer.Change(1000, 1000);
            }
        }

        internal AwaiterItem GetItem(int id)
        {
            return mAwaiterItemGroup.Get(id);
        }

        public (int, TaskCompletionSource<Response>) Create(Request request, Type[] resultType, int timeout = 1000 * 100)
        {
            int id = 0;
            long expiredTime;
            lock (this)
            {
                mID++;
                if (mID >= mEndID)
                    mID = mStartID;
                id = mID;
            
            }
            expiredTime = TimeWatch.GetElapsedMilliseconds() + timeout;
            var item = new AwaiterItem();
            item.ID = id;
            item.ResultType = resultType;
            item.Request = request;
            mAwaiterItemGroup.Set(item.ID, item);
            return (id, item.Create(expiredTime));
        }

        public bool Completed(AwaiterItem item, Response data)
        {
            if (item.Completed(data))
            {
                item.Response = null;
                item.Request = null;
                return true;
            }
            return false;
        }

        public class AwaiterGroup
        {
            public AwaiterGroup()
            {
                for (int i = 0; i < Groups; i++)
                {
                    mGroups.Add(new GroupItem());
                }
            }

            const int Groups = 10;

            private List<GroupItem> mGroups = new List<GroupItem>();

            public void Set(int id, AwaiterItem item)
            {
                mGroups[id % Groups].Set(id, item);
            }

            public AwaiterItem Get(int id)
            {
                return mGroups[id % Groups].Get(id);
            }

            public IList<AwaiterItem> GetTimeouts(double time)
            {
                List<AwaiterItem> items = new List<AwaiterItem>();
                for (int i = 0; i < Groups; i++)
                {
                    mGroups[i].GetTimeouts(items, time);
                }
                return items;
            }


            public class GroupItem
            {
                private ConcurrentDictionary<int, AwaiterItem> mItems = new ConcurrentDictionary<int, AwaiterItem>();

                public void Set(int id, AwaiterItem item)
                {
                    mItems[id] = item;
                }

                public AwaiterItem Get(int id)
                {
                    mItems.TryRemove(id, out AwaiterItem item);
                    return item;
                }

                public void GetTimeouts(List<AwaiterItem> items, double time)
                {
                    foreach (var item in mItems.Values)
                    {
                        if (time > item.TimeOut)
                        {
                            items.Add(Get(item.ID));
                        }
                    }
                }
            }
        }
    }
}
