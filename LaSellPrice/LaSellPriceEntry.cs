using System;
using System.Collections.Generic;
using System.Linq;
using LaSellPrice.main;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using SObject = StardewValley.Object;

namespace LaSellPrice
{
    /// <summary>Mod入口</summary>
    internal class LaSellPriceEntry : Mod
    {
        /*********
        ** Fields
        *********/
        /// <summary>硬币图标矩形</summary>
        private readonly Rectangle CoinSourceRect = new Rectangle(5, 69, 6, 6);

        /// <summary>工具提示框矩形</summary>
        private readonly Rectangle TooltipSourceRect = new Rectangle(0, 256, 60, 60);

        /// <summary>边框像素大小</summary>
        private const int TooltipBorderSize = 15;

        /// <summary>提示框内边距</summary>
        private const int Padding = 8;

        /// <summary>光标对于工具栏的偏移量</summary>
        private readonly Vector2 TooltipOffset = new Vector2(Game1.tileSize / 2);

        /// <summary>无法直接从游戏物品中获得的强制可出售的物品种类</summary>
        private DataModel Data;

        /// <summary>缓存的工具栏实例</summary>
        private readonly PerScreen<Toolbar> Toolbar = new PerScreen<Toolbar>();

        /// <summary>缓存的工具栏槽</summary>
        private readonly PerScreen<IList<ClickableComponent>> ToolbarSlots = new PerScreen<IList<ClickableComponent>>();


        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            // 设置全局日志函数
            Utils.InitLog(this.Monitor);

            // 初始化I18n多语言文本
            I18n.Init(helper.Translation);

            // 加载强制可出售物品
            this.Data = helper.Data.ReadJsonFile<DataModel>("assets/data.json") ?? new DataModel();
            this.Data.ForceSellable ??= new HashSet<int>();

            // 事件触发
            helper.Events.Display.RenderedActiveMenu += this.OnRenderedActiveMenu;
            helper.Events.Display.RenderedHud += this.OnRenderedHud;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>游戏状态更新后触发 (≈60次/秒).</summary>
        /// <param name="sender">当前事件对象</param>
        /// <param name="e">事件参数</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (e.IsOneSecond)
            {
                if (Context.IsPlayerFree)
                {
                    this.Toolbar.Value = Game1.onScreenMenus.OfType<Toolbar>().FirstOrDefault();
                    this.ToolbarSlots.Value = this.Toolbar.Value != null
                        ? this.Helper.Reflection.GetField<List<ClickableComponent>>(this.Toolbar.Value, "buttons").GetValue()
                        : null;
                }
                else
                {
                    this.Toolbar.Value = null;
                    this.ToolbarSlots.Value = null;
                }
            }
        }

        /// <summary>当打开一个菜单时，在渲染屏幕前绘制时触发。确保 activeClickableMenu 不为空</summary>
        /// <param name="sender">当前事件对象</param>
        /// <param name="e">事件参数</param>
        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            Item item = this.GetItemFromMenu(Game1.activeClickableMenu);
            if (item == null)
                return;

