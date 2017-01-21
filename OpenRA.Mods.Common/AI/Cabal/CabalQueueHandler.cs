using OpenRA.Mods.Common.Traits;
using System;

namespace OpenRA.Mods.Common.AI.Cabal
{
    public class CabalQueueHandler
    {
        private readonly CabalOrderManager _orderManager;
        private readonly ProductionQueue _queue;
        private readonly Func<ProductionQueue, ActorInfo> _startConstructionCallback;
        private readonly Action<ProductionItem> _constructionFinishedCallback;

        private ProductionItem _lastItem = null;

        public CabalQueueHandler(CabalOrderManager orderManager, ProductionQueue queue, Func<ProductionQueue, ActorInfo> startConstructionCallback, Action<ProductionItem> constructionFinishedCallback)
        {
            _orderManager = orderManager;
            _queue = queue;
            _constructionFinishedCallback = constructionFinishedCallback;
            _startConstructionCallback = startConstructionCallback;
        }


        public void Tick()
        {
            var currentItem = _queue.CurrentItem();


            if (currentItem == null)
            {
                //TODO: implement func callback to select what to build
                if (_lastItem != null)
                {
                    _constructionFinishedCallback.Invoke(_lastItem);
                }

                var itemToProduce = _startConstructionCallback.Invoke(_queue);
                if(itemToProduce != null)
                {
                    Utility.BotDebug("Queue production of {0}", itemToProduce.Name);
                    _orderManager.QueueOrder(Order.StartProduction(_queue.Actor, itemToProduce.Name, 1));
                }
            }
            //Building finished
            else if(currentItem.Done)
            {
                _constructionFinishedCallback.Invoke(currentItem);
                _lastItem = null;

            }
            //unit finished
            else if(currentItem != _lastItem && _lastItem != null)
            {
                _constructionFinishedCallback.Invoke(_lastItem);
                _lastItem = currentItem;
            }
            else
            {
                _lastItem = currentItem;
            }
        }
    }
}
