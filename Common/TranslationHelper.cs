using System;
using System.Collections.Generic;
using System.Reflection;

using StardewModdingAPI;

namespace Leclair.Stardew.Common
{
    public static class TranslationHelper {

		public static bool ContainsKey(this ITranslationHelper helper, string key) {
			// This is so ugly.
			FieldInfo field = helper.GetType()
				.GetField("Translator", BindingFlags.Instance | BindingFlags.NonPublic);

			if (field == null)
				return false;

			object Translator = field.GetValue(helper);
			if (Translator == null)
				return false;

			FieldInfo flfield = Translator.GetType()
				.GetField("ForLocale", BindingFlags.Instance | BindingFlags.NonPublic);

			if (flfield == null)
				return false;

			IDictionary<string, Translation> ForLocale = flfield.GetValue(Translator) as IDictionary<string, Translation>;
			return ForLocale != null && ForLocale.ContainsKey(key);
		}

	}
}