using UnityEngine;
using System.Threading;

namespace DroneSim
{
    public class ResourceNode : MonoBehaviour
    {
        int _state;                      // 0 free - 1 reserved - 2 taken

        public bool TryReserve() => Interlocked.CompareExchange(ref _state, 1, 0) == 0;
        public void Free() => _state = 0;
        public bool TryTake() => Interlocked.Exchange(ref _state, 2) == 1;
        public bool IsFree => _state == 0;
    }
}
