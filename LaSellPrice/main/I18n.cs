using System;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI;

namespace LaSellPrice.main
{
    internal static class I18n
    {
        /*********
        ** Fields
        *********/
        /// <summary>Mod翻译助手</summary>
        private static ITranslationHelper Translations;

        /*********
        ** Public methods
        *********/
        /// <summary>初始化</summary>
        /// <param name="translations">Mod翻译助手</param>
        public static void Init(ITranslationHelper translations)
        {
            I18n.Translations = translations;
        }

        /// <summary>获取单价对应翻译后的文本</summary>
        public static string Labels_SinglePrice()
        {
            return I18n.GetByKey("labels.single-price");
        }

        /// <summary>获取总价对应翻译后的文本</summary>
        public static string Labels_StackPrice()
        {
            return I18n.GetByKey("labels.stack-price");
        }

        /*********
        ** Private methods
        *********/
        /// <summary>通过KEY获取翻译后对应的文本</summary>
        /// <param name="key">JSON KEY</param>
        /// <param name="tokens">令牌，貌似没发现如何用</param>
        private static Translation GetByKey(string key, object tokens = null)
        {
            if (I18n.Translations == null)
                throw new InvalidOperationException($"You must call {nameof(I18n)}.{nameof(I18n.Init)} from the mod's entry method before reading translations.");
            return I18n.Translations.Get(key, tokens);
        }
    }
}

