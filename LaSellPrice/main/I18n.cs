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
        /// <summary>Mod��������</summary>
        private static ITranslationHelper Translations;

        /*********
        ** Public methods
        *********/
        /// <summary>��ʼ��</summary>
        /// <param name="translations">Mod��������</param>
        public static void Init(ITranslationHelper translations)
        {
            I18n.Translations = translations;
        }

        /// <summary>��ȡ���۶�Ӧ�������ı�</summary>
        public static string Labels_SinglePrice()
        {
            return I18n.GetByKey("labels.single-price");
        }

        /// <summary>��ȡ�ܼ۶�Ӧ�������ı�</summary>
        public static string Labels_StackPrice()
        {
            return I18n.GetByKey("labels.stack-price");
        }

        /*********
        ** Private methods
        *********/
        /// <summary>ͨ��KEY��ȡ������Ӧ���ı�</summary>
        /// <param name="key">JSON KEY</param>
        /// <param name="tokens">���ƣ�ò��û���������</param>
        private static Translation GetByKey(string key, object tokens = null)
        {
            if (I18n.Translations == null)
                throw new InvalidOperationException($"You must call {nameof(I18n)}.{nameof(I18n.Init)} from the mod's entry method before reading translations.");
            return I18n.Translations.Get(key, tokens);
        }
    }
}