            this.DrawPriceTooltip(Game1.spriteBatch, Game1.smallFont, item);
        }

        /// <summary>在渲染到屏幕前绘制 HUD（工具栏、时钟、天气） 时触发</summary>
        /// <param name="sender">当前事件对象</param>
        /// <param name="e">事件参数</param>
        private void OnRenderedHud(object sender, EventArgs e)
        {
            if (!Context.IsPlayerFree)
                return;

            Item item = this.GetItemFromToolbar();
            if (item == null)
                return;

            this.DrawPriceTooltip(Game1.spriteBatch, Game1.smallFont, item);
        }

        /// <summary>从菜单获取悬停物品的信息</summary>
        /// <param name="menu">悬浮显示的菜单</param>
        private Item GetItemFromMenu(IClickableMenu menu)
        {
            // 游戏菜单获取
            if (menu is GameMenu gameMenu)
            {
                Utils.DebugLog("SellPrice GetItemFromMenu", LogLevel.Info);
                IClickableMenu page = this.Helper.Reflection.GetField<List<IClickableMenu>>(gameMenu, "pages").GetValue()[gameMenu.currentTab];
                if (page is InventoryPage)
                    return this.Helper.Reflection.GetField<Item>(page, "hoveredItem").GetValue();
                else if (page is CraftingPage)
                    return this.Helper.Reflection.GetField<Item>(page, "hoverItem").GetValue();
            }

            // 资源菜单获取
            else if (menu is MenuWithInventory inventoryMenu)
                return inventoryMenu.hoveredItem;

            return null;
        }

        /// <summary>从工具栏获取悬停物品的信息</summary>
        private Item GetItemFromToolbar()
        {
            //Utils.DebugLog($"Toolbar {this.Toolbar}. ToolbarSlots {this.ToolbarSlots}.", LogLevel.Info);
            //Utils.DebugLog($"GameDisplay {Game1.displayHUD}.", LogLevel.Info);
            /*
                确保以下条件全都满足再继续获取物品
                1.角色处于闲置状态IsPlayerFree
                2.工具栏Value不为空
                3.工具栏中槽位不为空
                4.Hud处于显示状态
             */
            if (!Context.IsPlayerFree || this.Toolbar?.Value == null || this.ToolbarSlots == null || !Game1.displayHUD)
                return null;

            // 查找悬停位置
            int x = Game1.getMouseX();
            int y = Game1.getMouseY();
            ClickableComponent hoveredSlot = this.ToolbarSlots.Value.FirstOrDefault(slot => slot.containsPoint(x, y));
            if (hoveredSlot == null)
                return null;

            // 获取资源索引
            int index = this.ToolbarSlots.Value.IndexOf(hoveredSlot);
            if (index < 0 || index > Game1.player.Items.Count - 1)
                return null;
            // 获取悬停物品
            return Game1.player.Items[index];
        }

        /// <summary>绘制商品单价与总价提示框</summary>
        /// <param name="spriteBatch">绘图刷</param>
        /// <param name="font">绘制文本的字体</param>
        /// <param name="item">要显示信息的物品</param>
        private void DrawPriceTooltip(SpriteBatch spriteBatch, SpriteFont font, Item item)
        {
            int stack = item.Stack;
            bool showStack = stack > 1;
            int? price = this.GetSellPrice(item);
            if (price == null)
                return;

            // 获取全局设置的边框、内边距、硬币尺寸、行高
            const int borderSize = LaSellPriceEntry.TooltipBorderSize;
            const int padding = LaSellPriceEntry.Padding;
            int coinSize = this.CoinSourceRect.Width * Game1.pixelZoom;
            int lineHeight = (int)font.MeasureString("X").Y;
            Vector2 offsetFromCursor = this.TooltipOffset;

            // 文本拼接
            string unitLabel = I18n.Labels_SinglePrice() + ":";
            string unitPrice = price.ToString();
            string stackLabel = I18n.Labels_StackPrice() + ":";
            string stackPrice = (price * stack).ToString();

            // 计算单价尺寸，总价尺寸，文本尺寸
            Vector2 unitPriceSize = font.MeasureString(unitPrice);
            Vector2 stackPriceSize = font.MeasureString(stackPrice);
            Vector2 labelSize = font.MeasureString(unitLabel);
            // 有总价的话，取最长的
            if (showStack)
                labelSize = new Vector2(Math.Max(labelSize.X, font.MeasureString(stackLabel).X), labelSize.Y * 2);
            // 计算提示框内容尺寸以及最外层尺寸
            Vector2 innerSize = new Vector2(labelSize.X + padding + Math.Max(unitPriceSize.X, showStack ? stackPriceSize.X : 0) + padding + coinSize, labelSize.Y);
            Vector2 outerSize = innerSize + new Vector2((borderSize + padding) * 2);

            // 根据鼠标计算位置
            //float x = Game1.getMouseX() - offsetFromCursor.X - outerSize.X;
            float x = Game1.getMouseX() - outerSize.X;
            float y = Game1.getMouseY() + offsetFromCursor.Y + borderSize;

            // 调整位置以适应屏幕
            Rectangle area = new Rectangle((int)x, (int)y, (int)outerSize.X, (int)outerSize.Y);
            if (area.Right > Game1.uiViewport.Width)
                x = Game1.uiViewport.Width - area.Width;
            if (area.Bottom > Game1.uiViewport.Height)
                y = Game1.uiViewport.Height - area.Height;

            // 绘制提示框
            IClickableMenu.drawTextureBox(spriteBatch, Game1.menuTexture, this.TooltipSourceRect, (int)x, (int)y, (int)outerSize.X, (int)outerSize.Y, Color.White);

            // 绘制硬币与文本，如果showStack库存大于1则绘制总价行硬币与文本
            spriteBatch.Draw(Game1.debrisSpriteSheet, new Vector2(x + outerSize.X - borderSize - padding - coinSize, y + borderSize + padding), this.CoinSourceRect, Color.White, 0.0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 1f);
            if (showStack)
                spriteBatch.Draw(Game1.debrisSpriteSheet, new Vector2(x + outerSize.X - borderSize - padding - coinSize, y + borderSize + padding + lineHeight), this.CoinSourceRect, Color.White, 0.0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 1f);

            Utility.drawTextWithShadow(spriteBatch, unitLabel, font, new Vector2(x + borderSize + padding, y + borderSize + padding), Game1.textColor);
            Utility.drawTextWithShadow(spriteBatch, unitPrice, font, new Vector2(x + outerSize.X - borderSize - padding - coinSize - padding - unitPriceSize.X, y + borderSize + padding), Game1.textColor);
            if (showStack)
            {
                Utility.drawTextWithShadow(spriteBatch, stackLabel, font, new Vector2(x + borderSize + padding, y + borderSize + padding + lineHeight), Game1.textColor);
                Utility.drawTextWithShadow(spriteBatch, stackPrice, font, new Vector2(x + outerSize.X - borderSize - padding - coinSize - padding - stackPriceSize.X, y + borderSize + padding + lineHeight), Game1.textColor);
            }
        }

        /// <summary>从物品信息中获取售价</summary>
        /// <param name="item">物品</param>
        /// <returns>返回售价, 或者不能售出返回 <c>null</c> </returns>
        private int? GetSellPrice(Item item)
        {
            // 跳过不可出售物品
            if (!this.CanBeSold(item))
                return null;

            // 使用sv中Utility公用类方法获取出售价格
            // return ((i is Object) ? (i as Object).sellToStorePrice(-1L) : (i.salePrice() / 2)) * ((!countStack) ? 1 : i.Stack);
            int price = Utility.getSellToStorePriceOfItem(item, countStack: false);
            return price >= 0 ? price : null as int?;
        }

        /// <summary>判断是否可出售</summary>
        /// <param name="item">物品</param>
        private bool CanBeSold(Item item)
        {
            // 物品类型是否正确并且可被出售 or 是否包含在强制出售数组中（根据物品分类判断）
            return
                (item is SObject obj && obj.canBeShipped())
                || this.Data.ForceSellable.Contains(item.Category);
        }
    }
}
