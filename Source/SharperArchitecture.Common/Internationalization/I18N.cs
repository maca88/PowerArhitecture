﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SharperArchitecture.Common.Configuration;
using SecondLanguage;

namespace SharperArchitecture.Common.Internationalization
{
    public static class I18N
    {
        //Used by L10N
        internal static readonly Dictionary<string, Translator> Translators = new Dictionary<string, Translator>();
        private static bool _initialized;

        static I18N()
        {
        }

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }
            Translator.Default.FormatCallback = TranslatorFormatter.Custom;
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            //Using GetFullPath so we can combine absolute and relative paths
            var translDirectory = Path.GetFullPath(Path.Combine(baseDir, AppConfiguration.GetSetting<string>(CommonConfigurationKeys.TranslationsPath)));
            var translPattern = AppConfiguration.GetSetting<string>(CommonConfigurationKeys.TranslationsByCulturePattern);
            var defaultCulture = AppConfiguration.GetSetting<string>(CommonConfigurationKeys.DefaultCulture);
            var defaultTranslPath = Path.Combine(translDirectory, string.Format(translPattern, defaultCulture));

            if (!File.Exists(defaultTranslPath))
                throw new FileNotFoundException($"Default translation file not found. Path: {defaultTranslPath}");
            Translator.Default.RegisterTranslation(defaultTranslPath);

            foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
            {
                if (!File.Exists(Path.Combine(translDirectory, string.Format(translPattern, culture.Name))))
                    continue;
                var translator = new Translator
                {
                    Parent = Translator.Default,
                    FormatCallback = TranslatorFormatter.Custom
                };
                Translators.Add(culture.Name, translator);
                translator.RegisterTranslationsByCulture(translPattern, culture, translDirectory);
            }
            _initialized = true;
        }

        #region Register

        /// <summary>
        /// Use this function in order to register string as localizable (passing a variable as parameter will not work!)
        /// Valid usage: I18N.register("Hello world")
        /// </summary>
        /// <param name="id"></param>
        /// <returns>id</returns>
        public static string Register(string id)
        {
            return id;
        }

        #endregion

        #region Translate

        /// <summary>
        /// Translates a string.
        /// </summary>
        /// <param name="id">The translation key. For Gettext projects, this is typically an untranslated string.</param>
        /// <param name="args">Arguments to replace the format string's placeholders with.</param>
        /// <returns>The translated string, or the formatted translation key if none is set.</returns>
        public static string Translate(string id, params object[] args)
        {
            return Translate(CultureInfo.CurrentCulture, id, args);
        }

        public static string Translate(CultureInfo culture, string id, params object[] args)
        {
            var result = Translators.ContainsKey(culture.Name) 
                ? Translators[culture.Name].Translate(id, args)
                : Translator.Default.Translate(id, args);
            return string.IsNullOrEmpty(result) ? TranslatorFormatter.Custom(id, args) : result;
        }

        #endregion

        #region TranslatePlural

        /// <summary>
        /// Translates a string with distinct singular and plural forms.
        /// </summary>
        /// <param name="id">The singular translation key. For Gettext projects, this is typically a singular untranslated string.</param>
        /// <param name="idPlural">The plural translation key. For Gettext projects, this is typically a plural untranslated string.</param>
        /// <param name="value">The value to look up the plural string for.</param>
        /// <param name="args">Arguments to replace the format string's placeholders with.</param>
        /// <returns>The translated string, or the formatted translation key if none is set.</returns>
        public static string TranslatePlural(string id, string idPlural, long value, params object[] args)
        {
            return TranslatePlural(CultureInfo.CurrentCulture, id, idPlural, value, args);
        }

        public static string TranslatePlural(CultureInfo culture, string id, string idPlural, long value, params object[] args)
        {
            return Translators.ContainsKey(culture.Name) 
                ? Translators[culture.Name].TranslatePlural(id, idPlural, value, args)
                : Translator.Default.TranslatePlural(id, idPlural, value, args);
        }

        #endregion

        #region TranslateContextual

        /// <summary>
        /// Translates a string in a given context.
        /// </summary>
        /// <param name="context">The context, if any, or <c>null</c>.</param>
        /// <param name="id">The translation key. For Gettext projects, this is typically an untranslated string.</param>
        /// <param name="args">Arguments to replace the format string's placeholders with.</param>
        /// <returns>The translated string, or the formatted translation key if none is set.</returns>
        public static string TranslateContextual(string context, string id, params object[] args)
        {
            return TranslateContextual(CultureInfo.CurrentCulture, context, id, args);
        }

        public static string TranslateContextual(CultureInfo culture,  string context, string id, params object[] args)
        {
            return Translators.ContainsKey(culture.Name) 
                ? Translators[culture.Name].TranslateContextual(context, id, args)
                : Translator.Default.TranslateContextual(context, id, args);
        }

        #endregion

        #region TranslateContextualPlural

        /// <summary>
        /// Translates a string with distinct singular and plural forms, in a given context.
        /// </summary>
        /// <param name="context">The context, if any, or <c>null</c>.</param>
        /// <param name="id">The singular translation key. For Gettext projects, this is typically a singular untranslated string.</param>
        /// <param name="idPlural">The plural translation key. For Gettext projects, this is typically a plural untranslated string.</param>
        /// <param name="value">The value to look up the plural string for.</param>
        /// <param name="args">Arguments to replace the format string's placeholders with.</param>
        /// <returns>The translated string, or the formatted translation key if none is set.</returns>
        public static string TranslateContextualPlural(string context, string id, string idPlural, long value,
            params object[] args)
        {
            return TranslateContextualPlural(CultureInfo.CurrentCulture, context, id, idPlural, value, args);
        }

        public static string TranslateContextualPlural(CultureInfo culture, string context, string id, string idPlural,
            long value, params object[] args)
        {
            return Translators.ContainsKey(culture.Name) 
                ? Translators[culture.Name].TranslateContextualPlural(context, id, idPlural, value, args)
                : Translator.Default.TranslateContextualPlural(context, id, idPlural, value, args);
        }

        #endregion
    }
}
