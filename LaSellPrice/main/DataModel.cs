using System.Collections.Generic;
using SObject = StardewValley.Object;

namespace LaSellPrice.main
{
    /// <summary>ʵ����</summary>
    internal class DataModel
    {
        /// <summary>�̵��������Ʒ SObject.canBeShipped</summary>
        public HashSet<int> ForceSellable { get; set; } = new HashSet<int>();
    }
}
