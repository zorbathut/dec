using System;
using System.Collections.Generic;

namespace Dec.WriterUtil
{
    internal class PendingWriteCoordinator
    {
        // A list of writes that still have to happen. This is used so we don't have to do deep recursive dives and potentially blow our stack.
        private List<Action> pendingWrites = new List<Action>();

        public void RegisterPendingWrite(Action action)
        {
            pendingWrites.Add(action);
        }

        public void DequeuePendingWrites()
        {
            while (DequeuePendingWrite() is var pending && pending != null)
            {
                pending();
            }
        }

        private Action DequeuePendingWrite()
        {
            if (pendingWrites.Count == 0)
            {
                return null;
            }

            var result = pendingWrites[pendingWrites.Count - 1];
            pendingWrites.RemoveAt(pendingWrites.Count - 1);
            return result;
        }
    }
}
