﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace DwarfCorp
{

    /// <summary>
    /// A static collection of resource types (should eventually be replaced with a file)
    /// </summary>
    public class ResourceLibrary
    {
        public enum ResourceType
        {
            Wood,
            Stone,
            Dirt,
            Mana,
            Gold,
            Iron,
            Apple,
            Mushroom,
            Grain,
            Sand,
            Coal
        }

        public static Dictionary<ResourceType, Resource> Resources = new Dictionary<ResourceType, Resource>();

        public static Dictionary<ResourceType, string> ResourceNames = new Dictionary<ResourceType, string>()
        {
            {
                ResourceType.Wood, "Wood"
            },
            {
                ResourceType.Stone, "Stone"
            },
            {
                ResourceType.Dirt, "Dirt"
            },
            {
                ResourceType.Mana, "Mana"
            },
            {
                ResourceType.Gold, "Gold"
            },
            {
                ResourceType.Iron, "Iron"
            },
            {
                ResourceType.Apple, "Apple"
            },
            {
                ResourceType.Mushroom, "Mushroom"
            },
            {
                ResourceType.Grain, "Grain"
            },
            {
                ResourceType.Sand, "Sand"
            },
            {
                ResourceType.Coal, "Coal"
            }
        };

        public static Resource GetResourceByName(string name)
        {
            return (from pair in ResourceNames where pair.Value == name select Resources[pair.Key]).FirstOrDefault();
        }


        private static Rectangle GetRect(int x, int y)
        {
            int tileSheetWidth = AssetSettings.Default.ResourceSheet_tileWidth;
            int tileSheetHeight = AssetSettings.Default.ResourceSheet_tileHeight;
            return new Rectangle(x * tileSheetWidth, y * tileSheetHeight, tileSheetWidth, tileSheetHeight);
        }

        public ResourceLibrary(Game game)
        {
            Texture2D tileSheet = TextureManager.GetTexture(ContentPaths.Entities.Resources.resources);
            Resources = new Dictionary<ResourceType, Resource>();
            Resources[ResourceType.Wood] = new Resource(ResourceType.Wood, 1.0f, "Sometimes hard to come by! Comes from trees.", new ImageFrame(tileSheet, GetRect(3, 1)), Resource.ResourceTags.Material);
            Resources[ResourceType.Stone] = new Resource(ResourceType.Stone, 0.5f, "Dwarf's favorite material! Comes from the earth.", new ImageFrame(tileSheet, GetRect(3, 0)), Resource.ResourceTags.Material);
            Resources[ResourceType.Dirt] = new Resource(ResourceType.Dirt, 0.1f, "Can't get rid of it! Comes from the earth.", new ImageFrame(tileSheet, GetRect(0, 1)), Resource.ResourceTags.Material);
            Resources[ResourceType.Sand] = new Resource(ResourceType.Sand, 0.2f, "Can't get rid of it! Comes from the earth.", new ImageFrame(tileSheet, GetRect(1, 1)), Resource.ResourceTags.Material);
            Resources[ResourceType.Mana] = new Resource(ResourceType.Mana, 100.0f, "Mysterious properties!",
                new ImageFrame(tileSheet, GetRect(1, 0)), Resource.ResourceTags.Precious) {SelfIlluminating = true};
            Resources[ResourceType.Gold] = new Resource(ResourceType.Gold, 50.0f, "Shiny!", new ImageFrame(tileSheet, GetRect(0, 0)), Resource.ResourceTags.Precious);
            Resources[ResourceType.Coal] = new Resource(ResourceType.Coal, 10.0f, "Used as fuel", new ImageFrame(tileSheet, GetRect(2, 2)), Resource.ResourceTags.Material);
            Resources[ResourceType.Iron] = new Resource(ResourceType.Iron, 5.0f, "Needed to build things.", new ImageFrame(tileSheet, GetRect(2, 0)), Resource.ResourceTags.Material);
            Resources[ResourceType.Apple] = new Resource(ResourceType.Apple, 0.5f, "Dwarves can eat these.", new ImageFrame(tileSheet, GetRect(2, 1)), Resource.ResourceTags.Food) { FoodContent = 10 };
            Resources[ResourceType.Mushroom] = new Resource(ResourceType.Mushroom, 0.25f, "Dwarves can eat these.", new ImageFrame(tileSheet, GetRect(1, 2)), Resource.ResourceTags.Food) { FoodContent = 10 };
            Resources[ResourceType.Grain] = new Resource(ResourceType.Grain, 0.25f, "Dwarves can eat this.", new ImageFrame(tileSheet, GetRect(0, 2)), Resource.ResourceTags.Food) { FoodContent = 5 };
        }
    }

}