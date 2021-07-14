using System.Collections.Generic;
using SObject = StardewValley.Object;

namespace LaSellPrice.main
{
    /// <summary>实体类</summary>
    internal class DataModel
    {
        /// <summary>商店可销售物品 SObject.canBeShipped</summary>
        public HashSet<int> ForceSellable { get; set; } = new HashSet<int>();
    }
}
