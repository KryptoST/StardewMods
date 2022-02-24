using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Leclair.Stardew.BetterCrafting.Models;
using Leclair.Stardew.Common.Crafting;
using Leclair.Stardew.Common.Events;
using Leclair.Stardew.Common.Types;

using StardewModdingAPI;
using StardewModdingAPI.Events;

using StardewValley;

namespace Leclair.Stardew.BetterCrafting.Managers {

	public class RecipeManager : BaseManager {

		// Name -> ID Conversion
		private readonly static Regex NAME_TO_ID = new(@"[^a-z0-9_]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		// Providers
		private readonly List<IRecipeProvider> Providers = new();

		// Recipes
		private int CraftingCount = 0;
		private int CookingCount = 0;

		private readonly Dictionary<string, IRecipe> CraftingRecipesByName = new();
		private readonly Dictionary<string, IRecipe> CookingRecipesByName = new();

		private readonly List<IRecipe> CraftingRecipes = new();
		private readonly List<IRecipe> CookingRecipes = new();

		// Categories
		private Category[] DefaultCraftingCategories;
		private Category[] DefaultCookingCategories;
		private readonly object DefaultLock = new();

		private readonly Dictionary<long, Category[]> CraftingCategories = new();
		private readonly Dictionary<long, Category[]> CookingCategories = new();

		public bool DefaultsLoaded = false;

		public RecipeManager(ModEntry mod) : base(mod) { }

		private void AssertFarmer(Farmer who) {
			if (who == null)
				throw new ArgumentNullException("who cannot be null");
		}

		#region Events

		[Subscriber]
		private void OnSaveLoaded(object sender, SaveLoadedEventArgs e) {
			LoadRecipes();
			LoadCategories();
		}

		#endregion

		#region Lock Helpers

		private void WithRecipes(Action action) {
			lock ((CraftingRecipes as ICollection).SyncRoot) {
				lock ((CookingRecipes as ICollection).SyncRoot) {
					action();
				}
			}
		}

		private void WithRecipesByName(Action action) {
			lock ((CraftingRecipesByName as ICollection).SyncRoot) {
				lock ((CookingRecipesByName as ICollection).SyncRoot) {
					action();
				}
			}
		}

		private void WithCategories(Action action) {
			lock ((CraftingCategories as ICollection).SyncRoot) {
				lock ((CookingCategories as ICollection).SyncRoot) {
					action();
				}
			}
		}

		private void WithDefaultCategories(Action action) {
			lock (DefaultLock) {
				action();
			}
		}

		#endregion

		#region Recipe Handling

		public IList<IRecipe> GetRecipes(bool cooking) {
			if (CraftingCount != CraftingRecipe.craftingRecipes.Count || CookingCount != CraftingRecipe.cookingRecipes.Count) {
				Log("Recipe count changed. Re-caching recipes.", LogLevel.Info);
				LoadRecipes();
			}

			if (cooking)
				return CookingRecipes;

			return CraftingRecipes;
		}


		#endregion

		#region Category Handling

		public Category[] GetCategories(Farmer who, bool cooking) {
			AssertFarmer(who);

			if (!DefaultsLoaded)
				LoadDefaults();

			long id = who.UniqueMultiplayerID;
			Category[] result = null;

			WithCategories(() => {
				if (cooking)
					CookingCategories.TryGetValue(id, out result);
				else
					CraftingCategories.TryGetValue(id, out result);
			});

			return result ?? (cooking ? DefaultCookingCategories : DefaultCraftingCategories) ?? new Category[0];
		}

		public void SetCategories(Farmer who, IEnumerable<Category> categories, bool cooking) {
			AssertFarmer(who);

			long id = who.UniqueMultiplayerID;
			Category[] array = categories as Category[] ?? categories?.ToArray();

			WithCategories(() => {
				if (cooking) {
					if (array == null)
						CookingCategories.Remove(id);
					else
						CookingCategories[id] = array;
				} else {
					if (array == null)
						CraftingCategories.Remove(id);
					else
						CraftingCategories[id] = array;
				}
			});
		}

		#endregion

		#region IRecipeProvider

		public IRecipe GetRecipe(CraftingRecipe recipe) {
			return new BaseRecipe(Mod, recipe);
		}

		#endregion

		#region Recipe Providers

		public void AddProvider(IRecipeProvider provider) {
			if (provider == null)
				throw new ArgumentNullException("provider cannot be null");

			if (Providers.Contains(provider))
				return;

			Providers.Add(provider);
			Providers.Sort((a, b) => a.RecipePriority.CompareTo(b.RecipePriority));
		}

		#endregion

		#region Data Loading

		public IRecipe GetProvidedRecipe(string name, bool cooking) {
			CraftingRecipe raw = new(name, cooking);

			foreach (IRecipeProvider provider in Providers) {
				IRecipe recipe = provider.GetRecipe(raw);
				if (recipe != null)
					return recipe;
			}

			return GetRecipe(raw);
		}


		public void LoadRecipes() {
			WithRecipes(() => WithRecipesByName(() => {
				CraftingRecipesByName.Clear();
				CookingRecipesByName.Clear();

				CraftingRecipes.Clear();
				CookingRecipes.Clear();

				CookingCount = CraftingRecipe.cookingRecipes.Count;
				CraftingCount = CraftingRecipe.craftingRecipes.Count;

				// Cooking
				foreach (string key in CraftingRecipe.cookingRecipes.Keys) {
					IRecipe recipe = GetProvidedRecipe(key, true);

					CookingRecipesByName.Add(key, recipe);
					CookingRecipes.Add(recipe);
				}

				CookingRecipes.Sort((a, b) => a.SortValue.CompareTo(b.SortValue));

				// Crafting
				foreach (string key in CraftingRecipe.craftingRecipes.Keys) {
					IRecipe recipe = GetProvidedRecipe(key, false);
					CraftingRecipesByName.Add(key, recipe);
					CraftingRecipes.Add(recipe);
				}

				Log($"Loaded {CookingRecipes.Count} cooking recipes and {CraftingRecipes.Count} crafting recipes.", LogLevel.Debug);
			}));
		}

		private Dictionary<string, Category> HydrateCategories(IEnumerable<Category> categories, string source, Dictionary<string, Category> byID = null) {
			byID ??= new();

			foreach (Category cat in categories) {
				try {
					MergeCategory(cat, byID);
				} catch (Exception ex) {
					Log($"Skipping bad category in {source}.", LogLevel.Warn, ex);
				}
			}

			return byID;
		}

		public void SaveCategories() {
			WithCategories(() => {
				Dictionary<long, Categories> data = new();

				string newName = I18n.Category_New();

				foreach (var entry in CraftingCategories) {
					long id = entry.Key;
					if (entry.Value == null || entry.Value.Length == 0)
						continue;

					Category[] valid = entry.Value.Where(cat => {
						return
							!((cat.Recipes == null || cat.Recipes.Count == 0) &&
							cat.Name.Equals(newName) &&
							(cat.Icon == null ||
								(cat.Icon.Type == CategoryIcon.IconType.Item
								&& string.IsNullOrEmpty(cat.Icon.RecipeName))));
					}).ToArray();

					if (valid.Length == 0)
						continue;

					data[id] = new() {
						Crafting = valid
					};
				}

				foreach (var entry in CookingCategories) {
					long id = entry.Key;
					if (entry.Value == null || entry.Value.Length == 0)
						continue;

					Category[] valid = entry.Value.Where(cat => {
						return
							(cat.Recipes == null || cat.Recipes.Count == 0) &&
							cat.Name.Equals(newName) &&
							(cat.Icon == null ||
								(cat.Icon.Type == CategoryIcon.IconType.Item
								&& !string.IsNullOrEmpty(cat.Icon.RecipeName)));
					}).ToArray();

					if (valid.Length == 0)
						continue;

					if (data.ContainsKey(id))
						data[id].Cooking = valid;
					else
						data[id] = new() {
							Cooking = valid
						};
				}

				// We have the data. Save it.
				string path = $"savedata/categories/{Constants.SaveFolderName}.json";

				try {
					Mod.Helper.Data.WriteJsonFile(path, data);
				} catch (Exception ex) {
					Log($"The {path} file could not be saved.", LogLevel.Error, ex);
				}
			});
		}

		public void LoadCategories() {
			WithCategories(() => {
				CookingCategories.Clear();
				CraftingCategories.Clear();

				string path = $"savedata/categories/{Constants.SaveFolderName}.json";
				Dictionary<long, Categories> data;

				try {
					data = Mod.Helper.Data.ReadJsonFile<Dictionary<long, Categories>>(path);
				} catch (Exception ex) {
					Log($"The {path} file is invalid or corrupt.", LogLevel.Error, ex);
					data = null;
				}

				if (data == null)
					return;

				foreach (KeyValuePair<long, Categories> entry in data) {
					if (entry.Value == null)
						continue;

					// Cooking
					if (entry.Value.Cooking != null)
						CookingCategories.Add(
							entry.Key,
							HydrateCategories(
								entry.Value.Cooking,
								$"{path} for user {entry.Key}"
							).Values.ToArray()
						);

					// Crafting
					if (entry.Value.Crafting != null)
						CraftingCategories.Add(
							entry.Key,
							HydrateCategories(
								entry.Value.Crafting,
								$"{path} for user {entry.Key}"
							).Values.ToArray()
						);
				}
			});
		}

		public void LoadDefaults() {
			WithDefaultCategories(() => {
				DefaultsLoaded = true;
				Dictionary<string, Category> CraftingByID = new();
				Dictionary<string, Category> CookingByID = new();

				// Read the primary categories data file.
				const string path = "assets/categories.json";
				Categories cats = null;

				try {
					cats = Mod.Helper.Data.ReadJsonFile<Categories>(path);
					if (cats == null)
						Log($"The {path} file is missing or invalid.", LogLevel.Error);
				} catch (Exception ex) {
					Log($"The {path} file is invalid.", LogLevel.Error, ex);
				}

				if (cats != null) {
					if (cats.Cooking != null)
						HydrateCategories(cats.Cooking, path, CookingByID);

					if (cats.Crafting != null)
						HydrateCategories(cats.Crafting, path, CraftingByID);
				}

				// Now read categories from content packs.
				foreach (var cp in Mod.Helper.ContentPacks.GetOwned()) {
					if (!cp.HasFile("categories.json"))
						continue;

					cats = null;
					try {
						cats = cp.ReadJsonFile<Categories>("categories.json");
					} catch (Exception ex) {
						Log($"The categories.json file of {cp.Manifest.Name} is invalid.", LogLevel.Error, ex);
					}

					if (cats != null) {
						if (cats.Cooking != null)
							HydrateCategories(cats.Cooking, cp.Manifest.Name, CookingByID);
						if (cats.Crafting != null)
							HydrateCategories(cats.Crafting, cp.Manifest.Name, CraftingByID);
					}
				}

				DefaultCraftingCategories = CraftingByID.Values.ToArray();
				DefaultCookingCategories = CookingByID.Values.ToArray();

				bool too_many = DefaultCraftingCategories.Length > 7 || DefaultCookingCategories.Length > 7;

				Log($"Loaded {DefaultCookingCategories.Length} cooking categories and {DefaultCraftingCategories.Length} crafting categories.", too_many ? LogLevel.Warn : LogLevel.Debug);
				if (too_many)
					Log($"Further categories after seven will be ignored.", LogLevel.Warn);
			});
		}

		private void MergeCategory(Category cat, Dictionary<string, Category> categories) {
			if (string.IsNullOrEmpty(cat.Id)) {
				// Generate the ID from the name.
				if (string.IsNullOrEmpty(cat.Name))
					throw new InvalidOperationException("Category with no ID or name.");

				cat.Id = NAME_TO_ID.Replace(cat.Id.ToLower(), "_");
			}

			// Is there an existing entry?
			if (categories.TryGetValue(cat.Id, out Category existing)) {
				// Overwrite names.
				if (!string.IsNullOrEmpty(cat.Name))
					existing.Name = cat.Name;

				// Overwrite I18n keys.
				if (cat.I18nKey != null)
					existing.I18nKey = string.IsNullOrEmpty(cat.I18nKey) ? null : cat.I18nKey;

				// Overwrite icons.
				if (cat.Icon != null)
					existing.Icon = cat.Icon;

				// Merge Recipes

				IEnumerable<string> recipes = existing.Recipes;
				bool modified = false;

				// Unwanted Recipes
				if ((cat.UnwantedRecipes?.Length ?? 0) > 0) {
					recipes = recipes.Except(cat.UnwantedRecipes);
					modified = true;
				}

				// New Recipes
				if ((cat.Recipes?.Count ?? 0) > 0) {
					recipes = recipes.Concat(cat.Recipes.Except(recipes));
					modified = true;
				}

				if (modified)
					existing.Recipes = new CaseInsensitiveHashSet(recipes);

				return;
			}

			if (string.IsNullOrEmpty(cat.Id))
				cat.Name = cat.Id;

			if (cat.I18nKey == null) {
				string possible = $"category.{cat.Id}";
				if (Mod.DoesTranslationExist(possible))
					cat.I18nKey = possible;
			}

			if (cat.Recipes == null)
				cat.Recipes = new CaseInsensitiveHashSet();

			cat.UnwantedRecipes = null;
			categories.Add(cat.Id, cat);
		}

		#endregion

	}
}