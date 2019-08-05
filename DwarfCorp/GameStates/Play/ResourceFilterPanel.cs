using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using System.Text.RegularExpressions;

namespace DwarfCorp.Play
{
    public class ResourceFilterPanel : Widget
    {
        public Stockpile Stockpile;

        public override void Construct()
        {
            List<Resource.ResourceTags> blacklistableResources = new List<Resource.ResourceTags>()
            {
                Resource.ResourceTags.Alcohol, // Todo: Need a central clearing house for all resource tags. Do they need to be an actual object rather than an enum?
                Resource.ResourceTags.Meat,
                Resource.ResourceTags.Metal,
                Resource.ResourceTags.Gem,
                Resource.ResourceTags.Magical,
                Resource.ResourceTags.Wood,
                Resource.ResourceTags.Soil,
                Resource.ResourceTags.Sand,
                Resource.ResourceTags.Fruit,
                Resource.ResourceTags.Gourd,
                Resource.ResourceTags.Grain,
                Resource.ResourceTags.Fungus,
                Resource.ResourceTags.Fuel,
                Resource.ResourceTags.Craft,
                Resource.ResourceTags.CraftItem,
                Resource.ResourceTags.Bone,
                Resource.ResourceTags.Potion,
                Resource.ResourceTags.PreparedFood,
                Resource.ResourceTags.Rail,
                Resource.ResourceTags.Seed
            }.OrderBy(t => t.ToString()).ToList();

            Font = "font10";

            var bottom = AddChild(new Widget()
            {
                AutoLayout = AutoLayout.DockBottom,
                MinimumSize = new Point(0, 32)
            });

            var grid = AddChild(new GridPanel()
            {
                AutoLayout = AutoLayout.DockFill,
                ItemSize = new Point(200, 32),
                ItemSpacing = new Point(2, 2)
            }) as GridPanel;

            List<CheckBox> boxes = new List<CheckBox>();
            foreach (Resource.ResourceTags tagType in blacklistableResources)
            {
                var resource = Library.FindMedianResourceTypeWithTag(tagType);
                var resources = Library.EnumerateResourceTypesWithTag(tagType);
                Resource.ResourceTags lambdaType = tagType;
                var entry = grid.AddChild(new Widget());

                if (resource != null)
                {
                    entry.AddChild(new ResourceIcon()
                    {
                        MinimumSize = new Point(32, 32),
                        MaximumSize = new Point(32, 32),
                        Layers = resource.GuiLayers,
                        AutoLayout = AutoLayout.DockLeft
                    });
                }

                var numResourcesInGroup = resources.Count();
                var extraTooltip = numResourcesInGroup > 0 ? "\ne.g " + TextGenerator.GetListString(resources.Select(s => (string)s.Name).Take(Math.Min(numResourcesInGroup, 4)).ToList()) : "";

                boxes.Add(entry.AddChild(new CheckBox()
                {
                    Text = SplitCamelCase(tagType.ToString()),
                    Tooltip = "Check to allow this stockpile to store " + tagType.ToString() + " resources." + extraTooltip,
                    CheckState = !Stockpile.BlacklistResources.Contains(tagType),
                    OnCheckStateChange = (checkSender) =>
                    {
                        var checkbox = checkSender as CheckBox;
                        if (checkbox.CheckState && Stockpile.BlacklistResources.Contains(lambdaType))
                        {
                            Stockpile.BlacklistResources.Remove(lambdaType);
                        }
                        else if (!Stockpile.BlacklistResources.Contains(lambdaType))
                        {
                            Stockpile.BlacklistResources.Add(lambdaType);
                        }
                    },
                    AutoLayout = AutoLayout.DockLeft
                }
                ) as CheckBox);
            }

            bottom.AddChild(new CheckBox()
            {
                Text = "Toggle All",
                CheckState = boxes.All(b => b.CheckState),
                OnCheckStateChange = (checkSender) =>
                {
                    foreach (var box in boxes)
                    {
                        box.CheckState = (checkSender as CheckBox).CheckState;
                    }
                },
                AutoLayout = AutoLayout.DockLeft
            });

            this.Layout();
            base.Construct();
        }

        private static string SplitCamelCase(string str)
        {
            return Regex.Replace(
                Regex.Replace(
                    str,
                    @"(\P{Ll})(\P{Ll}\p{Ll})",
                    "$1 $2"
                ),
                @"(\p{Ll})(\P{Ll})",
                "$1 $2"
            );
        }
    }
}
