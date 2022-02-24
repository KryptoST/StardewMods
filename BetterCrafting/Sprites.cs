
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI;


namespace Leclair.Stardew.BetterCrafting {
	public static class Sprites {

		public static class Buttons {
			public static Texture2D Texture;

			public static Rectangle UNIFORM_OFF = new(0, 0, 16, 16);
			public static Rectangle UNIFORM_ON = new(0, 16, 16, 16);

			public static Rectangle SEASONING_ON = new(16, 0, 16, 16);
			public static Rectangle SEASONING_OFF = new(16, 16, 16, 16);
			public static Rectangle SEASONING_LOCAL = new(48, 16, 16, 16);

			public static Rectangle FAVORITE_ON = new(32, 0, 16, 16);
			public static Rectangle FAVORITE_OFF = new(32, 16, 16, 16);

			public static Rectangle WRENCH = new(48, 0, 16, 16);
		}

		public static void Load(IContentHelper Helper) {
			Buttons.Texture = Helper.Load<Texture2D>("assets/buttons.png");
		}

	}
}