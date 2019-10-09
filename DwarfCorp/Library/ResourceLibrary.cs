using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DwarfCorp
{
    public static partial class Library
    {
        private static Dictionary<String, ResourceType> Resources = null;
        private static bool ResourcesInitialized = false;

        private static void InitializeResources()
        {
            if (ResourcesInitialized)
                return;
            ResourcesInitialized = true;

            Resources = new Dictionary<String, ResourceType>();

            var resourceList = FileUtils.LoadJsonListFromDirectory<ResourceType>("World\\ResourceItems", null, r => r.Name);

            foreach (var resource in resourceList)
            {
                resource.Generated = false;
                AddResourceType(resource);
            }

            Console.WriteLine("Loaded Resource Library.");
        }

        public static IEnumerable<ResourceType> EnumerateResourceTypesWithTag(String tag)
        {
            InitializeResources();
            return Resources.Values.Where(resource => resource.Tags.Contains(tag));
        }

        public static ResourceType FindMedianResourceTypeWithTag(String tag)
        {
            InitializeResources();
            var applicable = Resources.Values.Where(resource => resource.Tags.Contains(tag)).ToList();
            if (applicable.Count == 0) return null;
            applicable.Sort((a, b) => (int)a.MoneyValue.Value - (int)b.MoneyValue.Value);
            return applicable[applicable.Count / 2];
        }

        public static MaybeNull<ResourceType> GetResourceType(string name)
        {
            InitializeResources();
            return Resources.ContainsKey((String) name) ? Resources[name] : null;
        }

        public static bool DoesResourceTypeExist(String Name)
        {
            InitializeResources();
            return Resources.ContainsKey(Name);
        }

        public static IEnumerable<ResourceType> EnumerateResourceTypes()
        {
            InitializeResources();
            return Resources.Values;
        }

        public static void AddResourceType(ResourceType resource)
        {
            InitializeResources();

            Resources[resource.Name] = resource;

            if (resource.Tags.Contains("Money"))
                EntityFactory.RegisterEntity(resource.Name + " Resource", (position, data) => new CoinPile(EntityFactory.World.ComponentManager, position)
                {
                    Money = data.Has("Money") ? data.GetData<DwarfBux>("Money") : (DwarfBux)64m
                });
            else
                EntityFactory.RegisterEntity(resource.Name + " Resource", (position, data) => new ResourceEntity(EntityFactory.World.ComponentManager, new Resource(resource.Name), position));   
        }

        public static void AddResourceTypeIfNew(ResourceType Resource)
        {
            InitializeResources();

            if (!DoesResourceTypeExist(Resource.Name))
                AddResourceType(Resource);
        }

        public static MaybeNull<ResourceType> CreateResourceType(MaybeNull<ResourceType> From)
        {
            if (From.HasValue(out var from))
            {
                var r = CreateResourceType();

                r.Generated = true;

                r.Name = from.Name;
                r.ShortName = from.ShortName;
                r.MoneyValue = from.MoneyValue;
                r.Description = from.Description;
                r.GuiLayers = new List<TileReference>(from.GuiLayers);
                r.Tint = from.Tint;
                r.Tags = new List<String>(from.Tags);
                r.FoodContent = from.FoodContent;
                r.PlantToGenerate = from.PlantToGenerate;
                r.CraftInfo = from.CraftInfo;
                r.CompositeLayers = from.CompositeLayers == null ? null : new List<ResourceType.CompositeLayer>(from.CompositeLayers);
                r.TrinketData = from.TrinketData;
                r.AleName = from.AleName;
                r.PotionType = from.PotionType;
                r.Category = from.Category;

                return r;
            }

            return null;
        }

        public static ResourceType CreateResourceType()
        {
            return new ResourceType()
            {
                Generated = true
            };
        }
        
        public static MaybeNull<ResourceType> CreateAleResourceType(String type)
        {
            InitializeResources();

            if (GetResourceType(type).HasValue(out var baseResource))
            {
                var aleName = String.IsNullOrEmpty(baseResource.AleName) ? type + " Ale" : baseResource.AleName;

                if (!DoesResourceTypeExist(aleName))
                {
                    if (CreateResourceType(GetResourceType("Ale")).HasValue(out var r))
                    {
                        r.Name = aleName;
                        r.ShortName = aleName;
                        AddResourceType(r);
                    }
                }

                return GetResourceType(aleName);
            }

            return null;
        }

        public static MaybeNull<ResourceType> CreateMealResourceType(String typeA, String typeB)
        {
            InitializeResources();

            var componentA = GetResourceType(typeA);
            var componentB = GetResourceType(typeB);
            if (componentA.HasValue(out var A) && componentB.HasValue(out var B) && CreateResourceType(GetResourceType("Meal")).HasValue(out var r))
            {
                r.FoodContent = A.FoodContent + B.FoodContent;
                r.Name = TextGenerator.GenerateRandom(new List<String>() { A.Name, B.Name }, TextGenerator.GetAtoms(ContentPaths.Text.Templates.food));
                r.MoneyValue = 2m * (A.MoneyValue + B.MoneyValue);
                r.ShortName = r.Name;

                AddResourceTypeIfNew(r);
                return GetResourceType(r.Name);
            }

            return null;
        }

        public static MaybeNull<ResourceType> CreateEncrustedTrinketResourceType(String resourcetype, String gemType)
        {
            InitializeResources();

            var resultName = gemType + "-encrusted " + resourcetype;
            if (DoesResourceTypeExist(resultName))
                return GetResourceType(resultName);

            if (GetResourceType(resourcetype).HasValue(out var baseResource) && GetResourceType(gemType).HasValue(out var gemResource))
            {
                if (CreateResourceType(baseResource).HasValue(out var toReturn))
                {
                    toReturn.Name = resultName;
                    toReturn.MoneyValue += gemResource.MoneyValue * 2m;
                    toReturn.Tags = new List<String>() { "Craft", "Precious" };

                    toReturn.CompositeLayers = new List<ResourceType.CompositeLayer>();
                    toReturn.CompositeLayers.AddRange(baseResource.CompositeLayers);
                    if (baseResource.TrinketData.EncrustingAsset != null)
                        toReturn.CompositeLayers.Add(
                            new ResourceType.CompositeLayer
                            {
                                Asset = baseResource.TrinketData.EncrustingAsset,
                                FrameSize = new Point(32, 32),
                                Frame = new Point(baseResource.TrinketData.SpriteColumn, gemResource.TrinketData.SpriteRow)
                            });

                    toReturn.GuiLayers = new List<TileReference>();
                    toReturn.GuiLayers.AddRange(baseResource.GuiLayers);
                    toReturn.GuiLayers.Add(new TileReference(baseResource.TrinketData.EncrustingAsset, gemResource.TrinketData.SpriteRow * 7 + baseResource.TrinketData.SpriteColumn));

                    AddResourceType(toReturn);
                    return toReturn;
                }
            }

            return null;
        }

        public static MaybeNull<ResourceType> CreateTrinketResourceType(String baseMaterial, float quality)
        {
            InitializeResources();

            string[] names =
            {
                "Ring",
                "Bracer",
                "Pendant",
                "Figure",
                "Earrings",
                "Staff",
                "Crown"
            };

            int[] tiles =
            {
                0,
                1,
                2,
                3,
                4,
                5,
                6
            };

            float[] values =
            {
                1.5f,
                1.8f,
                1.6f,
                3.0f,
                2.0f,
                3.5f,
                4.0f
            };

            string qualityType = "";
            if (quality < 0.5f)
                qualityType = "Very poor";
            else if (quality < 0.75)
                qualityType = "Poor";
            else if (quality < 1.0f)
                qualityType = "Mediocre";
           else if (quality < 1.25f)
                qualityType = "Good";
            else if (quality < 1.75f)
                qualityType = "Excellent";
            else if(quality < 2.0f)
                qualityType = "Masterwork";
            else
                qualityType = "Legendary";

            var item = MathFunctions.Random.Next(names.Count());
            var name = baseMaterial + " " + names[item] + " (" + qualityType + ")";

            if (DoesResourceTypeExist(name))
                return GetResourceType(name);

            if (GetResourceType(baseMaterial).HasValue(out var material) && CreateResourceType(Resources["Trinket"]).HasValue(out var toReturn))
            {
                toReturn.Name = name;
                toReturn.ShortName = baseMaterial + " " + names[item];
                toReturn.MoneyValue = values[item] * material.MoneyValue * 3m * quality;
                toReturn.Tint = material.Tint;

                var tile = new Point(tiles[item], material.TrinketData.SpriteRow);

                toReturn.CompositeLayers = new List<ResourceType.CompositeLayer>(new ResourceType.CompositeLayer[]
                {
                new ResourceType.CompositeLayer
                {
                    Asset = material.TrinketData.BaseAsset,
                    FrameSize = new Point(32, 32),
                    Frame = tile
                }
                });

                var trinketInfo = material.TrinketData;
                trinketInfo.SpriteColumn = tile.X;
                toReturn.TrinketData = trinketInfo;
                toReturn.GuiLayers = new List<TileReference>() { new TileReference(material.TrinketData.BaseAsset, tile.Y * 7 + tile.X) };

                AddResourceType(toReturn);
                return toReturn;
            }

            return null;
        }
        
        public static MaybeNull<ResourceType> CreateBreadResourceType(String component)
        {
            InitializeResources();

            if (DoesResourceTypeExist(component + " Bread"))
                return GetResourceType(component + " Bread");

            if (CreateResourceType(GetResourceType("Bread")).HasValue(out var toReturn))
            {
                toReturn.Name = component + " Bread";
                AddResourceType(toReturn);
                return toReturn;
            }

            return null;
        }
    }

}